#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	public enum BTCRegimeType { Trend = 1, Range = -1, Neutral = 0 }

	public class BTCRegime : Indicator
	{
		#region Variables
		private ATR				atr;
		private ADX				adx;
		private MAX				maxHigh;
		private MIN				minLow;
		private Series<double>	trSeries;
		private Series<double>	choppinessSeries;
		private Series<double>	atrPctSeries;
		private Series<int>		regimeSeries;	// 1 = Trend, -1 = Range, 0 = Neutral
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= "BTCRegime - detecte si le marche est en TENDANCE, RANGE ou NEUTRE. Combine Choppiness Index + ADX. ATR pur affiche pour reference volatilite.";
				Name						= "BTCRegime";
				Calculate					= Calculate.OnBarClose;
				IsOverlay					= false;
				DisplayInDataBox			= true;
				DrawOnPricePanel			= true;
				DrawHorizontalGridLines		= true;
				DrawVerticalGridLines		= true;
				PaintPriceMarkers			= true;
				ScaleJustification			= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive	= true;

				// Parametres
				Period						= 14;
				ChoppinessRangeThreshold	= 61.8;
				ChoppinessTrendThreshold	= 38.2;
				ADXTrendThreshold			= 25;
				ADXRangeThreshold			= 20;
				ShowRegimeLabel				= true;
				ColorBackground				= true;

				// Plots
				AddPlot(new Stroke(Brushes.DodgerBlue, 2),	PlotStyle.Line, "Choppiness");
				AddPlot(new Stroke(Brushes.Orange, 2),		PlotStyle.Line, "ADX");
				AddPlot(new Stroke(Brushes.Gray, 1),		PlotStyle.Line, "ATR%");

				// Lignes de seuil (Choppiness 38.2 / 61.8 = Fibonacci, standard)
				AddLine(new Stroke(Brushes.Red,			DashStyleHelper.Dash, 1), 61.8,	"ChopRange");
				AddLine(new Stroke(Brushes.Green,		DashStyleHelper.Dash, 1), 38.2,	"ChopTrend");
				AddLine(new Stroke(Brushes.Goldenrod,	DashStyleHelper.Dot,  1), 25,	"ADXTrend");
			}
			else if (State == State.DataLoaded)
			{
				atr					= ATR(Period);
				adx					= ADX(Period);
				maxHigh				= MAX(High, Period);
				minLow				= MIN(Low, Period);
				trSeries			= new Series<double>(this);
				choppinessSeries	= new Series<double>(this);
				atrPctSeries		= new Series<double>(this);
				regimeSeries		= new Series<int>(this);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < Period)
				return;

			// 1. True Range de la barre courante
			double tr;
			if (CurrentBar == 0)
				tr = High[0] - Low[0];
			else
				tr = Math.Max(High[0] - Low[0],
					  Math.Max(Math.Abs(High[0] - Close[1]),
							   Math.Abs(Low[0]  - Close[1])));
			trSeries[0] = tr;

			// 2. Somme des TR sur la periode
			double sumTR = 0;
			for (int i = 0; i < Period; i++)
				sumTR += trSeries[i];

			// 3. Range total sur la periode (plus haut - plus bas)
			double rangeTotal = maxHigh[0] - minLow[0];

			// 4. Choppiness Index = 100 * log10(sumTR / range) / log10(period)
			//    Borne haute (~100) = beaucoup d'agitation pour peu de progres = RANGE
			//    Borne basse (~0)   = peu d'agitation pour beaucoup de progres = TREND
			double chop = 50; // valeur neutre par defaut
			if (rangeTotal > 0 && sumTR > 0)
				chop = 100.0 * Math.Log10(sumTR / rangeTotal) / Math.Log10(Period);
			choppinessSeries[0] = chop;

			// 5. ATR en % du prix (volatilite normalisee)
			double atrPct = (Close[0] > 0) ? (atr[0] / Close[0] * 100.0) : 0;
			atrPctSeries[0] = atrPct;

			// 6. Plots
			Choppiness[0]	= chop;
			ADXValue[0]		= adx[0];
			ATRPercent[0]	= atrPct;

			// 7. Determination du regime
			BTCRegimeType regime = BTCRegimeType.Neutral;

			bool chopSaysTrend	= chop < ChoppinessTrendThreshold;
			bool chopSaysRange	= chop > ChoppinessRangeThreshold;
			bool adxSaysTrend	= adx[0] > ADXTrendThreshold;
			bool adxSaysRange	= adx[0] < ADXRangeThreshold;

			if (chopSaysTrend && adxSaysTrend)
				regime = BTCRegimeType.Trend;
			else if (chopSaysRange && adxSaysRange)
				regime = BTCRegimeType.Range;
			else
				regime = BTCRegimeType.Neutral;

			regimeSeries[0] = (int)regime;

			// 8. Coloration du fond
			//    - BackBrush  : panneau de l'indicateur
			//    - Draw.RegionHighlightX : bande verticale sur le panneau prix (full hauteur)
			if (ColorBackground)
			{
				Brush col = null;
				switch (regime)
				{
					case BTCRegimeType.Trend:	col = new SolidColorBrush(Color.FromArgb(40, 0, 200, 0));   break;
					case BTCRegimeType.Range:	col = new SolidColorBrush(Color.FromArgb(40, 220, 0, 0));   break;
				}
				BackBrush = col;

				string tag = "regHL_" + CurrentBar;
				if (col != null)
					Draw.RegionHighlightX(this, tag, 0, 0, col);
				else
					RemoveDrawObject(tag);
			}

			// 9. Label texte regime dans le coin (Draw.TextFixed avec tag unique = MAJ en place)
			if (ShowRegimeLabel)
			{
				string label;
				Brush col;
				switch (regime)
				{
					case BTCRegimeType.Trend:	label = "REGIME : TREND";   col = Brushes.LimeGreen;	break;
					case BTCRegimeType.Range:	label = "REGIME : RANGE";   col = Brushes.OrangeRed;	break;
					default:					label = "REGIME : NEUTRAL"; col = Brushes.Gray;			break;
				}
				label += string.Format("\nChop={0:F1}  ADX={1:F1}  ATR%={2:F2}", chop, adx[0], atrPct);
				Draw.TextFixed(this, "BTCRegimeLbl", label, TextPosition.TopRight, col, new SimpleFont("Consolas", 12), Brushes.Transparent, Brushes.Transparent, 0);
			}
		}

		#region Public access (pour strategies qui consomment le regime)
		public int RegimeValue(int barsAgo)
		{
			if (regimeSeries == null || barsAgo >= regimeSeries.Count) return 0;
			return regimeSeries[barsAgo];
		}
		public bool IsTrend(int barsAgo) { return RegimeValue(barsAgo) == 1; }
		public bool IsRange(int barsAgo) { return RegimeValue(barsAgo) == -1; }
		public bool IsNeutral(int barsAgo) { return RegimeValue(barsAgo) == 0; }
		#endregion

		#region Properties

		[NinjaScriptProperty]
		[Range(2, 200)]
		[Display(Name = "Period", Description = "Periode pour ATR / ADX / Choppiness (defaut 14)",
			Order = 1, GroupName = "1. Parametres")]
		public int Period { get; set; }

		[NinjaScriptProperty]
		[Range(50, 100)]
		[Display(Name = "Choppiness seuil RANGE", Description = "Au-dessus de ce seuil = marche en range (defaut 61.8)",
			Order = 2, GroupName = "1. Parametres")]
		public double ChoppinessRangeThreshold { get; set; }

		[NinjaScriptProperty]
		[Range(0, 50)]
		[Display(Name = "Choppiness seuil TREND", Description = "En-dessous de ce seuil = marche en tendance (defaut 38.2)",
			Order = 3, GroupName = "1. Parametres")]
		public double ChoppinessTrendThreshold { get; set; }

		[NinjaScriptProperty]
		[Range(10, 50)]
		[Display(Name = "ADX seuil TREND", Description = "Au-dessus de ce seuil ADX = vraie tendance (defaut 25)",
			Order = 4, GroupName = "1. Parametres")]
		public double ADXTrendThreshold { get; set; }

		[NinjaScriptProperty]
		[Range(5, 30)]
		[Display(Name = "ADX seuil RANGE", Description = "En-dessous de ce seuil ADX = pas de tendance (defaut 20)",
			Order = 5, GroupName = "1. Parametres")]
		public double ADXRangeThreshold { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Afficher label regime", Description = "Affiche le label REGIME en haut a droite",
			Order = 6, GroupName = "2. Affichage")]
		public bool ShowRegimeLabel { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Colorer fond panneau", Description = "Colore le fond du panneau (vert=trend, rouge=range)",
			Order = 7, GroupName = "2. Affichage")]
		public bool ColorBackground { get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Choppiness { get { return Values[0]; } }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> ADXValue { get { return Values[1]; } }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> ATRPercent { get { return Values[2]; } }

		#endregion
	}
}

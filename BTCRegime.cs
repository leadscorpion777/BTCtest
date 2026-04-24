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
		private MAX				maxHigh;
		private MIN				minLow;
		private Series<double>	trSeries;
		private Series<double>	choppinessSeries;
		private Series<int>		regimeSeries;
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= "BTCRegime - detecte si le marche est en TENDANCE, RANGE ou NEUTRE via Choppiness Index.";
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

				Period						= 14;
				ChoppinessRangeThreshold	= 61.8;
				ChoppinessTrendThreshold	= 38.2;
				ShowRegimeLabel				= true;
				ColorBackground				= true;

				AddPlot(new Stroke(Brushes.DodgerBlue, 2), PlotStyle.Line, "Choppiness");

				AddLine(new Stroke(Brushes.Red,		DashStyleHelper.Dash, 1), 61.8, "Seuil RANGE");
				AddLine(new Stroke(Brushes.Green,	DashStyleHelper.Dash, 1), 38.2, "Seuil TREND");
			}
			else if (State == State.DataLoaded)
			{
				maxHigh				= MAX(High, Period);
				minLow				= MIN(Low, Period);
				trSeries			= new Series<double>(this);
				choppinessSeries	= new Series<double>(this);
				regimeSeries		= new Series<int>(this);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < Period)
				return;

			// 1. True Range
			double tr;
			if (CurrentBar == 0)
				tr = High[0] - Low[0];
			else
				tr = Math.Max(High[0] - Low[0],
					  Math.Max(Math.Abs(High[0] - Close[1]),
							   Math.Abs(Low[0]  - Close[1])));
			trSeries[0] = tr;

			// 2. Somme TR
			double sumTR = 0;
			for (int i = 0; i < Period; i++)
				sumTR += trSeries[i];

			// 3. Range total
			double rangeTotal = maxHigh[0] - minLow[0];

			// 4. Choppiness
			double chop = 50;
			if (rangeTotal > 0 && sumTR > 0)
				chop = 100.0 * Math.Log10(sumTR / rangeTotal) / Math.Log10(Period);
			choppinessSeries[0] = chop;
			Choppiness[0] = chop;

			// 5. Regime (Choppiness uniquement)
			BTCRegimeType regime = BTCRegimeType.Neutral;
			if (chop < ChoppinessTrendThreshold)		regime = BTCRegimeType.Trend;
			else if (chop > ChoppinessRangeThreshold)	regime = BTCRegimeType.Range;

			regimeSeries[0] = (int)regime;

			// 6. Coloration sur tous panneaux
			if (ColorBackground)
			{
				Brush col = null;
				switch (regime)
				{
					case BTCRegimeType.Trend:	col = new SolidColorBrush(Color.FromArgb(110, 255, 140, 0));  break;	// Orange
					case BTCRegimeType.Range:	col = new SolidColorBrush(Color.FromArgb(110, 30, 144, 255)); break;	// Blue
				}
				BackBrushAll = col;
			}

			// 7. Label
			if (ShowRegimeLabel)
			{
				string label;
				Brush col;
				switch (regime)
				{
					case BTCRegimeType.Trend:	label = "REGIME : TREND";   col = Brushes.DarkOrange;	break;
					case BTCRegimeType.Range:	label = "REGIME : RANGE";   col = Brushes.DodgerBlue;	break;
					default:					label = "REGIME : NEUTRAL"; col = Brushes.Gray;			break;
				}
				label += string.Format("\nChop = {0:F1}", chop);
				Draw.TextFixed(this, "BTCRegimeLbl", label, TextPosition.TopRight, col, new SimpleFont("Consolas", 12), Brushes.Transparent, Brushes.Transparent, 0);
			}
		}

		#region Public access
		public int RegimeValue(int barsAgo)
		{
			if (regimeSeries == null || barsAgo >= regimeSeries.Count) return 0;
			return regimeSeries[barsAgo];
		}
		public bool IsTrend(int barsAgo)	{ return RegimeValue(barsAgo) == 1; }
		public bool IsRange(int barsAgo)	{ return RegimeValue(barsAgo) == -1; }
		public bool IsNeutral(int barsAgo)	{ return RegimeValue(barsAgo) == 0; }
		#endregion

		#region Properties

		[NinjaScriptProperty]
		[Range(2, 200)]
		[Display(Name = "Period", Description = "Periode Choppiness (defaut 14)",
			Order = 1, GroupName = "1. Parametres")]
		public int Period { get; set; }

		[NinjaScriptProperty]
		[Range(50, 100)]
		[Display(Name = "Choppiness seuil RANGE", Description = "Au-dessus = range (defaut 61.8)",
			Order = 2, GroupName = "1. Parametres")]
		public double ChoppinessRangeThreshold { get; set; }

		[NinjaScriptProperty]
		[Range(0, 50)]
		[Display(Name = "Choppiness seuil TREND", Description = "En-dessous = tendance (defaut 38.2)",
			Order = 3, GroupName = "1. Parametres")]
		public double ChoppinessTrendThreshold { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Afficher label regime", Description = "Label coin sup. droit",
			Order = 4, GroupName = "2. Affichage")]
		public bool ShowRegimeLabel { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Colorer fond", Description = "Bandes verticales sur prix + indicateur",
			Order = 5, GroupName = "2. Affichage")]
		public bool ColorBackground { get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Choppiness { get { return Values[0]; } }

		#endregion
	}
}

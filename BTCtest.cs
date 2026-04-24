#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Strategies
{
	public class BTCtest : Strategy
	{
		#region Variables
		// TODO : variables strategie
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= "BTCtest - squelette de travail (logique a coder).";
				Name						= "BTCtest";
				Calculate					= Calculate.OnBarClose;
				EntriesPerDirection			= 1;
				EntryHandling				= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy = false;
				IsFillLimitOnTouch			= false;
				MaximumBarsLookBack			= MaximumBarsLookBack.Infinite;
				StartBehavior				= StartBehavior.WaitUntilFlat;
				IsInstantiatedOnEachOptimizationIteration = true;

				DefaultQuantity				= 1;
			}
			else if (State == State.Configure)
			{
				// TODO : AddDataSeries / Indicators
			}
			else if (State == State.DataLoaded)
			{
				// TODO : init indicateurs
			}
			else if (State == State.Terminated)
			{
				if (SystemPerformance != null && SystemPerformance.AllTrades.Count > 0)
					ExportStats();
			}
		}

		protected override void OnBarUpdate()
		{
			// TODO : logique d'entree / sortie
		}

		#region Auto-export stats (Parallels-friendly via UserDataDir)
		private void ExportStats()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("========================================");
			sb.AppendLine("BTCtest - Backtest stats");
			sb.AppendLine("Date         : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
			sb.AppendLine("Instrument   : " + (Instrument != null ? Instrument.FullName : "?"));
			sb.AppendLine("Bars         : " + (Bars != null ? Bars.BarsPeriod.ToString() : "?"));
			sb.AppendLine("========================================");

			var all			= SystemPerformance.AllTrades;
			var perf		= all.TradesPerformance;
			int nTrades		= all.Count;
			int nWins		= all.WinningTrades.Count;
			int nLosses		= all.LosingTrades.Count;
			double winsPnL	= all.WinningTrades.TradesPerformance.Currency.CumProfit;
			double lossPnL	= all.LosingTrades.TradesPerformance.Currency.CumProfit;
			double pf		= (lossPnL != 0) ? winsPnL / Math.Abs(lossPnL) : 0;
			double winrate	= (nTrades > 0) ? (double)nWins / nTrades * 100.0 : 0;

			// Max DD calcule a la main depuis equity curve (pas de propriete fiable sur TradesPerformanceValues)
			double maxDD = 0;
			double peak = 0;
			double equity = 0;
			for (int i = 0; i < nTrades; i++)
			{
				equity += all[i].ProfitCurrency;
				if (equity > peak) peak = equity;
				double dd = peak - equity;
				if (dd > maxDD) maxDD = dd;
			}

			sb.AppendLine("Trades       : " + nTrades + " (W " + nWins + " / L " + nLosses + ")");
			sb.AppendLine("Net PnL      : " + perf.Currency.CumProfit.ToString("F2") + " $");
			sb.AppendLine("Profit Factor: " + pf.ToString("F2"));
			sb.AppendLine("Max DD       : " + maxDD.ToString("F2") + " $");
			sb.AppendLine("Winrate      : " + winrate.ToString("F1") + " %");
			sb.AppendLine("Avg Trade    : " + perf.Currency.AverageProfit.ToString("F2") + " $");
			sb.AppendLine("========================================");

			Print("[BTCtest LOG] UserDataDir = " + NinjaTrader.Core.Globals.UserDataDir);
			try
			{
				string logPath = System.IO.Path.Combine(NinjaTrader.Core.Globals.UserDataDir, "last_backtest_BTCtest.log");
				System.IO.File.WriteAllText(logPath, sb.ToString());
				Print("[BTCtest LOG] Stats ecrites dans : " + logPath);
			}
			catch (Exception ex)
			{
				Print("[BTCtest LOG] Echec ecriture : " + ex.Message);
			}
		}
		#endregion

		#region Properties
		// TODO : NinjaScriptProperty params
		#endregion
	}
}

# BTCtest — Contexte Strategie NinjaTrader — MAJ 2026-04-24

## Objectif
Projet de **test BTC** sur futures CME — squelette parallele aux projets MomentumFVG (BTC / ETH / Gold).
Logique a coder ensuite. Mise en place de la structure (Mac / NT / GitHub / log auto) avant le code.

## Utilisateur
- Trader FR resident au **Cambodge** (Siem Reap)
- C#/NinjaScript
- Setup dev : Mac + Parallels Windows (NinjaTrader cote Windows)
- Capital trading : **10 000$**

## Setup NinjaTrader
- **Instrument cible** : **MBT** (Micro Bitcoin, 0.1 BTC, contrat CME)
  - Tick : 5$/tick (a verifier dans NT)
  - Marge overnight ~1 100$ (~11% du capital 10k)
- **Timeframe** : a definir (4h par defaut, alignement avec autres MomentumFVG)
- **DefaultQuantity** : 1 contrat
- **Capital backtest** : 10 000$
- **Periode backtest** : a definir
- **Session template** : **Default 24 x 5** (avant 29/05/2026), **Cryptomonnaie** apres
- **"Arret en fin de journee"** : **DECOCHE** (regle generale always-in)

## Strategie
A coder. Squelette pose dans `BTCtest.cs` (classe `BTCtest`, NinjaScript `Strategy`).

### Defaults squelette
- `Calculate.OnBarClose`
- `StartBehavior.WaitUntilFlat`
- `MaximumBarsLookBack = Infinite`
- `IsExitOnSessionCloseStrategy = false`
- `DefaultQuantity = 1`

## Auto-export stats
- Fichier : `C:\Mac\Home\Documents\NinjaTrader 8\last_backtest_BTCtest.log`
- Ecrit via `NinjaTrader.Core.Globals.UserDataDir` (compat Parallels)
- Update auto a chaque backtest (`State.Terminated`)
- Contenu : Trades / PnL / PF / DD / Winrate / Avg trade

## Workflow technique
- Code edite sur Mac : `/Users/lead_scorpion/Desktop/Claude/BTCtest/`
- Copie vers `/Users/lead_scorpion/Documents/NinjaTrader 8/bin/Custom/Strategies/BTCtest.cs`
- NinjaTrader recompile auto (F5 pour forcer)
- Git repo : https://github.com/leadscorpion777/BTCtest

## Parametres NinjaTrader importants (rappel)
- Calcul a la fermeture de la barre (OnBarClose)
- BarsMax = Infini
- Comportement depart : "Attendre d'etre a plat"
- Sortie sur fermeture de session : DECOCHE
- Arret en fin de journee : DECOCHE
- Jours a charger : suffisant pour les warmup d'indicateurs

## A faire / questions ouvertes
- [ ] Definir la logique de trading (MR ? Breakout ? autre ?)
- [ ] Definir le timeframe de travail
- [ ] Definir la periode de backtest
- [ ] Premier backtest baseline une fois le code pose

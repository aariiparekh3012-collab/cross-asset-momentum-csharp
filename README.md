# Defensive Cross-Asset Momentum

A defensive cross-asset momentum strategy implemented in C# using the QuantConnect LEAN engine.

**Published strategy:** [View on QuantConnect](https://www.quantconnect.com/strategies/732/Defensive-Cross-Asset-Momentum)

## Overview

The strategy allocates capital across three liquid ETFs representing different macroeconomic asset classes:

| ETF | Exposure                    |
| --- | --------------------------- |
| SPY | US equities                 |
| TLT | Long-term US Treasury bonds |
| GLD | Gold                        |

The objective is to participate in persistent price trends while reducing exposure to assets experiencing negative momentum.

## Strategy Logic

On the first and last trading day of each month, the algorithm:

1. Retrieves 126 trading days of price history.
2. Calculates each asset's trailing return and annualized volatility.
3. Removes assets with negative momentum.
4. Ranks the remaining assets by momentum.
5. Selects the two strongest assets.
6. Allocates using inverse-volatility weights.
7. Moves entirely to cash if no asset has positive momentum.

The strategy uses daily data and executes its scheduled calculations at 8:00 AM.

## Backtest Results

Backtest period: **May 25, 2021 to May 25, 2026**

| Metric                    |    Strategy |
| ------------------------- | ----------: |
| Initial capital           |    $100,000 |
| Final portfolio value     | $182,624.96 |
| Net return                |      82.63% |
| Compounding annual return |      12.81% |
| Maximum drawdown          |      17.10% |
| Sharpe ratio              |       0.487 |
| Sortino ratio             |       0.548 |
| Completed trades          |         111 |
| Total fees                |     $154.82 |

Over the same period, SPY returned approximately 13.78% annually and experienced a maximum drawdown of approximately 24.50%. The strategy therefore underperformed SPY on absolute return but achieved a smaller drawdown.

## Development Process

The final model was selected through controlled iteration rather than presenting only the best result.

| Version | Change                                | Annual Return | Drawdown | Decision   |
| ------- | ------------------------------------- | ------------: | -------: | ---------- |
| V1      | SPY, TLT and GLD; monthly rebalancing |        11.12% |   21.50% | Baseline   |
| V2      | Expanded eight-asset universe         |         7.44% |   24.30% | Rejected   |
| V3      | Five-year publication adaptation      |        11.99% |   17.20% | Interim    |
| V4      | Full five-year monthly test           |        12.15% |   17.20% | Superseded |
| V5      | Twice-monthly rebalancing             |        12.81% |   17.10% | Selected   |

The expanded universe in V2 increased turnover and fees while reducing both absolute and risk-adjusted performance. This result demonstrates that adding more assets does not automatically improve diversification or strategy quality.

## Technology

* C#
* QuantConnect LEAN
* Daily ETF market data
* Scheduled portfolio rebalancing
* Historical-data requests
* Momentum ranking
* Inverse-volatility portfolio construction

## Running the Strategy

1. Create a C# project in QuantConnect.
2. Replace the generated algorithm with [`Main.cs`](Main.cs).
3. Build the project.
4. Run a backtest from May 25, 2021 to May 25, 2026.

## Limitations

* Results are based on a historical backtest, not live trading.
* The investment universe contains only three ETFs.
* Transaction fees are modeled, but real execution may include additional slippage and costs.
* The strategy underperformed a passive SPY investment on absolute return during the tested period.
* Historical results do not guarantee future performance.

## Disclaimer

This project is for educational and research purposes only and does not constitute investment advice.

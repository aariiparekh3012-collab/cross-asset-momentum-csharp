#region imports
using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect;
using QuantConnect.Util;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Scheduling;
using QuantConnect.Securities;
#endregion

public class CrossAssetMomentumAlgorithm : QCAlgorithm
{
    private const int LookbackDays = 126;
    private const int NumberOfAssetsToHold = 2;

    private readonly List<Symbol> _assets = new();
    private Symbol _spy;

    public override void Initialize()
    {
        // Latest five-year publication window
        SetStartDate(2021, 5, 25);
        SetEndDate(2026, 5, 25);
        SetCash(100000);

        _spy = AddEquity("SPY", Resolution.Daily).Symbol;
        _assets.Add(_spy);
        _assets.Add(AddEquity("TLT", Resolution.Daily).Symbol);
        _assets.Add(AddEquity("GLD", Resolution.Daily).Symbol);

        SetBenchmark(_spy);

        // Load historical data before trading begins
        SetWarmUp(LookbackDays, Resolution.Daily);

        // QuantConnect requests 8 AM for published daily strategies
        Schedule.On(
            DateRules.MonthStart(),
            TimeRules.At(8, 0),
            Rebalance
        );
        Schedule.On(
            DateRules.MonthEnd(),
            TimeRules.At(8, 0),
            Rebalance
            );
    }

    public override void OnWarmupFinished()
    {
        // Avoid an initially flat equity curve
        Rebalance();
    }

    public override void OnData(Slice data)
    {
        // This strategy makes decisions monthly.
    }

    private void Rebalance()
    {
        if (IsWarmingUp)
        {
            return;
        }

        var candidates = new List<AssetScore>();

        foreach (var symbol in _assets)
        {
            var bars = History<TradeBar>(
                    symbol,
                    LookbackDays,
                    Resolution.Daily
                )
                .OrderBy(bar => bar.EndTime)
                .ToList();

            if (bars.Count < LookbackDays)
            {
                continue;
            }

            var firstPrice = bars[0].Close;
            var lastPrice = bars[bars.Count - 1].Close;

            if (firstPrice <= 0)
            {
                continue;
            }

            var momentum =
                (double)(lastPrice / firstPrice - 1m);

            var dailyReturns = bars
                .Zip(
                    bars.Skip(1),
                    (previous, current) =>
                        (double)(
                            current.Close /
                            previous.Close - 1m
                        )
                )
                .ToList();

            var volatility =
                AnnualizedVolatility(dailyReturns);

            // Absolute-momentum filter
            if (momentum > 0 && volatility > 0)
            {
                candidates.Add(
                    new AssetScore(
                        symbol,
                        momentum,
                        volatility
                    )
                );
            }
        }

        // Relative-momentum selection
        var selected = candidates
            .OrderByDescending(asset => asset.Momentum)
            .Take(NumberOfAssetsToHold)
            .ToList();

        // Sell assets that have left the selection
        foreach (var symbol in _assets)
        {
            var remainsSelected = selected.Any(
                asset => asset.Symbol == symbol
            );

            if (
                Portfolio[symbol].Invested &&
                !remainsSelected
            )
            {
                Liquidate(symbol);
            }
        }

        // Remain in cash if every asset has negative momentum
        if (selected.Count == 0)
        {
            Liquidate();

            Debug(
                $"{Time:yyyy-MM-dd}: No eligible assets"
            );

            return;
        }

        // Allocate more capital to lower-volatility assets
        var totalInverseVolatility = selected.Sum(
            asset => 1.0 / asset.Volatility
        );

        foreach (var asset in selected)
        {
            var weight =
                (1.0 / asset.Volatility) /
                totalInverseVolatility;

            SetHoldings(
                asset.Symbol,
                (decimal)weight
            );
        }

        Debug(
            $"{Time:yyyy-MM-dd}: " +
            string.Join(
                ", ",
                selected.Select(
                    asset =>
                        $"{asset.Symbol.Value} " +
                        $"momentum={asset.Momentum:P1}"
                )
            )
        );
    }

    private static double AnnualizedVolatility(
        IReadOnlyList<double> returns)
    {
        if (returns.Count < 2)
        {
            return 0;
        }

        var average = returns.Average();

        var variance = returns.Sum(
            value => Math.Pow(value - average, 2)
        ) / (returns.Count - 1);

        return Math.Sqrt(variance) * Math.Sqrt(252);
    }

    private sealed class AssetScore
    {
        public Symbol Symbol { get; }
        public double Momentum { get; }
        public double Volatility { get; }

        public AssetScore(
            Symbol symbol,
            double momentum,
            double volatility)
        {
            Symbol = symbol;
            Momentum = momentum;
            Volatility = volatility;
        }
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Samples.AggregatedRates.Scenarios;

namespace Bodu.Financial.Samples.AggregatedRates;

/// <summary>
/// Entry point for the aggregated-rates sample: grouping several rate sources behind one entry point
/// with priority fallback, averaging, per-pair routing, and the DI builder — against two offline feeds.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Financial.Samples.AggregatedRates");
        Console.WriteLine("======================================");
        Console.WriteLine();

        // Two offline feeds with deliberately complementary coverage stand in for two live sources.
        //
        // --- To aggregate live central-bank feeds instead ---------------------------
        // 1. dotnet add package Bodu.Financial.ExchangeRates.Ecb   (and/or .Boe, .Rba, ...)
        // 2. In the DI scenario, register the providers and use the generic child form:
        //
        //     services.AddFinancialService()
        //             .AddEcbExchangeRates()
        //             .AddRbaExchangeRates()
        //             .AddAggregatedRateProvider(agg => agg
        //                 .AddCachedChild<EcbRateProvider>("ECB")
        //                 .AddCachedChild<RbaRateProvider>("RBA"));
        // ----------------------------------------------------------------------------

        PriorityFallback.Run();
        Averaging.Run();
        PerPairRouting.Run();
        DiComposition.Run();

        Console.WriteLine("Done.");
    }
}

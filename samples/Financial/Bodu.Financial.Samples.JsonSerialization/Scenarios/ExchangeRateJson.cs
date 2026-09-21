// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateJson.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;
using Bodu.Financial.Serialization.Json;

namespace Bodu.Financial.Samples.JsonSerialization.Scenarios;

/// <summary>
/// Demonstrates round-tripping the two exchange-rate value types registered by
/// <see cref="FinancialJsonSerializerOptionsExtensions.AddFinancialJsonConverters" />:
/// <see cref="ExchangeRate" /> (a dated, sourced multiplier) and <see cref="CurrencyPair" /> (the ordered
/// from/to identity). Both are constructed from fixed inputs so the output never varies between runs.
/// </summary>
public static class ExchangeRateJson
{
    /// <summary>
    /// Serializes and deserializes a fixed exchange rate and currency pair under the Strict policy.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "ExchangeRate and CurrencyPair round-trips",
            what: "Serializes rates and pairs, reads them back, and checks that both the value and the "
                + "direction survive.",
            why: "A rate is a directed quantity and the direction is the part that gets lost. USD/EUR and "
                + "EUR/USD are reciprocals, so a serialized rate that does not carry its pair unambiguously can "
                + "be read back inverted - and an inverted rate does not look wrong, it looks like a different "
                + "market. Making the pair part of the wire form rather than something the reader infers is what "
                + "prevents that, and it is why the pair has its own converter rather than being flattened into "
                + "two loose currency codes.",
            expect: "The rate comes back with the same value and the same direction, so a round trip cannot "
                + "silently invert it.");

        var options = new JsonSerializerOptions().AddFinancialJsonConverters();

        // A fixed, fully-specified rate: fixed date and provider keep the output deterministic.
        var rate = new ExchangeRate(CurrencyCode.USD, CurrencyCode.JPY, new DateOnly(2024, 3, 15), 148.25m, "SampleFeed");
        var rateJson = JsonSerializer.Serialize(rate, options);
        ExchangeRate rateBack = JsonSerializer.Deserialize<ExchangeRate>(rateJson, options);
        Console.WriteLine($"  ExchangeRate : {rateJson}");
        Console.WriteLine($"             -> {rateBack.From}/{rateBack.To} @ {rateBack.Rate} on {rateBack.Date:yyyy-MM-dd} [{rateBack.Provider}]");

        // CurrencyPair is just the ordered identity - Strict emits {"from":..,"to":..}.
        var pair = new CurrencyPair(CurrencyCode.EUR, CurrencyCode.USD);
        var pairJson = JsonSerializer.Serialize(pair, options);
        CurrencyPair pairBack = JsonSerializer.Deserialize<CurrencyPair>(pairJson, options);
        Console.WriteLine($"  CurrencyPair : {pairJson}");
        Console.WriteLine($"             -> {pairBack.From}/{pairBack.To} (round-trips equal: {pair == pairBack})");

        Console.WriteLine();
    }
}

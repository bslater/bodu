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
        Console.WriteLine("--- ExchangeRate and CurrencyPair round-trips ---");

        var options = new JsonSerializerOptions().AddFinancialJsonConverters();

        // A fixed, fully-specified rate: fixed date and provider keep the output deterministic.
        var rate = new ExchangeRate(CurrencyCode.USD, CurrencyCode.JPY, new DateOnly(2024, 3, 15), 148.25m, "SampleFeed");
        var rateJson = JsonSerializer.Serialize(rate, options);
        ExchangeRate rateBack = JsonSerializer.Deserialize<ExchangeRate>(rateJson, options);
        Console.WriteLine($"ExchangeRate : {rateJson}");
        Console.WriteLine($"             -> {rateBack.From}/{rateBack.To} @ {rateBack.Rate} on {rateBack.Date:yyyy-MM-dd} [{rateBack.Provider}]");

        // CurrencyPair is just the ordered identity - Strict emits {"from":..,"to":..}.
        var pair = new CurrencyPair(CurrencyCode.EUR, CurrencyCode.USD);
        var pairJson = JsonSerializer.Serialize(pair, options);
        CurrencyPair pairBack = JsonSerializer.Deserialize<CurrencyPair>(pairJson, options);
        Console.WriteLine($"CurrencyPair : {pairJson}");
        Console.WriteLine($"             -> {pairBack.From}/{pairBack.To} (round-trips equal: {pair == pairBack})");

        Console.WriteLine();
    }
}

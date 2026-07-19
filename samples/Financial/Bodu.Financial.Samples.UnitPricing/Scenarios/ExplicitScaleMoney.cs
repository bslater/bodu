// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExplicitScaleMoney.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;
using Bodu.Financial.Serialization.Json;

namespace Bodu.Financial.Samples.UnitPricing.Scenarios;

/// <summary>
/// Demonstrates carrying a <see cref="Money" /> at a precision beyond its currency's registered minor units - a
/// six-decimal-place share price in a two-decimal currency - and round-tripping that precision through JSON. A plain
/// <see cref="Money" /> settles to the currency's two places on construction; a value materialized through a
/// custom-scale <see cref="MonetaryContext" /> keeps its explicit scale, and the Strict JSON shape records that scale
/// on the wire so it survives deserialization.
/// </summary>
public static class ExplicitScaleMoney
{
    /// <summary>
    /// Settles a six-place unit price, serializes it, and reads it back showing the extra places are retained.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Explicit-scale Money: a 6-dp share price ---");

        // A quoted equity price with six decimal places.
        decimal quoted = 145.678912m;

        // A plain Money is settlement-grade: it rounds to the currency's registered minor units (USD = 2) on
        // construction, so the extra four places are lost immediately.
        Money settled = new Money(quoted, CurrencyCode.USD);
        Console.WriteLine($"Plain Money (settles to 2 dp) : {settled.ToString("R")}  (MinorUnits = {settled.MinorUnits})");

        // To keep six places, defer rounding in a CalculatedMoney and settle through a context that requests a custom
        // scale. The resulting Money reports six minor units and formats to six places.
        var priceContext = MonetaryContext.Default with { ScalePolicy = ScalePolicy.Custom, CustomScale = 6 };
        Money unitPrice = new CalculatedMoney(quoted, CurrencyCode.USD).RoundToMoney(priceContext);
        Console.WriteLine($"Unit-price Money (6 dp)       : {unitPrice.ToString("R")}  (MinorUnits = {unitPrice.MinorUnits})");

        // Register the converters and serialize. The Strict object shape adds a "scale" property whenever a value's
        // precision differs from its currency's registered minor units, so the six places are recorded on the wire.
        var options = new JsonSerializerOptions { WriteIndented = false }.AddFinancialJsonConverters(FinancialJsonPolicy.Strict);

        string settledJson = JsonSerializer.Serialize(settled, options);
        string unitPriceJson = JsonSerializer.Serialize(unitPrice, options);
        Console.WriteLine($"Plain Money JSON              : {settledJson}");
        Console.WriteLine($"Unit-price JSON               : {unitPriceJson}");

        // Deserialize and confirm the six-place precision came back intact.
        Money restored = JsonSerializer.Deserialize<Money>(unitPriceJson, options);
        Console.WriteLine($"Deserialized unit price       : {restored.ToString("R")}  (MinorUnits = {restored.MinorUnits})");

        // Trailing zeros are part of "how many decimal places": a price of exactly 12.5 held at six places round-trips
        // so that it still reports six fractional digits, even though the stored decimal has none to spare.
        Money halfDollar = new CalculatedMoney(12.5m, CurrencyCode.USD).RoundToMoney(priceContext);
        Money halfDollarRestored = JsonSerializer.Deserialize<Money>(JsonSerializer.Serialize(halfDollar, options), options);
        Console.WriteLine($"Trailing zeros preserved      : {JsonSerializer.Serialize(halfDollar, options)} -> {halfDollarRestored.ToString("R")}");

        Console.WriteLine();
    }
}

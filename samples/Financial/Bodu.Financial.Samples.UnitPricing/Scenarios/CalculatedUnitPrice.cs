// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedUnitPrice.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;
using Bodu.Financial.Currencies;
using Bodu.Financial.Serialization.Json;

namespace Bodu.Financial.Samples.UnitPricing.Scenarios;

/// <summary>
/// Demonstrates <see cref="CalculatedMoney" /> as the wire form for an un-settled, full-precision unit price. Because a
/// <see cref="CalculatedMoney" /> is never rounded on construction, its decimal already carries every significant digit
/// (and any trailing zeros), so the JSON converter writes and reads it verbatim - no scale metadata is needed. The
/// value settles to cash exactly once, after transport, when a line total is computed.
/// </summary>
public static class CalculatedUnitPrice
{
    /// <summary>
    /// Round-trips a high-precision unit price as <see cref="CalculatedMoney" />, then settles a line total once.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- CalculatedMoney: unrounded unit price on the wire ---");

        var options = new JsonSerializerOptions().AddFinancialJsonConverters(FinancialJsonPolicy.Strict);

        // A per-unit rate quoted to more places than the currency settles at.
        var unitPrice = new CalculatedMoney(0.0325125m, CurrencyCode.USD);

        string json = JsonSerializer.Serialize(unitPrice, options);
        CalculatedMoney restored = JsonSerializer.Deserialize<CalculatedMoney>(json, options);

        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Unit price (unrounded) : {0} USD", unitPrice.Amount));
        Console.WriteLine($"Serialized             : {json}");
        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Deserialized           : {0} USD", restored.Amount));

        // Multiply by a quantity while still unrounded, then settle to the currency's minor units exactly once - the
        // single rounding decision happens at settlement, not on every intermediate value.
        long quantity = 40_000;
        CalculatedMoney lineTotal = restored * quantity;
        Money settled = lineTotal.RoundToMoney();

        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "x {0:N0} units (unrounded): {1} USD", quantity, lineTotal.Amount));
        Console.WriteLine($"Settled line total     : {settled.ToString("R")}");

        Console.WriteLine();
    }
}

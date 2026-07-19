// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PriceListDocument.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;
using Bodu.Financial.Serialization.Json;

namespace Bodu.Financial.Samples.UnitPricing.Scenarios;

/// <summary>
/// Demonstrates high-precision unit prices inside a realistic object graph: a portfolio of holdings, each carrying a
/// six-decimal-place <see cref="Money" /> share price. Serializing the document and reading it back preserves every
/// price's precision through the nested POCO, after which each position settles to a two-decimal cash value.
/// </summary>
public static class PriceListDocument
{
    /// <summary>
    /// Represents one portfolio line: a ticker, a share count, and a high-precision unit price.
    /// </summary>
    /// <param name="Ticker">The instrument symbol.</param>
    /// <param name="Shares">The number of shares held.</param>
    /// <param name="UnitPrice">The per-share price, carried at unit-pricing precision.</param>
    public sealed record Holding(string Ticker, long Shares, Money UnitPrice);

    /// <summary>
    /// Serializes a portfolio of six-decimal-place prices, reads it back, and settles each position.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Price-list document: 6-dp prices inside a POCO ---");

        var priceContext = MonetaryContext.Default with { ScalePolicy = ScalePolicy.Custom, CustomScale = 6 };
        Money Price(decimal quoted) => new CalculatedMoney(quoted, CurrencyCode.USD).RoundToMoney(priceContext);

        var portfolio = new[]
        {
            new Holding("ACME", 1_250, Price(145.678912m)),
            new Holding("GLOBEX", 800, Price(12.500000m)),
            new Holding("INITECH", 3_400, Price(7.049999m)),
        };

        var options = new JsonSerializerOptions { WriteIndented = true }.AddFinancialJsonConverters(FinancialJsonPolicy.Strict);

        string json = JsonSerializer.Serialize(portfolio, options);
        Console.WriteLine(json);

        Holding[] restored = JsonSerializer.Deserialize<Holding[]>(json, options)!;

        Console.WriteLine("Settled positions (unit price kept at 6 dp, position settled to 2 dp):");
        foreach (Holding holding in restored)
        {
            // Multiply the exact per-share price by the share count without intermediate rounding, then settle once.
            Money position = (new CalculatedMoney(holding.UnitPrice.Amount, holding.UnitPrice.Code) * holding.Shares).RoundToMoney();
            Console.WriteLine($"  {holding.Ticker,-8} {holding.Shares,6:N0} @ {holding.UnitPrice.ToString("R"),-16} = {position.ToString("R")}");
        }

        Console.WriteLine();
    }
}

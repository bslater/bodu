// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyBagLedger.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;

namespace Bodu.Financial.Samples.MoneyBasics.Scenarios;

/// <summary>
/// Demonstrates <see cref="MoneyBag" /> as an immutable multi-currency ledger: accumulate amounts
/// per currency, read balances back, and convert the whole bag to one reporting currency — with a
/// full per-line audit trail when the conversion must be explainable.
/// </summary>
public static class MoneyBagLedger
{
    /// <summary>
    /// Builds a small ledger, prints its balances, and converts it to AUD twice (simple and audited).
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- MoneyBag: a multi-currency ledger ---");

        // A bag keeps one running balance per currency. Every operation returns a new bag, so a
        // ledger snapshot can be handed out without defensive copying.
        MoneyBag ledger = MoneyBag.Of(
            Money.From(1500.00m, CurrencyCode.AUD),
            Money.From(250.75m, CurrencyCode.USD));

        ledger = ledger.Add(Money.Of<EUR>(89.10m));          // typed and runtime amounts mix freely
        ledger += Money.From(120.00m, CurrencyCode.USD);     // operators merge into the currency's balance
        ledger -= Money.From(50.00m, CurrencyCode.AUD);

        Console.WriteLine("Balances:");
        foreach (Money balance in ledger)
            Console.WriteLine($"  {balance}");

        Money<USD>? usdBalance = ledger.GetBalance<USD>();
        Console.WriteLine($"USD balance      : {usdBalance}");

        // Simple conversion: any (from, to) -> rate delegate works, e.g. treasury rates from
        // configuration. Identity pairs (AUD -> AUD here) are part of the contract.
        Money<AUD> quickTotal = ledger.ConvertTo<AUD>((from, to) => (from, to) switch
        {
            ("AUD", "AUD") => 1m,
            ("USD", "AUD") => 1.5230m,
            ("EUR", "AUD") => 1.6310m,
            _ => throw new KeyNotFoundException($"No rate {from}->{to}"),
        });
        Console.WriteLine($"Total (delegate) : {quickTotal}");

        // Audited conversion: a dated provider supplies the rates, and the result carries one line
        // per source currency - amount, resolved rate (with provenance), and the raw converted
        // value before the final rounding policy - so the total is fully explainable.
        var valueDate = new DateOnly(2024, 3, 15);
        var builder = new RateTableBuilder();
        builder.Upsert(new CurrencyPair(CurrencyCode.USD, CurrencyCode.AUD), "Treasury", valueDate, 1.5230m);
        builder.Upsert(new CurrencyPair(CurrencyCode.EUR, CurrencyCode.AUD), "Treasury", valueDate, 1.6310m);
        var provider = new FixedDatedRateProvider(builder.ToBook());

        MoneyBagConversionAudit<AUD> audit = ledger.ConvertToWithAudit<AUD>(
            provider, valueDate, RateLookupOptions.PreviousWithin(3));

        Console.WriteLine($"Total (audited)  : {audit.Total}");
        foreach (MoneyBagConversionLine line in audit.Lines)
        {
            // The target currency's own balance needs no rate; its audit line carries Rate = null.
            var rate = line.Rate is { } r ? $"{r.Rate.Rate} [{r.Rate.Provider}]" : "1 (identity)";
            Console.WriteLine($"  {line.SourceIsoCode} {line.SourceAmount,10} x {rate,-20} = {line.RawConvertedAmount}");
        }

        Console.WriteLine();
    }
}

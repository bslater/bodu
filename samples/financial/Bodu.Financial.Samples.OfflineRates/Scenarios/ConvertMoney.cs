// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConvertMoney.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;
using Bodu.Financial.Extensions;

namespace Bodu.Financial.Samples.OfflineRates.Scenarios;

/// <summary>
/// Demonstrates converting money through a dated provider: the one-call extension form, the explicit
/// lookup-then-convert form with a typed <see cref="ExchangeRate{TBase, TQuote}" />, and the runtime
/// (<see cref="Money" />) form for currencies only known at run time.
/// </summary>
public static class ConvertMoney
{
    /// <summary>
    /// Converts an AUD invoice amount to USD and EUR using rates from the offline provider.
    /// </summary>
    /// <param name="source">The dated provider supplying the rates.</param>
    public static void Run(IDatedRateProvider source)
    {
        Console.WriteLine("--- Converting money with dated rates ---");

        var valueDate = new DateOnly(2024, 3, 15);
        Money<AUD> invoice = Money.Of<AUD>(2499.95m);
        Console.WriteLine($"Invoice: {invoice} (value date {valueDate:yyyy-MM-dd})");

        // One call: look up the (AUD -> USD) rate for the value date and convert, rounding to the
        // target currency's minor units. PreviousWithin(5) tolerates weekends and holidays.
        Money<USD> usd = invoice.ConvertTo<AUD, USD>(source, valueDate, RateLookupOptions.PreviousWithin(5));
        Console.WriteLine($"  -> {usd}  (ConvertTo extension)");

        // Explicit two-step form: fetch the lookup result (rate + provenance), bridge it into the
        // strongly typed ExchangeRate<AUD, EUR>, then convert. FromRuntime validates that the runtime
        // rate really is AUD -> EUR, so a wiring mistake fails fast instead of converting wrongly.
        RateLookupResult lookup = source.GetRate("AUD", "EUR", valueDate, RateLookupOptions.PreviousWithin(5));
        var typedRate = ExchangeRate<AUD, EUR>.FromRuntime(lookup.Rate);
        Money<EUR> eur = invoice.Convert(typedRate);
        Console.WriteLine($"  -> {eur}  (typed rate {typedRate.Rate}, observed {typedRate.Date:yyyy-MM-dd})");

        // Runtime form: when the target currency is only known at run time (user input, configuration),
        // work with Money + ISO codes instead of the generic types. Same provider, same options.
        Money runtimeInvoice = invoice;
        Money jpy = runtimeInvoice.ConvertTo(source, "JPY", valueDate, RateLookupOptions.PreviousWithin(5));
        Console.WriteLine($"  -> {jpy}  (runtime Money, target chosen at run time)");

        Console.WriteLine();
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;
using Bodu.Financial.ExchangeRates.Caching;
using Bodu.Financial.Extensions;

namespace Bodu.Financial.Samples.CustomProvider;

/// <summary>
/// Entry point for the custom-provider sample: a consumer-written <see cref="CsvFileRateProvider" />
/// used directly, through the money-conversion extensions, and composed under the caching decorator —
/// proving a custom source is a first-class citizen of the stack.
/// </summary>
public static class Program
{
    /// <summary>
    /// Exercises the custom provider end to end.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Financial.Samples.CustomProvider");
        Console.WriteLine("=====================================");
        Console.WriteLine();

        SampleConsole.Scenario(
            "A custom rate provider over a CSV file",
            what: "Reads rates from a committed CSV through a hand-written provider, then exercises it: an exact "
                + "lookup, the inverse pair, a weekend date resolved backwards, the declared history horizon, a "
                + "money conversion, and the same provider under the shipped caching decorator.",
            why: "Rates do not only come from web feeds. A treasury department publishes a spreadsheet, a "
                + "regulator distributes a file, a test needs a fixed table - and none of those should require "
                + "reimplementing date resolution, inverse-pair derivation or identity rates, which is where a "
                + "bespoke provider usually goes wrong. Delegating to the shipped dated-rate provider means the "
                + "custom code is only the part that is genuinely custom: parsing the file. Implementing the "
                + "library interface rather than a bespoke one is what lets the result compose with the caching "
                + "and aggregating decorators exactly like a web provider would.",
            expect: "The inverse pair and the weekend resolution work without any code here doing either - both "
                + "come from the delegated provider. Declaring the history horizon lets callers reason about what "
                + "this file can answer before asking. The conversion and the caching decorator both accept the "
                + "custom provider through the plain interface, which is the payoff for implementing it.");

        var path = Path.Combine(AppContext.BaseDirectory, "Data", "custom-feed.csv");
        var provider = new CsvFileRateProvider(path, "CustomFeed");

        // Direct lookups - the delegated FixedDatedRateProvider supplies date resolution, inverse
        // fallback, and identity rates without any custom code.
        RateLookupResult exact = provider.GetRate("AUD", "USD", new DateOnly(2024, 1, 15));
        Console.WriteLine($"  AUD/USD exact    : {exact.Rate.Rate} [{exact.Rate.Provider}]");

        RateLookupResult inverse = provider.GetRate("USD", "AUD", new DateOnly(2024, 1, 15));
        Console.WriteLine($"  USD/AUD inverse  : {inverse.Rate.Rate} (derived from the AUD/USD observation)"
            + "  (the CSV holds only AUD/USD - the delegated provider derived this, so the custom code never implements inversion)");

        RateLookupResult weekend = provider.GetRate("AUD", "USD", new DateOnly(2024, 1, 13), RateLookupOptions.PreviousWithin(3));
        Console.WriteLine($"  Saturday lookup  : {weekend.Rate.Rate} resolved to {weekend.Rate.Date:yyyy-MM-dd}"
            + "  (a date with no fixing, resolved back to the prior business day and reporting the date it used)");

        Console.WriteLine($"  Declared history : {provider.HistoryAvailability.Kind}, earliest {provider.HistoryAvailability.EarliestDate:yyyy-MM-dd}"
            + "  (the horizon is declared, so a caller can tell an unanswerable date from a failure before asking)");

        // The conversion extensions accept any IDatedRateProvider - including this one.
        Money<AUD> invoice = Money.Of<AUD>(2499.95m);
        Money<USD> converted = invoice.ConvertTo<AUD, USD>(provider, new DateOnly(2024, 1, 15));
        Console.WriteLine($"  Convert          : {invoice} -> {converted}");

        // And the custom source composes under the shipped decorators exactly like a web provider.
        using var cached = new CachingRateProvider(provider, new InMemoryRateCache("CustomFeed"), new CachingRateOptions());
        cached.GetRate("AUD", "USD", new DateOnly(2024, 1, 15));
        RateLookupResult fromCache = cached.GetRate("AUD", "USD", new DateOnly(2024, 1, 15));
        Console.WriteLine($"  Cached wrapper   : second lookup served from {fromCache.Provenance.Origin} ({fromCache.Provenance.Backend})"
            + "  (the shipped decorator accepts the custom provider through the plain interface - the payoff for implementing it)");

        Console.WriteLine();
        Console.WriteLine("Done.");
    }
}

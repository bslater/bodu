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

        var path = Path.Combine(AppContext.BaseDirectory, "Data", "custom-feed.csv");
        var provider = new CsvFileRateProvider(path, "CustomFeed");

        // Direct lookups - the delegated FixedDatedRateProvider supplies date resolution, inverse
        // fallback, and identity rates without any custom code.
        RateLookupResult exact = provider.GetRate("AUD", "USD", new DateOnly(2024, 1, 15));
        Console.WriteLine($"AUD/USD exact    : {exact.Rate.Rate} [{exact.Rate.Provider}]");

        RateLookupResult inverse = provider.GetRate("USD", "AUD", new DateOnly(2024, 1, 15));
        Console.WriteLine($"USD/AUD inverse  : {inverse.Rate.Rate} (derived from the AUD/USD observation)");

        RateLookupResult weekend = provider.GetRate("AUD", "USD", new DateOnly(2024, 1, 13), RateLookupOptions.PreviousWithin(3));
        Console.WriteLine($"Saturday lookup  : {weekend.Rate.Rate} resolved to {weekend.Rate.Date:yyyy-MM-dd}");

        Console.WriteLine($"Declared history : {provider.HistoryAvailability.Kind}, earliest {provider.HistoryAvailability.EarliestDate:yyyy-MM-dd}");

        // The conversion extensions accept any IDatedRateProvider - including this one.
        Money<AUD> invoice = Money.Of<AUD>(2499.95m);
        Money<USD> converted = invoice.ConvertTo<AUD, USD>(provider, new DateOnly(2024, 1, 15));
        Console.WriteLine($"Convert          : {invoice} -> {converted}");

        // And the custom source composes under the shipped decorators exactly like a web provider.
        using var cached = new CachingRateProvider(provider, new InMemoryRateCache("CustomFeed"), new CachingRateOptions());
        cached.GetRate("AUD", "USD", new DateOnly(2024, 1, 15));
        RateLookupResult fromCache = cached.GetRate("AUD", "USD", new DateOnly(2024, 1, 15));
        Console.WriteLine($"Cached wrapper   : second lookup served from {fromCache.Provenance.Origin} ({fromCache.Provenance.Backend})");

        Console.WriteLine();
        Console.WriteLine("Done.");
    }
}

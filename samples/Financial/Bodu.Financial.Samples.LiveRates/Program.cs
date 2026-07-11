// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using Bodu.Financial.ExchangeRates;
using Bodu.Financial.Samples.LiveRates.Scenarios;

namespace Bodu.Financial.Samples.LiveRates;

/// <summary>
/// Entry point for the live-rates sample — the one sample in this tree that goes online. It fetches
/// real published rates from a web provider for a computed historical date ("last Wednesday", with a
/// safety buffer) and its trailing week, so the requested data is virtually certain to exist.
/// Requires internet access; it is deliberately excluded from the CI samples run.
/// </summary>
public static class Program
{
    /// <summary>
    /// Warms the selected provider for the computed window and runs the scenarios.
    /// </summary>
    /// <returns>Zero on success; one when the live feed could not be reached.</returns>
    public static async Task<int> Main()
    {
        Console.WriteLine("Bodu.Financial.Samples.LiveRates");
        Console.WriteLine("================================");
        Console.WriteLine();

        // --- Choosing a date that is virtually certain to have a published rate ------------------
        // 1. Start from today (UTC) minus a 5-day buffer, so publication lag, time zones, and the
        //    current in-progress week can never make the target date "too fresh".
        // 2. Walk back to the Wednesday on or before that anchor. A Wednesday is never a weekend,
        //    and mid-week public holidays are rare - and the lookups below still carry a
        //    PreviousWithin(5) tolerance so even a holiday Wednesday resolves to the prior fixing.
        // 3. The week window is the 7 days ending on that Wednesday: it always spans at least four
        //    business days, and stays comfortably inside every provider's history window
        //    (including OANDA's rolling ~180 days).
        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
        var anchor = todayUtc.AddDays(-5);
        var wednesday = anchor.PreviousDateOfWeek(DayOfWeek.Wednesday);
        var weekStart = wednesday.AddDays(-6);

        Console.WriteLine($"Today (UTC)     : {todayUtc:yyyy-MM-dd} ({todayUtc.DayOfWeek})");
        Console.WriteLine($"Target date     : {wednesday:yyyy-MM-dd} ({wednesday.DayOfWeek}, >= 5 days old)");
        Console.WriteLine($"Week window     : {weekStart:yyyy-MM-dd} .. {wednesday:yyyy-MM-dd}");
        Console.WriteLine();

        // --- Active provider: European Central Bank (EUR reference rates, since 1999) -------------
        // The ECB feed is anonymous, robust, and has deep history - the safest default.
        using var provider = new EcbRateProvider(new EcbRateProviderOptions { EnableDiskCache = false });
        var fromIso = "EUR"; var toIso = "USD";

        // --- Other providers: uncomment ONE block (and comment the ECB block above) ---------------
        // Every provider serves the same contracts, and LoadPairAsync below works uniformly, so the
        // scenarios run unchanged. Single-base feeds need their base currency in the pair.
        //
        // Reserve Bank of Australia (base AUD, published .xls workbooks, since 1983):
        //     using var provider = new RbaRateProvider(new RbaRateProviderOptions { EnableDiskCache = false });
        //     var fromIso = "AUD"; var toIso = "USD";
        //
        // Bank of England (base GBP, CSV windows, since 1975):
        //     using var provider = new BoeRateProvider(new BoeRateProviderOptions { EnableDiskCache = false });
        //     var fromIso = "GBP"; var toIso = "USD";
        //
        // Yahoo Finance (any pair, per-ticker chart JSON, since 2003; sends a browser User-Agent):
        //     using var provider = new YahooRateProvider(new YahooRateProviderOptions { EnableDiskCache = false });
        //     var fromIso = "EUR"; var toIso = "USD";
        //
        // OFX (any pair, spot-rate history JSON, unbounded history):
        //     using var provider = new OfxRateProvider(new OfxRateProviderOptions { EnableDiskCache = false });
        //     var fromIso = "EUR"; var toIso = "USD";
        //
        // OANDA (any pair, rolling ~180-day anonymous window - the buffer above stays inside it):
        //     using var provider = new OandaRateProvider(new OandaRateProviderOptions { EnableDiskCache = false });
        //     var fromIso = "EUR"; var toIso = "USD";
        //
        // XE.com (any pair, rolling ~10-year window; experimental - scrapes an auth token):
        //     using var provider = new XeRateProvider(new XeRateProviderOptions { EnableDiskCache = false });
        //     var fromIso = "EUR"; var toIso = "USD";
        //
        // Fixer (fixer.io, any pair; requires an access_key - free plan is EUR-base only):
        //     using var provider = new FixerRateProvider(new FixerRateProviderOptions { ApiKey = "<your-key>" });
        //     var fromIso = "EUR"; var toIso = "USD";
        //
        // exchangerate.host (any pair; requires an access_key - free plan is USD-source only):
        //     using var provider = new ExchangeRateHostRateProvider(new ExchangeRateHostRateProviderOptions { ApiKey = "<your-key>" });
        //     var fromIso = "USD"; var toIso = "EUR";
        //
        // FRED (St. Louis Fed, mapped pairs; requires an api_key - EUR/USD maps to DEXUSEU):
        //     using var provider = new FredRateProvider(new FredRateProviderOptions { ApiKey = "<your-key>" });
        //     var fromIso = "EUR"; var toIso = "USD";
        //
        // IMF (keyless, but MONTHLY and USD/SDR-anchored - the weekly window above won't resolve;
        // use a month-spanning range and a first-of-month target date for this source):
        //     using var provider = new ImfRateProvider(new ImfRateProviderOptions());
        //     var fromIso = "USD"; var toIso = "GBP";
        // -------------------------------------------------------------------------------------------

        try
        {
            // One warm-up call works for every provider: LoadPairAsync is on the shared
            // WebRateProvider base, whether the source fetches by feed, era, window, or pair.
            Console.WriteLine($"Fetching {fromIso}/{toIso} {weekStart:yyyy-MM-dd}..{wednesday:yyyy-MM-dd} from {provider.GetType().Name}...");
            await provider.LoadPairAsync(fromIso, toIso, weekStart, wednesday);
            Console.WriteLine();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or ExchangeRateFormatException)
        {
            Console.WriteLine("Could not reach the live feed. This sample needs internet access (it is the");
            Console.WriteLine("one deliberate exception to the offline-by-default sample rule, and is");
            Console.WriteLine($"excluded from the CI samples run for that reason). Error: {ex.Message}");
            return 1;
        }

        HistoricalDate.Run(provider, fromIso, toIso, wednesday);
        WeekOfRates.Run(provider, fromIso, toIso, weekStart, wednesday);

        Console.WriteLine("Done.");
        return 0;
    }
}

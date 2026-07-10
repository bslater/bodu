// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LookupModes.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.ExchangeRates;

namespace Bodu.Financial.Samples.OfflineRates.Scenarios;

/// <summary>
/// Demonstrates the four <see cref="RateLookupOptions" /> date-resolution modes. Rate series only carry
/// business-day observations, so requests that land on a weekend or holiday must say how (and how far)
/// the lookup may move off the requested date.
/// </summary>
public static class LookupModes
{
    /// <summary>
    /// Resolves the same weekend date under each mode and prints how the date was resolved.
    /// </summary>
    /// <param name="source">The dated provider to query.</param>
    public static void Run(IDatedRateProvider source)
    {
        Console.WriteLine("--- RateLookupOptions: date-resolution modes ---");

        // 2024-03-16 is a Saturday: the series has observations on Friday the 15th and Monday the 18th,
        // but nothing on the requested date itself.
        var saturday = new DateOnly(2024, 3, 16);

        // Exact (the default): only the requested date satisfies the lookup. TryGetRate reports the
        // miss instead of throwing; GetRate would throw KeyNotFoundException.
        if (!source.TryGetRate("AUD", "USD", saturday, RateLookupOptions.Exact, out _))
            Console.WriteLine($"Exact              {saturday:yyyy-MM-dd} -> no observation (Saturday)");

        // PreviousWithin(n): walk backwards up to n days — the classic "most recent published rate"
        // rule used for valuations.
        Print("PreviousWithin(3)", source.GetRate("AUD", "USD", saturday, RateLookupOptions.PreviousWithin(3)));

        // NextWithin(n): walk forwards up to n days — e.g. settle on the first fixing after a value date.
        Print("NextWithin(3)   ", source.GetRate("AUD", "USD", saturday, RateLookupOptions.NextWithin(3)));

        // NearestWithin(n): whichever observation is closest in either direction.
        Print("NearestWithin(3)", source.GetRate("AUD", "USD", saturday, RateLookupOptions.NearestWithin(3)));

        // A tolerance is a hard bound: three days beyond the end of the data set finds nothing.
        var afterData = new DateOnly(2024, 7, 15);
        if (!source.TryGetRate("AUD", "USD", afterData, RateLookupOptions.PreviousWithin(3), out _))
            Console.WriteLine($"PreviousWithin(3)  {afterData:yyyy-MM-dd} -> outside tolerance, no result");

        Console.WriteLine();
    }

    /// <summary>
    /// Prints one lookup result: the resolved observation date, its distance from the requested date,
    /// and the rate itself. The result also carries provenance identifying the serving provider.
    /// </summary>
    /// <param name="label">The mode label to print.</param>
    /// <param name="result">The lookup result to describe.</param>
    private static void Print(string label, RateLookupResult result) =>
        Console.WriteLine(
            $"{label}  {result.RequestedDate:yyyy-MM-dd} -> {result.Rate.Date:yyyy-MM-dd} " +
            $"({result.Resolution}, offset {result.OffsetDays}d): {result.Rate.Rate} [{result.Rate.Provider}]");
}

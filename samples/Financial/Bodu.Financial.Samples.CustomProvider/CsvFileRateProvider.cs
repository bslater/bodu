// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CsvFileRateProvider.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;

namespace Bodu.Financial.Samples.CustomProvider;

/// <summary>
/// A custom <see cref="IDatedRateProvider" /> over a CSV rate file — the recommended shape for
/// bringing your own rate source into the financial stack. Parse your data into a
/// <see cref="RateTableBuilder" />, freeze it into a <see cref="RateBook" />, and delegate the whole
/// lookup surface to a <see cref="FixedDatedRateProvider" /> so date resolution, inverse fallback,
/// identity rates, and provenance behave exactly like every shipped provider — for free.
/// </summary>
/// <remarks>
/// Because the type implements <see cref="IDatedRateProvider" />, it composes with everything built
/// on that contract: wrap it in a <c>CachingRateProvider</c>, group it under an
/// <c>AggregatingRateProvider</c>, or register it with <c>AddDatedExchangeRateProvider</c>. The
/// accompanying test project derives the shipped
/// <c>DatedRateProviderContractTests&lt;CsvFileRateProvider&gt;</c> base to prove the contract holds.
/// </remarks>
public sealed class CsvFileRateProvider
    : IDatedRateProvider
{
    private readonly FixedDatedRateProvider _rates;

    /// <summary>
    /// Initializes a new instance by eagerly loading <paramref name="path" />.
    /// </summary>
    /// <param name="path">The CSV file holding <c>Date,From,To,Rate</c> rows.</param>
    /// <param name="providerName">The provider label stamped onto each observation.</param>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="FormatException">A data row is malformed.</exception>
    public CsvFileRateProvider(string path, string providerName)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Rate file not found: {path}", path);

        // Accumulate the file's observations, then freeze them. The builder tolerates any row
        // order and normalizes each series; the frozen book is immutable and thread-safe.
        var builder = new RateTableBuilder();
        foreach (var line in File.ReadLines(path))
        {
            if (line.Length == 0 || line[0] == '#' || line.StartsWith("Date,", StringComparison.Ordinal))
                continue;

            var fields = line.Split(',');
            if (fields.Length != 4)
                throw new FormatException($"Expected 'Date,From,To,Rate' but found: {line}");

            builder.Upsert(
                new CurrencyPair(CurrencyInfo.ParseCurrencyCode(fields[1]), CurrencyInfo.ParseCurrencyCode(fields[2])),
                providerName,
                DateOnly.Parse(fields[0], CultureInfo.InvariantCulture),
                decimal.Parse(fields[3], CultureInfo.InvariantCulture));
        }

        _rates = new FixedDatedRateProvider(builder.ToBook());
    }

    /// <summary>
    /// Gets the provider's declared history availability (derived from the loaded observations).
    /// </summary>
    public RateHistoryAvailability HistoryAvailability => _rates.HistoryAvailability;

    /// <inheritdoc />
    public RateLookupResult GetRate(string fromIsoCode, string toIsoCode, RateLookupOptions? options = null) =>
        _rates.GetRate(fromIsoCode, toIsoCode, options);

    /// <inheritdoc />
    public RateLookupResult GetRate(string fromIsoCode, string toIsoCode, DateOnly date, RateLookupOptions? options = null) =>
        _rates.GetRate(fromIsoCode, toIsoCode, date, options);

    /// <inheritdoc />
    public bool TryGetRate(string fromIsoCode, string toIsoCode, DateOnly date, RateLookupOptions? options, out RateLookupResult result) =>
        _rates.TryGetRate(fromIsoCode, toIsoCode, date, options, out result);

    /// <inheritdoc />
    public RateRangeResult GetRates(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate) =>
        _rates.GetRates(fromIsoCode, toIsoCode, startDate, endDate);

    /// <inheritdoc />
    public ValueTask<RateLookupResult> GetRateAsync(string fromIsoCode, string toIsoCode, RateLookupOptions? options = null, CancellationToken cancellationToken = default) =>
        _rates.GetRateAsync(fromIsoCode, toIsoCode, options, cancellationToken);

    /// <inheritdoc />
    public ValueTask<RateLookupResult> GetRateAsync(string fromIsoCode, string toIsoCode, DateOnly date, RateLookupOptions? options = null, CancellationToken cancellationToken = default) =>
        _rates.GetRateAsync(fromIsoCode, toIsoCode, date, options, cancellationToken);

    /// <inheritdoc />
    public ValueTask<RateRangeResult> GetRatesAsync(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) =>
        _rates.GetRatesAsync(fromIsoCode, toIsoCode, startDate, endDate, cancellationToken);
}

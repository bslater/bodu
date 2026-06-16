// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlFileExchangeRateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// An <see cref="IExchangeRateCache" /> that persists a single provider's rates as one TOML file per currency pair.
/// </summary>
/// <remarks>
/// <para>
/// Each file holds a TOML array of tables under <c>Entries</c>, one table per dated rate, and a second array of tables
/// under <c>Coverage</c>, one table per recorded fetch window. Decimal rates are written as quoted strings (
/// <see cref="TomlDecimalHandling.String" />) so the full precision and scale round-trips exactly; dates and instants
/// use TOML's native RFC 3339 forms.
/// </para>
/// <para>
/// A file written before coverage was tracked has no <c>[[Coverage]]</c> section; it deserializes to its rate rows with
/// empty coverage and no error, so older caches remain readable and simply refetch ranges until coverage is recorded.
/// Likewise, an entry written before the upstream fetch instant was tracked has no <c>ObservedAtUtc</c> key and
/// deserializes that field to <see langword="null" />.
/// </para>
/// <para>
/// Files are laid out under a per-provider subdirectory of the configured cache directory; malformed content is treated
/// as an empty result, and all file-level resilience — including atomic temp-and-move writes — is provided by
/// <see cref="FileExchangeRateCacheBase{TOptions}" />.
/// </para>
/// </remarks>
/// <example>
/// A cache bound to provider <c>RBA</c> stores the <c>AUD/USD</c> pair as <c>&lt;directory&gt;/RBA/AUDUSD.toml</c> with
/// one table per dated rate and one table per fetched window: <code language="toml">
///<![CDATA[
/// [[Entries]]
/// Date = 2023-01-03
/// Rate = "0.5000"
/// CachedAtUtc = 2023-01-04T09:15:00+00:00
/// ObservedAtUtc = 2023-01-03T16:00:00+00:00
///
/// [[Entries]]
/// Date = 2023-01-06
/// Rate = "0.5100"
/// CachedAtUtc = 2023-01-04T09:15:00+00:00
/// ObservedAtUtc = 2023-01-06T16:00:00+00:00
///
/// [[Coverage]]
/// Start = 2023-01-03
/// End = 2023-01-06
/// FetchedAtUtc = 2023-01-04T09:15:00+00:00
///]]>
/// </code>
/// </example>
public sealed class TomlFileExchangeRateCache
    : FileExchangeRateCacheBase<FileExchangeRateCacheOptions>
{
    /// <summary>
    /// The serializer options shared by every read and write; decimals are written as strings for lossless round-trips.
    /// </summary>
    private static readonly TomlSerializerOptions s_tomlOptions = new() { DecimalHandling = TomlDecimalHandling.String };

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlFileExchangeRateCache" /> class.
    /// </summary>
    /// <param name="options">The file-cache options that select the bound provider and storage directory.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    public TomlFileExchangeRateCache(FileExchangeRateCacheOptions options)
        : base(options)
    {
    }

    /// <inheritdoc />
    protected override string FileExtension => ".toml";

    /// <inheritdoc />
    private protected override string Serialize(CachePairState state)
    {
        ExchangeRateCacheFile file = new()
        {
            Entries = new List<ExchangeRateCacheEntry>(state.Entries.Count),
            Coverage = new List<ExchangeRateCacheCoverageEntry>(state.Coverage.Count),
        };

        foreach (CachedExchangeRate entry in state.Entries)
            file.Entries.Add(new ExchangeRateCacheEntry { Date = entry.Date, Rate = entry.Rate, CachedAtUtc = entry.CachedAtUtc, ObservedAtUtc = entry.ObservedAtUtc });

        foreach (CoverageWindow window in state.Coverage)
            file.Coverage.Add(new ExchangeRateCacheCoverageEntry { Start = window.Start, End = window.End, FetchedAtUtc = window.FetchedAtUtc });

        return TomlSerializer.Serialize(file, s_tomlOptions);
    }

    /// <inheritdoc />
    private protected override CachePairState Deserialize(string text)
    {
        try
        {
            ExchangeRateCacheFile file = TomlSerializer.Deserialize<ExchangeRateCacheFile>(text, s_tomlOptions);
            if (file is null)
                return CachePairState.Empty;

            // A pre-coverage file simply has no [[Coverage]] array, deserializing to an empty list with no error.
            List<CachedExchangeRate> entries = new(file.Entries?.Count ?? 0);
            if (file.Entries is { Count: > 0 })
            {
                // A pre-C file with no ObservedAtUtc key deserializes that property to null, same as the missing
                // [[Coverage]] array handling above, so older caches keep their rows and simply carry no fetch instant.
                foreach (ExchangeRateCacheEntry entry in file.Entries)
                    entries.Add(new CachedExchangeRate(entry.Date, entry.Rate, entry.CachedAtUtc, entry.ObservedAtUtc));
            }

            List<CoverageWindow> coverage = new(file.Coverage?.Count ?? 0);
            if (file.Coverage is { Count: > 0 })
            {
                foreach (ExchangeRateCacheCoverageEntry window in file.Coverage)
                    coverage.Add(new CoverageWindow(window.Start, window.End, window.FetchedAtUtc));
            }

            if (entries.Count == 0 && coverage.Count == 0)
                return CachePairState.Empty;

            return new CachePairState(entries, coverage);
        }
        catch (TomlFormatException)
        {
            return CachePairState.Empty;
        }
        catch (TomlSerializationException)
        {
            return CachePairState.Empty;
        }
    }
}

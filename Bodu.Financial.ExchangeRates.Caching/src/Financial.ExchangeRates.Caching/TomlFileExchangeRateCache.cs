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
/// Each file holds a TOML array of tables under <c>Entries</c>, one table per dated rate. Decimal rates are written as
/// quoted strings (<see cref="TomlDecimalHandling.String" />) so the full precision and scale round-trips exactly;
/// dates and the caching instant use TOML's native RFC 3339 forms.
/// </para>
/// <para>
/// Files are laid out under a per-provider subdirectory of the configured cache directory; malformed content is treated
/// as an empty result, and all file-level resilience is provided by <see cref="FileExchangeRateCacheBase{TOptions}" />.
/// </para>
/// </remarks>
/// <example>
/// A cache bound to provider <c>RBA</c> stores the <c>AUD/USD</c> pair as <c>&lt;directory&gt;/RBA/AUDUSD.toml</c> with
/// one table per dated rate: <code language="toml">
///<![CDATA[
/// [[Entries]]
/// Date = 2023-01-03
/// Rate = "0.5000"
/// CachedAtUtc = 2023-01-04T09:15:00+00:00
///
/// [[Entries]]
/// Date = 2023-01-06
/// Rate = "0.5100"
/// CachedAtUtc = 2023-01-04T09:15:00+00:00
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
    protected override string Serialize(IReadOnlyList<CachedExchangeRate> entries)
    {
        ExchangeRateCacheFile file = new() { Entries = new List<ExchangeRateCacheEntry>(entries.Count) };
        foreach (CachedExchangeRate entry in entries)
            file.Entries.Add(new ExchangeRateCacheEntry { Date = entry.Date, Rate = entry.Rate, CachedAtUtc = entry.CachedAtUtc });

        return TomlSerializer.Serialize(file, s_tomlOptions);
    }

    /// <inheritdoc />
    protected override IReadOnlyList<CachedExchangeRate> Deserialize(string text)
    {
        try
        {
            ExchangeRateCacheFile file = TomlSerializer.Deserialize<ExchangeRateCacheFile>(text, s_tomlOptions);
            if (file?.Entries is not { Count: > 0 })
                return Array.Empty<CachedExchangeRate>();

            List<CachedExchangeRate> result = new(file.Entries.Count);
            foreach (ExchangeRateCacheEntry entry in file.Entries)
                result.Add(new CachedExchangeRate(entry.Date, entry.Rate, entry.CachedAtUtc));

            return result;
        }
        catch (TomlFormatException)
        {
            return Array.Empty<CachedExchangeRate>();
        }
        catch (TomlSerializationException)
        {
            return Array.Empty<CachedExchangeRate>();
        }
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlFileSystemExchangeRateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// An <see cref="IExchangeRateCache" /> that persists rates as one TOML file per provider and currency pair.
/// </summary>
/// <remarks>
/// <para>
/// Each file holds a TOML array of tables under <c>Entries</c>, one table per dated rate. Decimal rates are written as
/// quoted strings (<see cref="TomlDecimalHandling.String" />) so the full precision and scale round-trips exactly;
/// dates and the caching instant use TOML's native RFC 3339 forms.
/// </para>
/// <para>
/// The cache is best-effort: any I/O or TOML failure while reading is reported as an empty result, and any failure
/// while writing is swallowed, so a cache problem never breaks rate retrieval.
/// </para>
/// </remarks>
/// <example>
/// A store under provider <c>Yahoo</c> for the <c>AUD/USD</c> pair produces a file named <c>Yahoo_AUDUSD.toml</c> with
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
public sealed class TomlFileSystemExchangeRateCache
    : ExchangeRateCacheBase
{
    /// <summary>
    /// The serializer options shared by every read and write; decimals are written as strings for lossless round-trips.
    /// </summary>
    private static readonly TomlSerializerOptions s_tomlOptions = new() { DecimalHandling = TomlDecimalHandling.String };

    /// <summary>
    /// The directory in which cached rate files are stored.
    /// </summary>
    private readonly string _directory;

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlFileSystemExchangeRateCache" /> class.
    /// </summary>
    /// <param name="options">The cache options that select the storage directory.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    public TomlFileSystemExchangeRateCache(FileSystemExchangeRateCacheOptions options)
    {
        ThrowHelper.ThrowIfNull(options);

        _directory = string.IsNullOrWhiteSpace(options.CacheDirectory)
            ? Path.Combine(Path.GetTempPath(), "bodu-exchange-rates")
            : options.CacheDirectory;
    }

    /// <summary>
    /// Gets the directory in which cached rate files are stored.
    /// </summary>
    /// <returns>The absolute or relative cache directory path.</returns>
    public string Directory => _directory;

    /// <inheritdoc />
    protected override IReadOnlyList<CachedExchangeRate> ReadEntries(string provider, ExchangeRatePair pair)
    {
        var path = Path.Combine(_directory, BuildFileName(provider, pair));

        try
        {
            if (!File.Exists(path))
                return Array.Empty<CachedExchangeRate>();

            var text = File.ReadAllText(path);
            ExchangeRateCacheFile file = TomlSerializer.Deserialize<ExchangeRateCacheFile>(text, s_tomlOptions);
            if (file?.Entries is not { Count: > 0 })
                return Array.Empty<CachedExchangeRate>();

            List<CachedExchangeRate> result = new(file.Entries.Count);
            foreach (ExchangeRateCacheEntry entry in file.Entries)
                result.Add(new CachedExchangeRate(entry.Date, entry.Rate, entry.CachedAtUtc));

            return result;
        }
        catch (IOException)
        {
            return Array.Empty<CachedExchangeRate>();
        }
        catch (UnauthorizedAccessException)
        {
            return Array.Empty<CachedExchangeRate>();
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

    /// <inheritdoc />
    protected override void WriteEntries(string provider, ExchangeRatePair pair, IReadOnlyList<CachedExchangeRate> entries)
    {
        try
        {
            System.IO.Directory.CreateDirectory(_directory);

            ExchangeRateCacheFile file = new() { Entries = new List<ExchangeRateCacheEntry>(entries.Count) };
            foreach (CachedExchangeRate entry in entries)
                file.Entries.Add(new ExchangeRateCacheEntry { Date = entry.Date, Rate = entry.Rate, CachedAtUtc = entry.CachedAtUtc });

            var text = TomlSerializer.Serialize(file, s_tomlOptions);
            File.WriteAllText(Path.Combine(_directory, BuildFileName(provider, pair)), text);
        }
        catch (IOException)
        {
            // Best-effort cache: a failed write must not break rate retrieval.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cache: a failed write must not break rate retrieval.
        }
        catch (TomlSerializationException)
        {
            // Best-effort cache: a serialization failure must not break rate retrieval.
        }
    }
}

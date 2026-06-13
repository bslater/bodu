// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemYahooRateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Financial.ExchangeRates.Yahoo;

/// <summary>
/// An <see cref="IYahooRateCache" /> that persists rates as one JSON file per provider and currency pair, in the common
/// dated-rate-with-retrieval-time format.
/// </summary>
/// <remarks>
/// The cache is best-effort: any I/O or deserialization failure while reading is reported as an empty result, and any
/// failure while writing is swallowed, so a cache problem never breaks rate retrieval. Stores merge with the existing
/// file so the most recently retrieved rate wins per date.
/// </remarks>
public sealed class FileSystemYahooRateCache
    : IYahooRateCache
{
    /// <summary>
    /// The serializer options used for the cache files.
    /// </summary>
    private static readonly JsonSerializerOptions s_serializerOptions = new() { WriteIndented = false };

    /// <summary>
    /// The directory in which cached rate files are stored.
    /// </summary>
    private readonly string _directory;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemYahooRateCache" /> class.
    /// </summary>
    /// <param name="directory">
    /// The cache directory. When <see langword="null" /> or empty, a <c>bodu-yahoo</c> folder under the system
    /// temporary path is used.
    /// </param>
    public FileSystemYahooRateCache(string? directory)
    {
        _directory = string.IsNullOrWhiteSpace(directory)
            ? Path.Combine(Path.GetTempPath(), "bodu-yahoo")
            : directory;
    }

    /// <summary>
    /// Gets the directory in which cached rate files are stored.
    /// </summary>
    /// <returns>The absolute or relative cache directory path.</returns>
    public string Directory => _directory;

    /// <inheritdoc />
    public IReadOnlyList<CachedExchangeRate> GetRates(ExchangeRatePair pair, string provider)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(provider);

        var path = Path.Combine(_directory, BuildFileName(pair, provider));

        try
        {
            if (!File.Exists(path))
                return Array.Empty<CachedExchangeRate>();

            using FileStream stream = File.OpenRead(path);
            return JsonSerializer.Deserialize<List<CachedExchangeRate>>(stream, s_serializerOptions)
                ?? (IReadOnlyList<CachedExchangeRate>)Array.Empty<CachedExchangeRate>();
        }
        catch (IOException)
        {
            return Array.Empty<CachedExchangeRate>();
        }
        catch (UnauthorizedAccessException)
        {
            return Array.Empty<CachedExchangeRate>();
        }
        catch (JsonException)
        {
            return Array.Empty<CachedExchangeRate>();
        }
    }

    /// <inheritdoc />
    public void Store(ExchangeRatePair pair, string provider, IReadOnlyList<CachedExchangeRate> rates)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(provider);
        ThrowHelper.ThrowIfNull(rates);

        if (rates.Count == 0)
            return;

        try
        {
            System.IO.Directory.CreateDirectory(_directory);

            // Merge with any existing entry so the most recently retrieved rate wins per date.
            Dictionary<DateOnly, CachedExchangeRate> merged = new();
            foreach (CachedExchangeRate existing in GetRates(pair, provider))
                merged[existing.Date] = existing;

            foreach (CachedExchangeRate rate in rates)
            {
                if (!merged.TryGetValue(rate.Date, out CachedExchangeRate current) || rate.RetrievedAt >= current.RetrievedAt)
                    merged[rate.Date] = rate;
            }

            var ordered = merged.Values.OrderBy(static r => r.Date).ToList();

            using FileStream stream = File.Create(Path.Combine(_directory, BuildFileName(pair, provider)));
            JsonSerializer.Serialize(stream, ordered, s_serializerOptions);
        }
        catch (IOException)
        {
            // Best-effort cache: a failed write must not break rate retrieval.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cache: a failed write must not break rate retrieval.
        }
    }

    /// <summary>
    /// Builds the cache file name for a provider and pair.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <param name="provider">The provider identifier.</param>
    /// <returns>The file name, for example <c>Yahoo_AUDUSD.json</c>.</returns>
    private static string BuildFileName(ExchangeRatePair pair, string provider) =>
        $"{provider}_{pair.FromIsoCode}{pair.ToIsoCode}.json";
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="JsonFileExchangeRateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// An <see cref="IExchangeRateCache" /> that persists a provider's rates as JSON files, one file per currency pair (or,
/// under a partitioned layout, per pair and calendar period).
/// </summary>
/// <remarks>
/// <para>
/// Each file is a JSON object recording the bound <c>Provider</c> and the pair's <c>From</c> and <c>To</c> currency
/// codes — making the file self-describing rather than identified only by its name and folder — alongside an
/// <c>Entries</c> array, one object per dated rate, and a <c>Coverage</c> array, one object per recorded fetch window.
/// Decimal rates are written as JSON numbers, which <see cref="System.Text.Json" /> round-trips losslessly to
/// <see cref="decimal" />; dates and instants use ISO 8601 forms.
/// </para>
/// <para>
/// A file written before coverage was tracked has no <c>Coverage</c> array; it deserializes to its rate rows with empty
/// coverage and no error, so older caches remain readable. A row written before the upstream fetch instant was tracked
/// has no <c>ObservedAtUtc</c> key and deserializes that field to <see langword="null" />, and a file written before
/// the self-describing header was added has no <c>Provider</c>/<c>From</c>/<c>To</c> keys.
/// </para>
/// <para>
/// Files are laid out by the configured <see cref="FileExchangeRateCacheOptions.Layout" /> — by default a single file
/// per pair under a per-provider subdirectory, or one file per calendar period when a partitioned layout is selected.
/// Malformed content is treated as an empty result, and all file-level resilience — including atomic temp-and-move
/// writes — is provided by <see cref="FileExchangeRateCacheBase{TOptions}" />.
/// </para>
/// </remarks>
/// <example>
/// A cache bound to provider <c>RBA</c> stores the <c>AUD/USD</c> pair as <c>&lt;directory&gt;/RBA/AUDUSD.json</c> with
/// the self-describing header, one object per dated rate, and one object per fetched window:
/// <code language="json">
///<![CDATA[
/// {
///   "Provider": "RBA",
///   "From": "AUD",
///   "To": "USD",
///   "Entries": [
///     { "Date": "2023-01-03", "Rate": 0.5000, "CachedAtUtc": "2023-01-04T09:15:00+00:00", "ObservedAtUtc": "2023-01-03T16:00:00+00:00" }
///   ],
///   "Coverage": [
///     { "Start": "2023-01-03", "End": "2023-01-06", "FetchedAtUtc": "2023-01-04T09:15:00+00:00" }
///   ]
/// }
///]]>
/// </code>
/// </example>
public sealed class JsonFileExchangeRateCache
    : FileExchangeRateCacheBase<FileExchangeRateCacheOptions>
{
    /// <summary>The serializer options shared by every read and write; indented output for readability and null-valued keys omitted.</summary>
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonFileExchangeRateCache" /> class.
    /// </summary>
    /// <param name="options">
    /// The file-cache options that select the bound provider, storage directory, and layout.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    public JsonFileExchangeRateCache(FileExchangeRateCacheOptions options)
        : base(options)
    {
    }

    /// <inheritdoc />
    protected override string FileExtension => ".json";

    /// <inheritdoc />
    private protected override string Serialize(CurrencyPair pair, CachePairState state) =>
        JsonSerializer.Serialize(ExchangeRateCacheFileConverter.ToFile(Provider, pair, state), s_jsonOptions);

    /// <inheritdoc />
    private protected override CachePairState Deserialize(string text)
    {
        try
        {
            return ExchangeRateCacheFileConverter.ToState(JsonSerializer.Deserialize<ExchangeRateCacheFile>(text, s_jsonOptions));
        }
        catch (JsonException)
        {
            return CachePairState.Empty;
        }
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlFileRateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml;
using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// An <see cref="IRateCache" /> that persists a provider's rates as TOML files, one file per currency pair (or, under a
/// partitioned layout, per pair and calendar period).
/// </summary>
/// <remarks>
/// <para>
/// Each file records the bound <c>Provider</c> and the pair's <c>From</c> and <c>To</c> currency codes as top-level
/// keys — making the file self-describing rather than identified only by its name and folder — followed by a TOML array
/// of tables under <c>Entries</c>, one table per dated rate, and a second array of tables under <c>Coverage</c>, one
/// table per recorded fetch window. Decimal rates are written as quoted strings (<see cref="TomlDecimalHandling.String" />)
/// so the full precision and scale round-trips exactly; dates and instants use TOML's native RFC 3339 forms.
/// </para>
/// <para>
/// A file written before coverage was tracked has no <c>[[Coverage]]</c> section; it deserializes to its rate rows with
/// empty coverage and no error, so older caches remain readable and simply refetch ranges until coverage is recorded.
/// Likewise, an entry written before the upstream fetch instant was tracked has no <c>ObservedAtUtc</c> key and
/// deserializes that field to <see langword="null" />, and a file written before the self-describing header was added
/// has no <c>Provider</c>/<c>From</c>/<c>To</c> keys.
/// </para>
/// <para>
/// Files are laid out by the configured <see cref="FileRateCacheOptions.Layout" /> — by default a single file per pair
/// under a per-provider subdirectory, or one file per calendar period when a partitioned layout is selected. Malformed
/// content is treated as an empty result, and all file-level resilience — including atomic temp-and-move writes — is
/// provided by <see cref="FileRateCacheBase{TOptions}" />.
/// </para>
/// </remarks>
/// <example>
/// A cache bound to provider <c>RBA</c> stores the <c>AUD/USD</c> pair as <c>&lt;directory&gt;/RBA/AUDUSD.toml</c> with
/// the self-describing header, one table per dated rate, and one table per fetched window:
/// <code language="toml">
///<![CDATA[
/// Provider = "RBA"
/// From = "AUD"
/// To = "USD"
///
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
public sealed class TomlFileRateCache
    : FileRateCacheBase<FileRateCacheOptions>
{
    /// <summary>The serializer options shared by every read and write; decimals are written as strings for lossless round-trips.</summary>
    private static readonly TomlSerializerOptions s_tomlOptions = new() { DecimalHandling = TomlDecimalHandling.String };

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlFileRateCache" /> class.
    /// </summary>
    /// <param name="options">The file-cache options that select the bound provider and storage directory.</param>
    /// <param name="timeProvider">
    /// The time source the swallowed-failure warning rate-limiting is measured against, or <see langword="null" /> to
    /// use <see cref="TimeProvider.System" />.
    /// </param>
    /// <param name="logger">
    /// The logger that receives a rate-limited warning when a best-effort storage failure is swallowed, or
    /// <see langword="null" /> to disable that reporting.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    public TomlFileRateCache(FileRateCacheOptions options, TimeProvider? timeProvider = null, ILogger? logger = null)
        : base(options, timeProvider, logger)
    {
    }

    /// <inheritdoc />
    protected override string FileExtension => ".toml";

    /// <inheritdoc />
    private protected override string Serialize(CurrencyPair pair, CachePairState state) =>
        TomlSerializer.Serialize(RateCacheFileConverter.ToFile(Provider, pair, state), s_tomlOptions);

    /// <inheritdoc />
    private protected override CachePairState Deserialize(string text, string path)
    {
        try
        {
            return RateCacheFileConverter.ToState(TomlSerializer.Deserialize<RateCacheFile>(text, s_tomlOptions));
        }
        catch (TomlFormatException ex)
        {
            OnCacheFileCorrupt(path, ex);
            return CachePairState.Empty;
        }
        catch (TomlSerializationException ex)
        {
            OnCacheFileCorrupt(path, ex);
            return CachePairState.Empty;
        }
    }
}

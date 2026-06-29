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
/// Files are laid out by the configured <see cref="FileExchangeRateCacheOptions.Layout" /> — by default a single file
/// per pair under a per-provider subdirectory, or one file per calendar period when a partitioned layout is selected.
/// Malformed content is treated as an empty result, and all file-level resilience — including atomic temp-and-move
/// writes — is provided by <see cref="FileExchangeRateCacheBase{TOptions}" />.
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
public sealed class TomlFileExchangeRateCache
    : FileExchangeRateCacheBase<FileExchangeRateCacheOptions>
{
    /// <summary>The serializer options shared by every read and write; decimals are written as strings for lossless round-trips.</summary>
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
    private protected override string Serialize(ExchangeRatePair pair, CachePairState state) =>
        TomlSerializer.Serialize(ExchangeRateCacheFileConverter.ToFile(Provider, pair, state), s_tomlOptions);

    /// <inheritdoc />
    private protected override CachePairState Deserialize(string text)
    {
        try
        {
            return ExchangeRateCacheFileConverter.ToState(TomlSerializer.Deserialize<ExchangeRateCacheFile>(text, s_tomlOptions));
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

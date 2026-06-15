// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileExchangeRateCacheBase.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Provides the file-storage mechanism for an <see cref="IExchangeRateCache" />: directory resolution, per-pair
/// file-name resolution, and best-effort file read and write. Derived types supply only the serialization format.
/// </summary>
/// <typeparam name="TOptions">
/// The file-cache options type carrying the bound provider and storage directory.
/// </typeparam>
/// <remarks>
/// <para>
/// A cache instance is bound to one provider, so files are laid out under a per-provider subdirectory of
/// <see cref="CacheDirectory" /> and named by the currency pair alone (see <see cref="ResolveFilePath" />). Reads and
/// writes are best-effort: any <see cref="IOException" /> or <see cref="UnauthorizedAccessException" /> surfaces as an
/// empty read result or a skipped write, so a storage problem never breaks rate retrieval. Derived types are expected
/// to treat malformed content the same way by returning an empty list from <see cref="Deserialize" />.
/// </para>
/// </remarks>
public abstract class FileExchangeRateCacheBase<TOptions>
    : ExchangeRateCacheBase<TOptions>, IFileExchangeRateCache
    where TOptions : FileExchangeRateCacheOptions
{
    /// <summary>
    /// The cached set of characters that are not permitted in a file-name segment.
    /// </summary>
    private static readonly char[] s_invalidFileNameChars = Path.GetInvalidFileNameChars();

    /// <summary>
    /// The resolved directory in which this provider's cached rate files are stored.
    /// </summary>
    private readonly string _directory;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileExchangeRateCacheBase{TOptions}" /> class.
    /// </summary>
    /// <param name="options">The file-cache options selecting the bound provider and storage directory.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    protected FileExchangeRateCacheBase(TOptions options)
        : base(options)
    {
        _directory = string.IsNullOrWhiteSpace(options.CacheDirectory)
            ? Path.Combine(Path.GetTempPath(), "bodu-exchange-rates")
            : options.CacheDirectory!;
    }

    /// <inheritdoc />
    public string CacheDirectory => _directory;

    /// <summary>
    /// Gets the file extension, including the leading period, applied to cached rate files.
    /// </summary>
    /// <returns>The file extension used by the serialization format, for example <c>.toml</c>.</returns>
    protected abstract string FileExtension { get; }

    /// <inheritdoc />
    public string ResolveFilePath(ExchangeRatePair pair) =>
        Path.Combine(_directory, SanitizeProvider(Provider), BuildFileName(pair));

    /// <inheritdoc />
    protected sealed override IReadOnlyList<CachedExchangeRate> ReadEntries(ExchangeRatePair pair)
    {
        var path = ResolveFilePath(pair);

        try
        {
            if (!File.Exists(path))
                return Array.Empty<CachedExchangeRate>();

            var text = File.ReadAllText(path);
            return Deserialize(text);
        }
        catch (IOException)
        {
            return Array.Empty<CachedExchangeRate>();
        }
        catch (UnauthorizedAccessException)
        {
            return Array.Empty<CachedExchangeRate>();
        }
    }

    /// <inheritdoc />
    protected sealed override void WriteEntries(ExchangeRatePair pair, IReadOnlyList<CachedExchangeRate> entries)
    {
        try
        {
            var path = ResolveFilePath(pair);
            Directory.CreateDirectory(Path.GetDirectoryName(path) !);

            var text = Serialize(entries);
            File.WriteAllText(path, text);
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
    /// Builds the file name (without directory) for a pair under this cache's provider.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <returns>The file name, for example <c>AUDUSD.toml</c>.</returns>
    protected virtual string BuildFileName(ExchangeRatePair pair) =>
        $"{pair.FromIsoCode}{pair.ToIsoCode}{FileExtension}";

    /// <summary>
    /// Maps a provider name to a safe path segment by replacing characters that are illegal in a file name.
    /// </summary>
    /// <param name="provider">The provider name.</param>
    /// <returns>The provider name with any illegal characters replaced by an underscore.</returns>
    protected virtual string SanitizeProvider(string provider)
    {
        ThrowHelper.ThrowIfNull(provider);

        if (provider.AsSpan().IndexOfAny(s_invalidFileNameChars) < 0)
            return provider;

        var chars = provider.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (Array.IndexOf(s_invalidFileNameChars, chars[i]) >= 0)
                chars[i] = '_';
        }

        return new string(chars);
    }

    /// <summary>
    /// Serializes the supplied entries to the cache file's text format.
    /// </summary>
    /// <param name="entries">The ordered entries to persist.</param>
    /// <returns>The serialized text to write to the cache file.</returns>
    protected abstract string Serialize(IReadOnlyList<CachedExchangeRate> entries);

    /// <summary>
    /// Deserializes the cache file's text into entries, returning an empty list when the content is malformed.
    /// </summary>
    /// <param name="text">The file text to parse.</param>
    /// <returns>The parsed entries, or an empty list when the content cannot be parsed.</returns>
    protected abstract IReadOnlyList<CachedExchangeRate> Deserialize(string text);
}

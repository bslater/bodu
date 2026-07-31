// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeConfigurationProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

using Bodu.Text.Bencode;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// A read-only <see cref="IConfigurationProvider" /> that consumes Bencode through the <c>Bodu.Text.Bencode</c>
/// read-only document model and flattens it into the colon-delimited configuration key model. Because the provider
/// exposes no mutation surface, <see cref="Set" /> is rejected.
/// </summary>
/// <remarks>
/// <para>
/// The provider implements <see cref="IConfigurationProvider" /> directly rather than deriving from
/// <see cref="Microsoft.Extensions.Configuration.ConfigurationProvider" />: omitting the mutable base keeps the
/// configuration bridge on the read-only half of the contract, so configuration loaded from Bencode cannot be mutated
/// back through the provider.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Built by BencodeConfigurationSource; flattens Bencode dictionaries into
/// // colon-separated configuration keys.
/// IConfiguration configuration = new ConfigurationBuilder()
///     .AddBencodeFile("appsettings.bencode")
///     .Build();
///
/// var level = configuration["logging:level"];
///]]>
/// </code>
/// </example>
/// </remarks>
public sealed class BencodeConfigurationProvider
    : IConfigurationProvider
{
    /// <summary>The source that produced this provider.</summary>
    private readonly BencodeConfigurationSource _source;

    /// <summary>The change token returned by <see cref="GetReloadToken" />. The provider does not reload, so it never fires.</summary>
    private readonly ConfigurationReloadToken _reloadToken = new();

    /// <summary>The flattened configuration data, keyed case-insensitively.</summary>
    private IDictionary<string, string?> _data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeConfigurationProvider" /> class backed by the supplied
    /// source.
    /// </summary>
    /// <param name="source">The source that produced this provider.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    public BencodeConfigurationProvider(BencodeConfigurationSource source)
    {
        ThrowHelper.ThrowIfNull(source);
        _source = source;
    }

    /// <summary>
    /// Loads (and flattens) the Bencode source into the configuration data map.
    /// </summary>
    /// <exception cref="BencodeFormatException">Thrown when the source is not a valid Bencode document.</exception>
    /// <exception cref="FormatException">
    /// Thrown when the document root is not a dictionary, or when two keys collide after case-insensitive flattening.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Thrown when the configured file is absent and the source is not optional.
    /// </exception>
    public void Load()
    {
        if (_source.Stream is { } stream)
        {
            _data = BencodeConfigurationParser.Parse(stream);
            return;
        }

        if (_source.Path is { } path)
        {
            if (!File.Exists(path))
            {
                if (_source.Optional)
                {
                    _data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                    return;
                }

                throw new FileNotFoundException(string.Format(CultureInfo.CurrentCulture, ConfigurationTextResourceStrings.IO_FileNotFound_BencodeFile, path), path);
            }

            using FileStream file = File.OpenRead(path);
            _data = BencodeConfigurationParser.Parse(file);
            return;
        }

        _data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Attempts to retrieve the configuration value for <paramref name="key" />.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">When this method returns <see langword="true" />, the associated value.</param>
    /// <returns><see langword="true" /> when the key is present; otherwise <see langword="false" />.</returns>
    public bool TryGet(string key, out string? value) =>
        _data.TryGetValue(key, out value);

    /// <summary>
    /// Rejected: the Bencode configuration provider is read-only.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The value that would be assigned.</param>
    /// <exception cref="NotSupportedException">
    /// Always thrown, because the provider does not permit mutation.
    /// </exception>
    public void Set(string key, string? value) =>
        throw new NotSupportedException(ConfigurationTextResourceStrings.Op_NotSupported_BencodeReadOnly);

    /// <summary>
    /// Returns the change token for this provider. The provider does not reload, so the token never fires.
    /// </summary>
    /// <returns>A non-firing <see cref="IChangeToken" />.</returns>
    public IChangeToken GetReloadToken() =>
        _reloadToken;

    /// <summary>
    /// Returns the immediate child keys beneath <paramref name="parentPath" />, merged with
    /// <paramref name="earlierKeys" />.
    /// </summary>
    /// <param name="earlierKeys">Keys contributed by earlier providers.</param>
    /// <param name="parentPath">The parent key path, or <see langword="null" /> for the root.</param>
    /// <returns>The distinct child key segments in configuration order.</returns>
    public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string? parentPath)
    {
        string prefix = parentPath is null ? string.Empty : parentPath + ConfigurationPath.KeyDelimiter;

        var results = new List<string>();
        foreach (string key in _data.Keys)
        {
            if (key.Length > prefix.Length && key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                results.Add(Segment(key, prefix.Length));
        }

        results.AddRange(earlierKeys);
        results.Sort(ConfigurationKeyComparer.Instance.Compare);
        return results;
    }

    /// <summary>
    /// Returns the first path segment of <paramref name="key" /> beginning at <paramref name="prefixLength" />.
    /// </summary>
    /// <param name="key">The full key.</param>
    /// <param name="prefixLength">The number of leading characters to skip.</param>
    /// <returns>The segment up to the next delimiter, or to the end of the key.</returns>
    private static string Segment(string key, int prefixLength)
    {
        int delimiterIndex = key.IndexOf(ConfigurationPath.KeyDelimiter, prefixLength, StringComparison.OrdinalIgnoreCase);
        return delimiterIndex < 0 ? key[prefixLength..] : key[prefixLength..delimiterIndex];
    }
}

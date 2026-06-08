// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationView.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Text.Configuration;

/// <summary>
/// Represents the resolved snapshot of a configuration document for a specific target path: a flattened dictionary of
/// configuration keys to their effective values, computed by layering preamble and matching sections in source order.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="ConfigurationExtensions.Resolve(Bodu.Text.Ini.IniDocumentBase, string?, ConfigurationResolveOptions?)" />
/// to obtain a view for a target path. The view is a one-shot snapshot — subsequent mutation of the originating
/// document does not retroactively update the view.
/// </para>
/// <para>
/// Values are <see langword="string" />? to match <c>Microsoft.Extensions.Configuration</c>'s
/// <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}" /> shape. A key that resolves to the
/// EditorConfig sentinel <c>unset</c> under <see cref="ConfigurationUnsetValueMode.RemoveEffectiveValue" /> is omitted
/// entirely.
/// </para>
/// <para>
/// Lookups accept either the canonical colon-delimited form (<c>logging:level:default</c>) or the dotted form (
/// <c>logging.level.default</c>); both resolve to the same value because dotted keys are normalized to colon-delimited
/// form before consulting the backing dictionary. Enumeration yields keys in their canonical colon-delimited form.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// IniDocument           doc  = ConfigurationDocument.Parse(text);
/// ConfigurationView view = doc.Resolve("src/Foo.cs");
///
/// Indexer lookup — colon and dotted forms are equivalent.
/// string? level = view["logging:level:default"];
/// string? alt   = view["logging.level.default"]; // same value
///
/// Typed convenience accessors on the view.
/// int indent = view.GetInt32("format:indent:size", fallback: 4);
///
/// Enumeration yields canonical colon-delimited keys.
/// foreach (KeyValuePair<string, string?> kv in view)
///     Console.WriteLine($"{kv.Key} = {kv.Value}");
///]]>
/// </example>
public sealed partial class ConfigurationView
    : IEnumerable<KeyValuePair<string, string?>>, IReadOnlyDictionary<string, string?>
{
    private readonly IReadOnlyDictionary<string, ConfigurationResolvedEntry> _entries;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationView" /> class over the supplied resolved values
    /// together with their origin metadata.
    /// </summary>
    /// <param name="values">The resolved configuration values, keyed by canonical configuration key.</param>
    /// <param name="entries">The resolved-entry metadata for each key in <paramref name="values" />.</param>
    internal ConfigurationView(
        IReadOnlyDictionary<string, string?> values,
        IReadOnlyDictionary<string, ConfigurationResolvedEntry> entries)
    {
        Values = values;
        _entries = entries;
    }

    /// <summary>
    /// Gets the effective value for <paramref name="key" />, or <see langword="null" /> if the key is absent from the
    /// resolved view.
    /// </summary>
    /// <param name="key">
    /// The configuration key, in colon-delimited form (e.g. <c>logging:level:default</c>) or the dotted form (
    /// <c>logging.level.default</c>). Both produce the same lookup.
    /// </param>
    /// <returns>The value, or <see langword="null" /> when absent.</returns>
    /// <remarks>
    /// This indexer deliberately returns <see langword="null" /> for an absent key rather than throwing
    /// <see cref="KeyNotFoundException" /> as <see cref="IReadOnlyDictionary{TKey, TValue}.this" /> normally would,
    /// matching <c>Microsoft.Extensions.Configuration</c>'s null-on-absent convention. Use <see cref="ContainsKey" />
    /// to distinguish an absent key from one whose value is <see langword="null" />.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public string? this[string key]
    {
        get
        {
            ThrowHelper.ThrowIfNull(key);

            return LookupValue(Values, key);
        }
    }

    /// <summary>
    /// Looks up a value by canonical colon-delimited key or by the equivalent dotted form. Dotted lookups are
    /// normalized to colon-delimited keys before consulting the backing dictionary.
    /// </summary>
    /// <param name="values">The dictionary of resolved values.</param>
    /// <param name="key">The lookup key in either dotted or colon form.</param>
    /// <returns>The value, or <see langword="null" /> when absent.</returns>
    internal static string? LookupValue(IReadOnlyDictionary<string, string?> values, string key)
    {
        if (values.TryGetValue(key, out var value)) return value;

        if (key.IndexOf('.') < 0) return null;

        var normalized = key.Replace('.', ':');

        return values.TryGetValue(normalized, out value) ? value : null;
    }

    /// <summary>
    /// Gets the underlying resolved dictionary as a read-only view.
    /// </summary>
    /// <returns>The resolved values keyed by configuration key.</returns>
    public IReadOnlyDictionary<string, string?> Values { get; }

    /// <summary>
    /// Gets the configuration keys present in the resolved view.
    /// </summary>
    /// <returns>An enumerable of configuration keys.</returns>
    public IEnumerable<string> Keys => Values.Keys;

    /// <summary>
    /// Gets the number of resolved keys.
    /// </summary>
    /// <returns>The count of keys present in the resolved view.</returns>
    public int Count => Values.Count;

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, string?>> GetEnumerator() =>
        Values.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Gets the resolved-entry origin metadata for every key in the view.
    /// </summary>
    /// <returns>Origin metadata per resolved key.</returns>
    public IEnumerable<ConfigurationResolvedEntry> Entries =>
        _entries.Values;

    /// <summary>
    /// Gets the origin metadata for <paramref name="key" />, or <see langword="null" /> when the key is absent from the
    /// resolved view.
    /// </summary>
    /// <param name="key">The configuration key in either dotted or colon-delimited form.</param>
    /// <returns>The resolved entry, or <see langword="null" /> when absent.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public ConfigurationResolvedEntry? GetEntry(string key)
    {
        ThrowHelper.ThrowIfNull(key);

        if (_entries.TryGetValue(key, out ConfigurationResolvedEntry? entry))
            return entry;

        if (key.IndexOf('.') < 0)
            return null;

        var normalized = key.Replace('.', ':');
        return _entries.TryGetValue(normalized, out entry) ? entry : null;
    }

    /// <summary>
    /// Determines whether the resolved view contains <paramref name="key" />, accepting either the colon-delimited or
    /// the equivalent dotted form.
    /// </summary>
    /// <param name="key">The configuration key, in either dotted or colon-delimited form.</param>
    /// <returns><see langword="true" /> when the key is present; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool ContainsKey(string key)
    {
        ThrowHelper.ThrowIfNull(key);

        if (Values.ContainsKey(key))
            return true;

        return key.IndexOf('.') >= 0 && Values.ContainsKey(key.Replace('.', ':'));
    }

    /// <summary>
    /// Gets the values present in the resolved view. Satisfies
    /// <see cref="IReadOnlyDictionary{TKey, TValue}.Values" />.
    /// </summary>
    /// <returns>An enumerable of the resolved values.</returns>
    IEnumerable<string?> IReadOnlyDictionary<string, string?>.Values =>
        Values.Values;

    /// <summary>
    /// Attempts to get the value for <paramref name="key" /> without throwing, accepting either the colon-delimited or
    /// the equivalent dotted form. Satisfies <see cref="IReadOnlyDictionary{TKey, TValue}.TryGetValue" />.
    /// </summary>
    /// <param name="key">The configuration key, in either dotted or colon-delimited form.</param>
    /// <param name="value">When this method returns <see langword="true" />, contains the resolved value.</param>
    /// <returns><see langword="true" /> when the key is present; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    bool IReadOnlyDictionary<string, string?>.TryGetValue(string key, out string? value)
    {
        ThrowHelper.ThrowIfNull(key);

        if (Values.TryGetValue(key, out value))
            return true;

        if (key.IndexOf('.') >= 0 && Values.TryGetValue(key.Replace('.', ':'), out value))
            return true;

        value = null;
        return false;
    }
}

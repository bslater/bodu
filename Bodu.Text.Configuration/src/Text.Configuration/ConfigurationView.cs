// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationView.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
/// Use
/// <see cref="ConfigurationExtensions.Resolve(Bodu.Text.Ini.IniDocument, string?, ConfigurationResolveOptions?)" />
/// to obtain a view for a target path. The view is a one-shot snapshot — subsequent mutation of the originating
/// document does not retroactively update the view.
/// </para>
/// <para>
/// Values are <see langword="string" />? to match <c>Microsoft.Extensions.Configuration</c>'s
/// <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}" /> shape. A key that resolves to the
/// EditorConfig sentinel <c>unset</c> under <see cref="ConfigurationUnsetValueMode.RemoveEffectiveValue" /> is
/// omitted entirely.
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
/// // Indexer lookup — colon and dotted forms are equivalent.
/// string? level = view["logging:level:default"];
/// string? alt   = view["logging.level.default"]; // same value
///
/// // Typed convenience accessors on the view.
/// int indent = view.GetInt32("format:indent:size", fallback: 4);
///
/// // Enumeration yields canonical colon-delimited keys.
/// foreach (KeyValuePair<string, string?> kv in view)
///     Console.WriteLine($"{kv.Key} = {kv.Value}");
///]]>
/// </example>
public sealed partial class ConfigurationView : IEnumerable<KeyValuePair<string, string?>>
{
    internal ConfigurationView(IReadOnlyDictionary<string, string?> values)
    {
        Values = values;
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

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

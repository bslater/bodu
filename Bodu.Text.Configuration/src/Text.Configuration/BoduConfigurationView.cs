// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationView.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;

namespace Bodu.Text.Configuration;

/// <summary>
/// Represents the resolved snapshot of a configuration document for a specific target path: a flattened
/// dictionary of configuration keys to their effective values, computed by layering preamble and matching
/// sections in source order.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="BoduConfigurationExtensions.Resolve(Bodu.Text.Formats.IniDocument, string?, BoduConfigurationResolveOptions?)" /> to obtain a
/// view for a target path. The view is a one-shot snapshot — subsequent mutation of the originating document
/// does not retroactively update the view.
/// </para>
/// <para>
/// Values are <see langword="string" />? to match <c>Microsoft.Extensions.Configuration</c>'s
/// <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}" /> shape. A key that resolves to
/// the EditorConfig sentinel <c>unset</c> under
/// <see cref="BoduConfigurationUnsetValueMode.RemoveEffectiveValue" /> is omitted entirely.
/// </para>
/// </remarks>
public sealed partial class BoduConfigurationView : IEnumerable<KeyValuePair<string, string?>>
{
    private readonly IReadOnlyDictionary<string, string?> _values;

    internal BoduConfigurationView(IReadOnlyDictionary<string, string?> values)
    {
        this._values = values;
    }

    /// <summary>
    /// Gets the effective value for <paramref name="key" />, or <see langword="null" /> if the key is absent
    /// from the resolved view.
    /// </summary>
    /// <param name="key">The configuration key, in colon-delimited form (e.g. <c>logging:level:default</c>)
    /// or the dotted form (<c>logging.level.default</c>). Both produce the same lookup.</param>
    /// <returns>The value, or <see langword="null" /> when absent.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public string? this[string key]
    {
        get
        {
            ThrowHelper.ThrowIfNull(key);
            return LookupValue(this._values, key);
        }
    }

    /// <summary>
    /// Looks up a value by canonical colon-delimited key or by the equivalent dotted form. Dotted lookups
    /// are normalized to colon-delimited keys before consulting the backing dictionary.
    /// </summary>
    /// <param name="values">The dictionary of resolved values.</param>
    /// <param name="key">The lookup key in either dotted or colon form.</param>
    /// <returns>The value, or <see langword="null" /> when absent.</returns>
    internal static string? LookupValue(IReadOnlyDictionary<string, string?> values, string key)
    {
        if (values.TryGetValue(key, out string? value))
            return value;

        if (key.IndexOf('.') < 0)
            return null;

        string normalized = key.Replace('.', ':');
        return values.TryGetValue(normalized, out value) ? value : null;
    }

    /// <summary>
    /// Gets the underlying resolved dictionary as a read-only view.
    /// </summary>
    /// <returns>The resolved values keyed by configuration key.</returns>
    public IReadOnlyDictionary<string, string?> Values => this._values;

    /// <summary>
    /// Gets the configuration keys present in the resolved view.
    /// </summary>
    /// <returns>An enumerable of configuration keys.</returns>
    public IEnumerable<string> Keys => this._values.Keys;

    /// <summary>
    /// Gets the number of resolved keys.
    /// </summary>
    /// <returns>The count of keys present in the resolved view.</returns>
    public int Count => this._values.Count;

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, string?>> GetEnumerator() =>
        this._values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}

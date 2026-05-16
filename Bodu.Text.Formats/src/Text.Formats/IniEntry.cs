// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniEntry.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bodu.Text.Formats;

/// <summary>
/// Represents a single key/value entry within an INI section.
/// </summary>
public sealed class IniEntry
{

    /// <summary>
    /// Initializes a new instance of the <see cref="IniEntry" /> class.
    /// </summary>
    /// <param name="key">The trimmed key name.</param>
    /// <param name="value">The trimmed value string.</param>
    internal IniEntry(string key, string value)
    {
        Key = key;
        Value = value;
    }

    /// <summary>
    /// Gets the key name of this entry.
    /// </summary>
    /// <returns>The trimmed key string as it appeared in the source.</returns>
    public string Key { get; }

    /// <summary>
    /// Gets the raw string value of this entry.
    /// </summary>
    /// <returns>The trimmed value string as it appeared in the source.</returns>
    public string Value { get; }

    /// <summary>
    /// Parses <see cref="Value" /> as <typeparamref name="T" /> using <see cref="CultureInfo.InvariantCulture" />.
    /// </summary>
    /// <typeparam name="T">The target type. Must implement <see cref="ISpanParsable{TSelf}" />.</typeparam>
    /// <returns>The parsed value.</returns>
    /// <exception cref="FormatException">
    /// Thrown when <see cref="Value" /> cannot be parsed as <typeparamref name="T" />.
    /// </exception>
    public T GetValue<T>()
        where T : ISpanParsable<T> =>
        T.Parse(Value.AsSpan(), CultureInfo.InvariantCulture);

    /// <summary>
    /// Attempts to parse <see cref="Value" /> as <typeparamref name="T" /> using
    /// <see cref="CultureInfo.InvariantCulture" />.
    /// </summary>
    /// <typeparam name="T">The target type. Must implement <see cref="ISpanParsable{TSelf}" />.</typeparam>
    /// <param name="value">
    /// When this method returns <see langword="true" />, contains the parsed result; otherwise, the default value
    /// of <typeparamref name="T" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when parsing succeeded; otherwise, <see langword="false" />.
    /// </returns>
    public bool TryGetValue<T>([MaybeNullWhen(false)] out T value)
        where T : ISpanParsable<T> =>
        T.TryParse(Value.AsSpan(), CultureInfo.InvariantCulture, out value);

}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvEntry.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bodu.Text.Formats;

/// <summary>
/// Represents a single key/value assignment within a DotEnv document.
/// </summary>
public sealed class DotEnvEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvEntry" /> class.
    /// </summary>
    /// <param name="key">The validated key name.</param>
    /// <param name="value">The fully processed value string — quotes stripped, escape sequences resolved.</param>
    internal DotEnvEntry(string key, string value)
    {
        Key = key;
        Value = value;
    }

    /// <summary>
    /// Gets the key name of this entry.
    /// </summary>
    /// <returns>The key string, validated against the <c>[A-Za-z_][A-Za-z0-9_]*</c> pattern.</returns>
    public string Key { get; }

    /// <summary>
    /// Gets the fully processed value of this entry.
    /// </summary>
    /// <returns>
    /// The value string with surrounding quotes removed and escape sequences resolved. Never
    /// <see langword="null" />.
    /// </returns>
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

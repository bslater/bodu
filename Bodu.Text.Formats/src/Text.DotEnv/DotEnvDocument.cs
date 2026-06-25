// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvDocument.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bodu.Text.DotEnv;

/// <summary>
/// Represents a parsed DotEnv document, providing ordered access to entries and O(1) key lookup.
/// </summary>
/// <remarks>
/// <para>
/// Obtain an instance by calling <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> or
/// <see cref="DotEnv.TryParse(ReadOnlySpan{char}, out DotEnvDocument)" />.
/// </para>
/// <para>
/// Key lookup uses <see cref="StringComparer.Ordinal" />. DotEnv keys are case-sensitive by convention.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// DotEnvDocument doc = DotEnv.Parse(text);
///
/// // Direct lookup — throws KeyNotFoundException when the key is missing.
/// string apiKey = doc["API_KEY"];
///
/// // TryGetValue for optional keys.
/// if (doc.TryGetValue("LOG_LEVEL", out string? level))
///     Console.WriteLine($"Log level: {level}");
///
/// // Walk every entry in source order.
/// foreach (DotEnvEntry entry in doc.Entries)
///     Console.WriteLine($"{entry.Key}={entry.Value}");
///]]>
/// </code>
/// </example>
public sealed class DotEnvDocument
{
    /// <summary>The ordered list of entries in source order.</summary>
    private readonly List<DotEnvEntry> _entries;

    /// <summary>The key-to-entry lookup used for fast value resolution.</summary>
    private readonly Dictionary<string, DotEnvEntry> _lookup;

    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvDocument" /> class.
    /// </summary>
    /// <param name="entries">The ordered list of entries.</param>
    /// <param name="lookup">The key-to-entry lookup dictionary.</param>
    internal DotEnvDocument(List<DotEnvEntry> entries, Dictionary<string, DotEnvEntry> lookup)
    {
        _entries = entries;
        _lookup = lookup;
        Entries = _entries.AsReadOnly();
    }

    /// <summary>
    /// Gets the ordered list of entries in this document.
    /// </summary>
    /// <value>
    /// A read-only list of <see cref="DotEnvEntry" /> instances in source order, with duplicates resolved according to
    /// the <see cref="DuplicateKeyPolicy" /> that was active during parsing.
    /// </value>
    public IReadOnlyList<DotEnvEntry> Entries { get; }

    /// <summary>
    /// Gets the value associated with the specified key, or <see langword="null" /> if the key is absent.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <returns>
    /// The string value, or <see langword="null" /> when the key is not present or <paramref name="key" /> is
    /// <see langword="null" />.
    /// </returns>
    public string? this[string key] =>
        key is not null && _lookup.TryGetValue(key, out DotEnvEntry? entry) ? entry.Value : null;

    /// <summary>
    /// Gets the value associated with the specified key as type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The target type. Must implement <see cref="ISpanParsable{TSelf}" />.</typeparam>
    /// <param name="key">The key to look up.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when <paramref name="key" /> is not present in this document.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when the value string cannot be parsed as <typeparamref name="T" />.
    /// </exception>
    public T GetValue<T>(string key)
        where T : ISpanParsable<T>
    {
        ThrowHelper.ThrowIfNull(key);

        return !_lookup.TryGetValue(key, out DotEnvEntry? entry)
            ? throw new KeyNotFoundException(string.Format(CultureInfo.CurrentCulture, FormatsResourceStrings.Op_Invalid_DotEnvKeyNotFound, key))
            : entry.GetValue<T>();
    }

    /// <summary>
    /// Attempts to get the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">
    /// When this method returns <see langword="true" />, contains the string value; otherwise, <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when the key is present; otherwise, <see langword="false" />.</returns>
    public bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
    {
        if (key is not null && _lookup.TryGetValue(key, out DotEnvEntry? entry))
        {
            value = entry.Value;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Attempts to get the value associated with the specified key as type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The target type. Must implement <see cref="ISpanParsable{TSelf}" />.</typeparam>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">
    /// When this method returns <see langword="true" />, contains the parsed result; otherwise, the default value of
    /// <typeparamref name="T" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the key is present and its value was successfully parsed as
    /// <typeparamref name="T" />; otherwise, <see langword="false" />.
    /// </returns>
    public bool TryGetValue<T>(string key, [MaybeNullWhen(false)] out T value)
        where T : ISpanParsable<T>
    {
        if (key is null || !_lookup.TryGetValue(key, out DotEnvEntry? entry))
        {
            value = default;
            return false;
        }

        return entry.TryGetValue<T>(out value);
    }
}

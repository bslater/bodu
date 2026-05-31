// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvEntry.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bodu.Text.DotEnv;

/// <summary>
/// Represents a single key/value assignment within a DotEnv document.
/// </summary>
/// <remarks>
/// <para>
/// Entries are produced by <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> and exposed through
/// <see cref="DotEnvDocument.Entries" />. The <see cref="Key" /> is validated against the conventional
/// <c>[A-Za-z_][A-Za-z0-9_]*</c> identifier pattern, and <see cref="Value" /> is the fully-processed payload —
/// surrounding quotes have been removed and any double-quoted escape sequences (<c>\n</c>, <c>\t</c>, <c>\"</c>,
/// <c>\\</c>, <c>\xHH</c>, and so on) have been resolved.
/// </para>
/// <para>
/// Entries are immutable; construct a new <see cref="DotEnvDocument" /> to alter the value associated with a key rather
/// than mutating the entry in place.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// DotEnvDocument doc = DotEnv.Parse("""
///     DATABASE_URL=postgres://localhost/app
///     DEBUG="true"
///     """);
///
/// foreach (DotEnvEntry entry in doc.Entries)
///     Console.WriteLine($"{entry.Key} = {entry.Value}");
///]]>
/// </example>
public sealed class DotEnvEntry
{
    private static readonly IReadOnlyList<DotEnvComment> EmptyComments = Array.Empty<DotEnvComment>();

    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvEntry" /> class with no associated comments.
    /// </summary>
    /// <param name="key">The validated key name.</param>
    /// <param name="value">The fully processed value string — quotes stripped, escape sequences resolved.</param>
    internal DotEnvEntry(string key, string value)
        : this(key, value, EmptyComments)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvEntry" /> class with the supplied leading comments.
    /// </summary>
    /// <param name="key">The validated key name.</param>
    /// <param name="value">The fully processed value string — quotes stripped, escape sequences resolved.</param>
    /// <param name="leadingComments">The comment lines that precede the entry in source order.</param>
    internal DotEnvEntry(string key, string value, IReadOnlyList<DotEnvComment> leadingComments)
    {
        Key = key;
        Value = value;
        LeadingComments = leadingComments;
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
    /// The value string with surrounding quotes removed and escape sequences resolved. Never <see langword="null" />.
    /// </returns>
    public string Value { get; }

    /// <summary>
    /// Gets the comment lines that immediately precede this entry in the source document, in source order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Populated only when <see cref="DotEnvParseOptions.PreserveComments" /> is <see langword="true" />. The
    /// collection is empty for entries that have no annotating comments and for documents parsed with
    /// <c>PreserveComments = false</c>.
    /// </para>
    /// </remarks>
    /// <returns>A non-null read-only list of <see cref="DotEnvComment" /> instances.</returns>
    public IReadOnlyList<DotEnvComment> LeadingComments { get; }

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
    /// When this method returns <see langword="true" />, contains the parsed result; otherwise, the default value of
    /// <typeparamref name="T" />.
    /// </param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise, <see langword="false" />.</returns>
    public bool TryGetValue<T>([MaybeNullWhen(false)] out T value)
        where T : ISpanParsable<T> =>
        T.TryParse(Value.AsSpan(), CultureInfo.InvariantCulture, out value);
}

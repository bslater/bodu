// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniEntry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bodu.Text.Ini;

/// <summary>
/// Represents a single key/value entry within an <see cref="IniSection" />. The key and value are immutable data;
/// callers assemble documents programmatically through the owning <see cref="IniSection" /> and may edit comment trivia
/// before calling <see cref="Ini.Format(IniDocument)" />.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Key" /> and <see cref="Value" /> are immutable once constructed: the owning section maintains a
/// key-to-entry lookup that depends on the key, and the value forms the entry's data identity. To change a value, call
/// <see cref="IniSection.SetEntry(string, string)" />, which replaces the entry while preserving its trivia.
/// </para>
/// <para>
/// The comment trivia — <see cref="InlineComment" /> and the <see cref="LeadingComments" /> list — remains editable so
/// that annotations can be attached when authoring a document. The <see cref="LineNumber" /> reflects parser-assigned
/// trivia and is read-only.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// // Build a section programmatically and format it back to text.
/// var doc = new IniDocument();
/// IniSection db = doc.GetOrAddSection("database");
/// db.SetEntry("host", "localhost");
/// IniEntry port = db.SetEntry("port", "5432");
/// port.InlineComment = new IniComment('#', " default Postgres port");
///
/// // Key and Value are immutable; replace the value via SetEntry.
/// db.SetEntry("port", "5433");
///
/// string text = Ini.Format(doc);
///]]>
/// </example>
public sealed class IniEntry
{
    /// <summary>
    /// The comments that immediately precede this entry in source order.
    /// </summary>
    private readonly List<IniComment> _leadingComments;

    /// <summary>
    /// Initializes a new instance of the <see cref="IniEntry" /> class.
    /// </summary>
    /// <param name="key">The trimmed key name.</param>
    /// <param name="value">The trimmed value string.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> or <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public IniEntry(string key, string value)
        : this(key, value, lineNumber: 0, leadingComments: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IniEntry" /> class with a source line number.
    /// </summary>
    /// <param name="key">The trimmed key name.</param>
    /// <param name="value">The trimmed value string.</param>
    /// <param name="lineNumber">
    /// The 1-based source line at which this entry was authored, or <c>0</c> when the entry was constructed
    /// programmatically rather than parsed.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> or <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public IniEntry(string key, string value, int lineNumber)
        : this(key, value, lineNumber, leadingComments: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IniEntry" /> class with source line and leading-comment trivia.
    /// </summary>
    /// <param name="key">The trimmed key name.</param>
    /// <param name="value">The trimmed value string.</param>
    /// <param name="lineNumber">The 1-based source line, or <c>0</c> when constructed programmatically.</param>
    /// <param name="leadingComments">
    /// The comments authored on lines that precede this entry, in source order, or <see langword="null" /> when no
    /// comments precede the entry. The supplied sequence is copied; subsequent mutations of the input collection do not
    /// affect this entry.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> or <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public IniEntry(string key, string value, int lineNumber, IReadOnlyList<IniComment>? leadingComments)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfNull(value);

        Key = key;
        Value = value;
        LineNumber = lineNumber;
        _leadingComments = leadingComments is null ? new List<IniComment>() : new List<IniComment>(leadingComments);
    }

    /// <summary>
    /// Gets the key name of this entry.
    /// </summary>
    /// <returns>The trimmed key string as it appeared in the source.</returns>
    public string Key { get; }

    /// <summary>
    /// Gets the raw string value of this entry.
    /// </summary>
    /// <returns>The entry value as supplied at construction. Never <see langword="null" />.</returns>
    public string Value { get; }

    /// <summary>
    /// Gets the 1-based source line number at which this entry was authored, or <c>0</c> when the entry was constructed
    /// programmatically rather than parsed.
    /// </summary>
    /// <returns>A non-negative line number.</returns>
    public int LineNumber { get; }

    /// <summary>
    /// Gets the comments authored on lines preceding this entry, in source order.
    /// </summary>
    /// <returns>
    /// A read-only view onto the section's mutable leading-comment list. The list is empty when no comments precede the
    /// entry or when <see cref="IniParseOptions.PreserveComments" /> was <see langword="false" />.
    /// </returns>
    public IReadOnlyList<IniComment> LeadingComments => _leadingComments;

    /// <summary>
    /// Gets or sets the comment captured on the same line as this entry, or <see langword="null" /> when no inline
    /// comment is present.
    /// </summary>
    /// <returns>The inline comment, or <see langword="null" />.</returns>
    public IniComment? InlineComment { get; set; }

    /// <summary>
    /// Appends a leading comment to this entry.
    /// </summary>
    /// <param name="comment">The comment to append.</param>
    public void AddLeadingComment(IniComment comment) => _leadingComments.Add(comment);

    /// <summary>
    /// Replaces the leading comments of this entry with the supplied sequence.
    /// </summary>
    /// <param name="comments">The new sequence of leading comments.</param>
    /// <exception cref="ArgumentNullException"><paramref name="comments" /> is <see langword="null" />.</exception>
    public void SetLeadingComments(IEnumerable<IniComment> comments)
    {
        ThrowHelper.ThrowIfNull(comments);
        _leadingComments.Clear();
        _leadingComments.AddRange(comments);
    }

    /// <summary>
    /// Removes every leading comment from this entry.
    /// </summary>
    public void ClearLeadingComments() => _leadingComments.Clear();

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

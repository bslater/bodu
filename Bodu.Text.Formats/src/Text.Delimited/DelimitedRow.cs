// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedRow.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Bodu.Text.Delimited;

/// <summary>
/// Represents a single data record in a <see cref="DelimitedDocument" />, exposing its fields by index and — when a
/// header row was present — by column name, with optional typed value conversion.
/// </summary>
/// <remarks>
/// <para>
/// Rows are produced by <see cref="Delimited.Parse(ReadOnlySpan{char})" /> and exposed through
/// <see cref="DelimitedDocument.Rows" />. Field access by name throws <see cref="KeyNotFoundException" /> when the
/// column was not present in the header row, so callers consuming optional columns should test header presence first or
/// use the indexer's fallback overload where available.
/// </para>
/// <para>
/// Typed accessors such as <c>GetInt32</c>, <c>GetDouble</c>, <c>GetBoolean</c>, and <c>GetDateTime</c> parse the
/// underlying string using <see cref="System.Globalization.CultureInfo.InvariantCulture" /> by default and accept an
/// explicit culture for locale-aware sources.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// foreach (DelimitedRow row in doc.Rows)
/// {
///     // By column name when a header row was parsed.
///     string name = row["name"];
///
///     // By zero-based index for headerless data.
///     string firstField = row[0];
///
///     // Typed access with culture-invariant parsing.
///     int    age   = row.GetInt32("age");
///     double price = row.GetDouble("price", fallback: 0.0);
/// }
///]]>
/// </example>
public sealed class DelimitedRow
{
    private static readonly CompositeFormat s_headerNotFound =
        CompositeFormat.Parse(FormatsResourceStrings.Op_Invalid_DelimitedHeaderNotFound);

    /// <summary>
    /// The column-name-to-index map built from the document's header row, or <see langword="null" /> when the document
    /// was parsed without a header row.
    /// </summary>
    private readonly IReadOnlyDictionary<string, int>? _headerIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedRow" /> class.
    /// </summary>
    /// <param name="fields">The ordered list of field values for this row.</param>
    /// <param name="headerIndex">
    /// The column-name-to-index map from the document's header row, or <see langword="null" /> when no header row was
    /// parsed.
    /// </param>
    internal DelimitedRow(IReadOnlyList<string> fields, IReadOnlyDictionary<string, int>? headerIndex)
    {
        Fields = fields;
        _headerIndex = headerIndex;
    }

    /// <summary>
    /// Gets the ordered, unmodified field values for this row.
    /// </summary>
    /// <returns>A read-only list of string field values in source order.</returns>
    public IReadOnlyList<string> Fields { get; }

    /// <summary>
    /// Gets the number of fields in this row.
    /// </summary>
    /// <returns>The number of fields.</returns>
    public int Count => Fields.Count;

    /// <summary>
    /// Gets the field at the specified zero-based index.
    /// </summary>
    /// <param name="index">The zero-based index of the field to retrieve.</param>
    /// <returns>The field value at <paramref name="index" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index" /> is less than zero or greater than or equal to <see cref="Count" />.
    /// </exception>
    public string this[int index] => (uint)index >= (uint)Fields.Count ? throw new ArgumentOutOfRangeException(nameof(index)) : Fields[index];

    /// <summary>
    /// Gets the field in the column with the specified header name.
    /// </summary>
    /// <param name="header">The column header name to look up.</param>
    /// <returns>
    /// The field value for the named column, or <see cref="string.Empty" /> when this row has fewer fields than the
    /// header row.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the document was parsed without a header row.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="header" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when <paramref name="header" /> does not match any column name.
    /// </exception>
    public string this[string header]
    {
        get
        {
            if (_headerIndex is null)
                ThrowNoHeaders();

            ThrowHelper.ThrowIfNull(header);

            if (!_headerIndex.TryGetValue(header, out var index))
                ThrowHeaderNotFound(header);

            return index < Fields.Count ? Fields[index] : string.Empty;
        }
    }

    /// <summary>
    /// Parses the field at the specified zero-based index as <typeparamref name="T" /> using
    /// <see cref="CultureInfo.InvariantCulture" />.
    /// </summary>
    /// <typeparam name="T">The target type. Must implement <see cref="ISpanParsable{TSelf}" />.</typeparam>
    /// <param name="index">The zero-based index of the field to parse.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index" /> is out of range.</exception>
    /// <exception cref="FormatException">
    /// Thrown when the field value cannot be parsed as <typeparamref name="T" />.
    /// </exception>
    public T GetValue<T>(int index)
        where T : ISpanParsable<T>
    {
        return (uint)index >= (uint)Fields.Count
            ? throw new ArgumentOutOfRangeException(nameof(index))
            : T.Parse(Fields[index].AsSpan(), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Attempts to parse the field at the specified zero-based index as <typeparamref name="T" /> using
    /// <see cref="CultureInfo.InvariantCulture" />.
    /// </summary>
    /// <typeparam name="T">The target type. Must implement <see cref="ISpanParsable{TSelf}" />.</typeparam>
    /// <param name="index">The zero-based index of the field to parse.</param>
    /// <param name="value">
    /// When this method returns <see langword="true" />, contains the parsed result; otherwise, the default value of
    /// <typeparamref name="T" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the index is in range and parsing succeeded; otherwise, <see langword="false" />.
    /// </returns>
    public bool TryGetValue<T>(int index, [MaybeNullWhen(false)] out T value)
        where T : ISpanParsable<T>
    {
        if ((uint)index >= (uint)Fields.Count)
        {
            value = default;
            return false;
        }

        return T.TryParse(Fields[index].AsSpan(), CultureInfo.InvariantCulture, out value);
    }

    /// <summary>
    /// Parses the field in the column with the specified header name as <typeparamref name="T" /> using
    /// <see cref="CultureInfo.InvariantCulture" />.
    /// </summary>
    /// <typeparam name="T">The target type. Must implement <see cref="ISpanParsable{TSelf}" />.</typeparam>
    /// <param name="header">The column header name to look up.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the document was parsed without a header row.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="header" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when <paramref name="header" /> does not match any column name.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when the field value cannot be parsed as <typeparamref name="T" />.
    /// </exception>
    public T GetValue<T>(string header)
        where T : ISpanParsable<T>
    {
        var field = this[header];
        return T.Parse(field.AsSpan(), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Attempts to parse the field in the column with the specified header name as <typeparamref name="T" /> using
    /// <see cref="CultureInfo.InvariantCulture" />.
    /// </summary>
    /// <typeparam name="T">The target type. Must implement <see cref="ISpanParsable{TSelf}" />.</typeparam>
    /// <param name="header">The column header name to look up.</param>
    /// <param name="value">
    /// When this method returns <see langword="true" />, contains the parsed result; otherwise, the default value of
    /// <typeparamref name="T" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the header exists and the field was parsed successfully; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public bool TryGetValue<T>(string header, [MaybeNullWhen(false)] out T value)
        where T : ISpanParsable<T>
    {
        if (_headerIndex is null || header is null || !_headerIndex.TryGetValue(header, out var index))
        {
            value = default;
            return false;
        }

        var field = index < Fields.Count ? Fields[index] : string.Empty;
        return T.TryParse(field.AsSpan(), CultureInfo.InvariantCulture, out value);
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException" /> indicating that no header row was parsed.
    /// </summary>
    [DoesNotReturn]
    private static void ThrowNoHeaders() =>
        throw new InvalidOperationException(FormatsResourceStrings.Op_Invalid_DelimitedRowNoHeaders);

    /// <summary>
    /// Throws a <see cref="KeyNotFoundException" /> for an absent column header.
    /// </summary>
    [DoesNotReturn]
    private static void ThrowHeaderNotFound(string header) =>
        throw new KeyNotFoundException(
            string.Format(CultureInfo.InvariantCulture, s_headerNotFound, header));
}

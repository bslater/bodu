// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlElement.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Text.Toml.Document;

/// <summary>
/// Represents a single read-only value within a <see cref="TomlDocument" />. The element is a lightweight view — a pair
/// of the owning document and a row index — so copying it is cheap and never materializes a node.
/// </summary>
/// <remarks>
/// <para>
/// An element is valid only for the lifetime of its owning <see cref="TomlDocument" />. After the document is disposed,
/// any member access throws <see cref="ObjectDisposedException" />.
/// </para>
/// <para>
/// TOML defines eight scalar value kinds — string, integer, float, Boolean, and four date/time forms — alongside arrays
/// and tables. Each scalar accessor returns the value decoded once during parsing and throws
/// <see cref="InvalidOperationException" /> when invoked on an element of a different kind.
/// </para>
/// </remarks>
public readonly partial struct TomlElement
{
    /// <summary>The owning document.</summary>
    private readonly TomlDocument _document;

    /// <summary>The index of this element's row within the owning document's flat metadata index.</summary>
    private readonly int _index;

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlElement" /> struct.
    /// </summary>
    /// <param name="document">The owning document.</param>
    /// <param name="index">The row index of the element within the document.</param>
    internal TomlElement(TomlDocument document, int index)
    {
        _document = document;
        _index = index;
    }

    /// <summary>
    /// Gets the kind of this element.
    /// </summary>
    /// <returns>The value kind.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    public TomlValueKind ValueKind =>
        _document.GetKind(_index);

    /// <summary>
    /// Gets the value of this string element.
    /// </summary>
    /// <returns>The decoded string value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this element is not a <see cref="TomlValueKind.String" />.
    /// </exception>
    public string GetString() =>
        _document.GetString(_index);

    /// <summary>
    /// Gets the value of this integer element as a 64-bit signed integer.
    /// </summary>
    /// <returns>The decoded integer value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this element is not an <see cref="TomlValueKind.Integer" />.
    /// </exception>
    public long GetInt64() =>
        _document.GetInt64(_index);

    /// <summary>
    /// Gets the value of this float element as an IEEE 754 binary64 floating-point value.
    /// </summary>
    /// <returns>The decoded floating-point value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this element is not a <see cref="TomlValueKind.Float" />.
    /// </exception>
    public double GetDouble() =>
        _document.GetDouble(_index);

    /// <summary>
    /// Gets the value of this Boolean element.
    /// </summary>
    /// <returns>The decoded Boolean value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this element is not a <see cref="TomlValueKind.Boolean" />.
    /// </exception>
    public bool GetBoolean() =>
        _document.GetBoolean(_index);

    /// <summary>
    /// Gets the value of this offset-date-time element.
    /// </summary>
    /// <returns>The decoded offset date-time value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this element is not an <see cref="TomlValueKind.OffsetDateTime" />.
    /// </exception>
    public DateTimeOffset GetDateTimeOffset() =>
        _document.GetDateTimeOffset(_index);

    /// <summary>
    /// Gets the value of this local-date-time element.
    /// </summary>
    /// <returns>
    /// The decoded local date-time value, whose <see cref="DateTime.Kind" /> is <see cref="DateTimeKind.Unspecified" />
    /// .
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this element is not a <see cref="TomlValueKind.LocalDateTime" />.
    /// </exception>
    public DateTime GetDateTime() =>
        _document.GetDateTime(_index);

    /// <summary>
    /// Gets the value of this local-date element.
    /// </summary>
    /// <returns>The decoded local date value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this element is not a <see cref="TomlValueKind.LocalDate" />.
    /// </exception>
    public DateOnly GetDateOnly() =>
        _document.GetDateOnly(_index);

    /// <summary>
    /// Gets the value of this local-time element.
    /// </summary>
    /// <returns>The decoded local time value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this element is not a <see cref="TomlValueKind.LocalTime" />.
    /// </exception>
    public TimeOnly GetTimeOnly() =>
        _document.GetTimeOnly(_index);

    /// <summary>
    /// Gets the number of elements in this array element.
    /// </summary>
    /// <returns>The element count.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this element is not an <see cref="TomlValueKind.Array" />.
    /// </exception>
    public int GetArrayLength() =>
        _document.GetArrayLength(_index);

    /// <summary>
    /// Gets the element at the supplied index within this array element.
    /// </summary>
    /// <param name="index">The zero-based index of the element to retrieve.</param>
    /// <returns>The element at <paramref name="index" />.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this element is not an <see cref="TomlValueKind.Array" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index" /> is negative or not less than the array length.
    /// </exception>
    public TomlElement this[int index] =>
        new(_document, _document.GetArrayElementRow(_index, index));

    /// <summary>
    /// Gets the value of the property with the supplied name within this table element.
    /// </summary>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <returns>The value element of the matching property.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="propertyName" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this element is not a <see cref="TomlValueKind.Table" />.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when no property named <paramref name="propertyName" /> exists.
    /// </exception>
    public TomlElement GetProperty(string propertyName)
    {
        ThrowHelper.ThrowIfNull(propertyName);

        if (_document.TryGetProperty(_index, propertyName, out int valueRow))
            return new TomlElement(_document, valueRow);

        throw new KeyNotFoundException(string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.IO_KeyNotFound_Property, propertyName));
    }

    /// <summary>
    /// Attempts to get the value of the property with the supplied name within this table element.
    /// </summary>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <param name="value">
    /// When this method returns, the value element of the matching property; otherwise the default element.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when a matching property was found; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="propertyName" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this element is not a <see cref="TomlValueKind.Table" />.
    /// </exception>
    public bool TryGetProperty(string propertyName, out TomlElement value)
    {
        ThrowHelper.ThrowIfNull(propertyName);

        if (_document.TryGetProperty(_index, propertyName, out int valueRow))
        {
            value = new TomlElement(_document, valueRow);
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Returns an enumerator that iterates the elements of this array element in order.
    /// </summary>
    /// <returns>An <see cref="ArrayEnumerator" /> over the array's elements.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this element is not an <see cref="TomlValueKind.Array" />.
    /// </exception>
    public ArrayEnumerator EnumerateArray()
    {
        int length = _document.GetArrayLength(_index);
        return new ArrayEnumerator(_document, _index, length);
    }

    /// <summary>
    /// Returns an enumerator that iterates the key/value pairs of this table element in stored order.
    /// </summary>
    /// <returns>An <see cref="ObjectEnumerator" /> over the table's properties.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this element is not a <see cref="TomlValueKind.Table" />.
    /// </exception>
    public ObjectEnumerator EnumerateObject()
    {
        int pairs = _document.GetTablePairCount(_index);
        return new ObjectEnumerator(_document, _index, pairs);
    }

    /// <summary>
    /// Returns a textual representation of this element.
    /// </summary>
    /// <returns>
    /// The decoded value for a scalar element, or the literal name of the value kind for an array or table.
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    /// <remarks>
    /// Container values are not re-serialized; the kind name is returned instead to keep the operation
    /// allocation-light. Scalar values are formatted with the invariant culture.
    /// </remarks>
    public override string ToString() =>
        ValueKind switch
        {
            TomlValueKind.String => GetString(),
            TomlValueKind.Integer => GetInt64().ToString(CultureInfo.InvariantCulture),
            TomlValueKind.Float => GetDouble().ToString(CultureInfo.InvariantCulture),
            TomlValueKind.Boolean => GetBoolean() ? "true" : "false",
            TomlValueKind.OffsetDateTime => GetDateTimeOffset().ToString("O", CultureInfo.InvariantCulture),
            TomlValueKind.LocalDateTime => GetDateTime().ToString("O", CultureInfo.InvariantCulture),
            TomlValueKind.LocalDate => GetDateOnly().ToString("O", CultureInfo.InvariantCulture),
            TomlValueKind.LocalTime => GetTimeOnly().ToString("O", CultureInfo.InvariantCulture),
            TomlValueKind.Array => nameof(TomlValueKind.Array),
            _ => nameof(TomlValueKind.Table),
        };
}

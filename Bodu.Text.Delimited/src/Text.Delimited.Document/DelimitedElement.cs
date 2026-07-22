// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedElement.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Text.Delimited.Document;

/// <summary>
/// Represents a single element — the document array, a record, or a field value — within a
/// <see cref="DelimitedDocument" />, shaped after <c>System.Text.Json</c>'s <c>JsonElement</c>.
/// </summary>
public readonly struct DelimitedElement
{
    /// <summary>The row index that identifies the document array element.</summary>
    internal const int DocumentRow = -1;

    /// <summary>The field index that identifies a non-field (array or object) element.</summary>
    internal const int NoField = -1;

    /// <summary>The owning document.</summary>
    private readonly DelimitedDocument _document;

    /// <summary>The record index, or <see cref="DocumentRow" /> for the document array.</summary>
    private readonly int _row;

    /// <summary>The field index within a record, or <see cref="NoField" /> for a container.</summary>
    private readonly int _field;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedElement" /> struct.
    /// </summary>
    /// <param name="document">The owning document.</param>
    /// <param name="row">The record index, or <see cref="DocumentRow" /> for the document array.</param>
    /// <param name="field">The field index, or <see cref="NoField" /> for a container.</param>
    internal DelimitedElement(DelimitedDocument document, int row, int field)
    {
        _document = document;
        _row = row;
        _field = field;
    }

    /// <summary>
    /// Gets the value kind of this element.
    /// </summary>
    /// <value>The element's <see cref="DelimitedValueKind" />.</value>
    public DelimitedValueKind ValueKind
    {
        get
        {
            if (_field != NoField)
                return DelimitedValueKind.String;
            if (_row == DocumentRow)
                return DelimitedValueKind.Array;

            return _document.HasHeader ? DelimitedValueKind.Object : DelimitedValueKind.Array;
        }
    }

    /// <summary>
    /// Gets the number of elements in an array element (records in the document, or fields in a positional record).
    /// </summary>
    /// <returns>The element count.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this element is not an array.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    public int GetArrayLength()
    {
        if (ValueKind != DelimitedValueKind.Array)
            throw new InvalidOperationException();

        return _row == DocumentRow ? _document.RowCount : _document.FieldCount(_row);
    }

    /// <summary>
    /// Gets the string value of a field element.
    /// </summary>
    /// <returns>The field value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this element is not a string.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    public string GetString()
    {
        if (_field == NoField)
            throw new InvalidOperationException();

        return _document.FieldAt(_row, _field);
    }

    /// <summary>
    /// Gets the element at the specified index within an array element.
    /// </summary>
    /// <param name="index">The zero-based index.</param>
    /// <value>The child element.</value>
    /// <exception cref="InvalidOperationException">Thrown when this element is not an array.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    public DelimitedElement this[int index]
    {
        get
        {
            if (ValueKind != DelimitedValueKind.Array)
                throw new InvalidOperationException();

            return _row == DocumentRow
                ? new DelimitedElement(_document, index, NoField)
                : new DelimitedElement(_document, _row, index);
        }
    }

    /// <summary>
    /// Gets the field value element for the specified column name within a record object.
    /// </summary>
    /// <param name="name">The column name.</param>
    /// <returns>The field value element.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when this element is not an object.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the column is not present.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    public DelimitedElement GetProperty(string name)
    {
        if (!TryGetProperty(name, out DelimitedElement value))
            throw new KeyNotFoundException();

        return value;
    }

    /// <summary>
    /// Attempts to get the field value element for the specified column name.
    /// </summary>
    /// <param name="name">The column name.</param>
    /// <param name="value">
    /// When this method returns <see langword="true" />, the field element; otherwise the default element.
    /// </param>
    /// <returns><see langword="true" /> when the column is present with a value in this record.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when this element is not an object.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    public bool TryGetProperty(string name, out DelimitedElement value)
    {
        ThrowHelper.ThrowIfNull(name);

        if (ValueKind != DelimitedValueKind.Object)
            throw new InvalidOperationException();

        int column = _document.IndexOfHeader(name);
        if (column < 0 || column >= _document.FieldCount(_row))
        {
            value = default;
            return false;
        }

        value = new DelimitedElement(_document, _row, column);
        return true;
    }

    /// <summary>
    /// Enumerates the elements of an array element.
    /// </summary>
    /// <returns>An enumerator over the array's elements.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this element is not an array.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    public ArrayEnumerator EnumerateArray()
    {
        if (ValueKind != DelimitedValueKind.Array)
            throw new InvalidOperationException();

        _document.ThrowIfDisposed();

        return new ArrayEnumerator(this, GetArrayLength());
    }

    /// <summary>
    /// Enumerates the fields of a record object element in column order.
    /// </summary>
    /// <returns>An enumerator over the record's fields.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this element is not an object.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    public ObjectEnumerator EnumerateObject()
    {
        if (ValueKind != DelimitedValueKind.Object)
            throw new InvalidOperationException();

        _document.ThrowIfDisposed();

        return new ObjectEnumerator(_document, _row);
    }

    /// <summary>
    /// Enumerates the child elements of a <see cref="DelimitedElement" /> array.
    /// </summary>
    public struct ArrayEnumerator
        : IEnumerator<DelimitedElement>, IEnumerable<DelimitedElement>
    {
        /// <summary>The array element being enumerated.</summary>
        private readonly DelimitedElement _array;

        /// <summary>The number of elements.</summary>
        private readonly int _length;

        /// <summary>The current index, starting at <c>-1</c>.</summary>
        private int _index;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayEnumerator" /> struct.
        /// </summary>
        /// <param name="array">The array element.</param>
        /// <param name="length">The element count.</param>
        internal ArrayEnumerator(DelimitedElement array, int length)
        {
            _array = array;
            _length = length;
            _index = -1;
        }

        /// <inheritdoc />
        public readonly DelimitedElement Current => _array[_index];

        /// <inheritdoc />
        readonly object IEnumerator.Current => Current;

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (_index + 1 >= _length)
                return false;

            _index++;
            return true;
        }

        /// <inheritdoc />
        public void Reset() => _index = -1;

        /// <inheritdoc />
        public readonly void Dispose()
        {
        }

        /// <summary>
        /// Returns this enumerator.
        /// </summary>
        /// <returns>This enumerator.</returns>
        public readonly ArrayEnumerator GetEnumerator() => this;

        /// <inheritdoc />
        readonly IEnumerator<DelimitedElement> IEnumerable<DelimitedElement>.GetEnumerator() => this;

        /// <inheritdoc />
        readonly IEnumerator IEnumerable.GetEnumerator() => this;
    }

    /// <summary>
    /// Enumerates the fields of a <see cref="DelimitedElement" /> record object.
    /// </summary>
    public struct ObjectEnumerator
        : IEnumerator<DelimitedProperty>, IEnumerable<DelimitedProperty>
    {
        /// <summary>The owning document.</summary>
        private readonly DelimitedDocument _document;

        /// <summary>The record index.</summary>
        private readonly int _row;

        /// <summary>The current column index, starting at <c>-1</c>.</summary>
        private int _column;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectEnumerator" /> struct.
        /// </summary>
        /// <param name="document">The owning document.</param>
        /// <param name="row">The record index.</param>
        internal ObjectEnumerator(DelimitedDocument document, int row)
        {
            _document = document;
            _row = row;
            _column = -1;
        }

        /// <inheritdoc />
        public readonly DelimitedProperty Current =>
            new(_document.HeaderAt(_column), new DelimitedElement(_document, _row, _column));

        /// <inheritdoc />
        readonly object IEnumerator.Current => Current;

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (_column + 1 >= _document.FieldCount(_row))
                return false;

            _column++;
            return true;
        }

        /// <inheritdoc />
        public void Reset() => _column = -1;

        /// <inheritdoc />
        public readonly void Dispose()
        {
        }

        /// <summary>
        /// Returns this enumerator.
        /// </summary>
        /// <returns>This enumerator.</returns>
        public readonly ObjectEnumerator GetEnumerator() => this;

        /// <inheritdoc />
        readonly IEnumerator<DelimitedProperty> IEnumerable<DelimitedProperty>.GetEnumerator() => this;

        /// <inheritdoc />
        readonly IEnumerator IEnumerable.GetEnumerator() => this;
    }
}

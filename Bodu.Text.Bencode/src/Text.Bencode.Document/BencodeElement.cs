// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeElement.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Text.Bencode.Document;

/// <summary>
/// Represents a single read-only value within a <see cref="BencodeDocument" />. The element is a lightweight view — a
/// pair of the owning document and a row index — so copying it is cheap and never materializes a node.
/// </summary>
/// <remarks>
/// <para>
/// An element is valid only for the lifetime of its owning <see cref="BencodeDocument" />. After the document is
/// disposed, any member access throws <see cref="ObjectDisposedException" />.
/// </para>
/// <para>
/// Because Bencode (BEP 3) defines only dictionaries, lists, byte strings, and integers, this type has no Boolean,
/// null, or floating-point surface, and exposes byte-string content through both <see cref="GetString" /> (UTF-8 text)
/// and <see cref="GetBytes" /> (raw bytes).
/// </para>
/// </remarks>
public readonly partial struct BencodeElement
{
    /// <summary>The owning document.</summary>
    private readonly BencodeDocument _document;

    /// <summary>The index of this element's row within the owning document's flat metadata index.</summary>
    private readonly int _index;

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeElement" /> struct.
    /// </summary>
    /// <param name="document">The owning document.</param>
    /// <param name="index">The row index of the element within the document.</param>
    internal BencodeElement(BencodeDocument document, int index)
    {
        _document = document;
        _index = index;
    }

    /// <summary>
    /// Gets the kind of this element.
    /// </summary>
    /// <returns>The value kind.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    public BencodeValueKind ValueKind =>
        _document.GetKind(_index);

    /// <summary>
    /// Gets the value of this integer element as a 64-bit signed integer.
    /// </summary>
    /// <returns>The decoded integer value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this element is not an <see cref="BencodeValueKind.Integer" />.
    /// </exception>
    /// <exception cref="BencodeFormatException">
    /// Thrown when the integer's value exceeds <see cref="long.MaxValue" />; use <see cref="GetUInt64" /> to read
    /// values in the upper unsigned 64-bit range.
    /// </exception>
    public long GetInt64() =>
        _document.GetInteger(_index);

    /// <summary>
    /// Attempts to get the value of this integer element as a 64-bit signed integer.
    /// </summary>
    /// <param name="value">When this method returns <see langword="true" />, the integer value; otherwise zero.</param>
    /// <returns>
    /// <see langword="true" /> when the value fits the signed 64-bit range; <see langword="false" /> when it is
    /// readable only through <see cref="GetUInt64" />.
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this element is not an <see cref="BencodeValueKind.Integer" />.
    /// </exception>
    public bool TryGetInt64(out long value) =>
        _document.TryGetInteger(_index, out value);

    /// <summary>
    /// Gets the value of this integer element as a 64-bit unsigned integer, accepting any value in [0,
    /// <see cref="ulong.MaxValue" /> ].
    /// </summary>
    /// <returns>The decoded unsigned integer value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this element is not an <see cref="BencodeValueKind.Integer" />.
    /// </exception>
    /// <exception cref="BencodeFormatException">Thrown when the integer's value is negative.</exception>
    /// <remarks>
    /// Bencode integers are arbitrary-precision per BEP 3, so a document may carry a value between
    /// <see cref="long.MaxValue" /> and <see cref="ulong.MaxValue" /> that <see cref="GetInt64" /> cannot represent;
    /// this accessor reads such values without loss, mirroring <see cref="Reader.Utf8BencodeReader.GetUInt64" />.
    /// </remarks>
    public ulong GetUInt64() =>
        _document.GetUnsignedInteger(_index);

    /// <summary>
    /// Attempts to get the value of this integer element as a 64-bit unsigned integer.
    /// </summary>
    /// <param name="value">
    /// When this method returns <see langword="true" />, the unsigned integer value; otherwise zero.
    /// </param>
    /// <returns><see langword="true" /> when the value is non-negative; otherwise <see langword="false" />.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this element is not an <see cref="BencodeValueKind.Integer" />.
    /// </exception>
    public bool TryGetUInt64(out ulong value)
    {
        if (_document.TryGetInteger(_index, out long signed))
        {
            if (signed < 0)
            {
                value = 0;
                return false;
            }

            value = (ulong)signed;
            return true;
        }

        value = _document.GetUnsignedInteger(_index);
        return true;
    }

    /// <summary>
    /// Decodes this byte-string element as UTF-8 text.
    /// </summary>
    /// <returns>The decoded string.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this element is not a <see cref="BencodeValueKind.ByteString" />.
    /// </exception>
    /// <remarks>
    /// Use <see cref="GetBytes" /> instead when the byte string is not valid UTF-8 text.
    /// </remarks>
    public string GetString() =>
        _document.GetString(_index);

    /// <summary>
    /// Copies this byte-string element's content to a new array.
    /// </summary>
    /// <returns>A copy of the byte-string content.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this element is not a <see cref="BencodeValueKind.ByteString" />.
    /// </exception>
    public byte[] GetBytes() =>
        _document.GetBytes(_index);

    /// <summary>
    /// Copies the complete encoded form of this element to a new array. For a byte string the result includes the
    /// length prefix, for an integer the <c>i…e</c> framing, and for a container both delimiters and every child.
    /// </summary>
    /// <returns>The raw encoded bytes of this element.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    /// <remarks>
    /// Because canonical Bencode is byte-exact, the returned slice is suitable for hashing — for example, computing a
    /// torrent's info-hash from the <c>info</c> dictionary's element — and for verbatim re-emission through
    /// <see cref="Writer.Utf8BencodeWriter.WriteRawValue(ReadOnlySpan{byte}, bool)" />.
    /// </remarks>
    public byte[] GetRawBytes() =>
        _document.GetRawSpan(_index).ToArray();

    /// <summary>
    /// Writes the complete encoded form of this element to the supplied writer.
    /// </summary>
    /// <param name="writer">The destination writer.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this element is the default value and belongs to no document, or when the writer's call sequence
    /// does not permit a value at the current position.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    /// <remarks>
    /// The encoded bytes are emitted verbatim; because the owning document was validated when parsed, no re-validation
    /// occurs.
    /// </remarks>
    public void WriteTo(Writer.Utf8BencodeWriter writer)
    {
        if (_document is null)
            throw new InvalidOperationException(BencodeResourceStrings.Op_Invalid_DefaultElement);

        writer.WriteRawValue(_document.GetRawSpan(_index), skipInputValidation: true);
    }

    /// <summary>
    /// Creates an independent copy of this element whose lifetime is not tied to the owning document.
    /// </summary>
    /// <returns>A <see cref="BencodeElement" /> backed by a private document that does not require disposal.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    public BencodeElement Clone() =>
        BencodeDocument.ParseUnpooled(_document.GetRawSpan(_index)).RootElement;

    /// <summary>
    /// Gets the number of elements in this array element.
    /// </summary>
    /// <returns>The element count.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this element is not an <see cref="BencodeValueKind.Array" />.
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
    /// Thrown when this element is not an <see cref="BencodeValueKind.Array" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index" /> is negative or not less than the array length.
    /// </exception>
    public BencodeElement this[int index] =>
        new(_document, _document.GetArrayElementRow(_index, index));

    /// <summary>
    /// Gets the value of the property with the supplied name within this object element.
    /// </summary>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <returns>The value element of the matching property.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="propertyName" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this element is not an <see cref="BencodeValueKind.Object" />.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when no property named <paramref name="propertyName" /> exists.
    /// </exception>
    public BencodeElement GetProperty(string propertyName)
    {
        ThrowHelper.ThrowIfNull(propertyName);

        if (_document.TryGetProperty(_index, propertyName, out int valueRow))
            return new BencodeElement(_document, valueRow);

        throw new KeyNotFoundException(string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.IO_KeyNotFound_Property, propertyName));
    }

    /// <summary>
    /// Attempts to get the value of the property with the supplied name within this object element.
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
    /// Thrown when this element is not an <see cref="BencodeValueKind.Object" />.
    /// </exception>
    public bool TryGetProperty(string propertyName, out BencodeElement value)
    {
        ThrowHelper.ThrowIfNull(propertyName);

        if (_document.TryGetProperty(_index, propertyName, out int valueRow))
        {
            value = new BencodeElement(_document, valueRow);
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
    /// Thrown when this element is not an <see cref="BencodeValueKind.Array" />.
    /// </exception>
    public ArrayEnumerator EnumerateArray()
    {
        int length = _document.GetArrayLength(_index);
        return new ArrayEnumerator(_document, _index, length);
    }

    /// <summary>
    /// Returns an enumerator that iterates the key/value pairs of this object element in stored order.
    /// </summary>
    /// <returns>An <see cref="ObjectEnumerator" /> over the object's properties.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this element is not an <see cref="BencodeValueKind.Object" />.
    /// </exception>
    public ObjectEnumerator EnumerateObject()
    {
        int pairs = _document.GetObjectPairCount(_index);
        return new ObjectEnumerator(_document, _index, pairs);
    }

    /// <summary>
    /// Returns a textual representation of this element.
    /// </summary>
    /// <returns>
    /// The decimal value for an integer, the UTF-8 text for a byte string, or the literal name of the container kind
    /// for an array or object.
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    /// <remarks>
    /// Container values are not re-serialized; the kind name is returned instead to keep the operation
    /// allocation-light.
    /// </remarks>
    public override string ToString() =>
        _document.GetKind(_index) switch
        {
            BencodeValueKind.Integer => _document.TryGetInteger(_index, out long integer)
                ? integer.ToString(CultureInfo.InvariantCulture)
                : _document.GetUnsignedInteger(_index).ToString(CultureInfo.InvariantCulture),
            BencodeValueKind.ByteString => _document.GetString(_index),
            BencodeValueKind.Array => nameof(BencodeValueKind.Array),
            _ => nameof(BencodeValueKind.Object),
        };
}

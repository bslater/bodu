// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode.Nodes;

/// <summary>
/// Represents a scalar Bencode (BEP 3) value — either an integer or a byte string — within a node tree. Mirrors the
/// role of <see cref="System.Text.Json.Nodes.JsonValue" /> for Bencode.
/// </summary>
/// <remarks>
/// Because Bencode has only two scalar kinds, a <see cref="BencodeValue" /> stores either a 64-bit integer or a byte
/// string; the two are distinguished by <see cref="GetValueKind" />. A string is stored as its UTF-8 byte string, and
/// an integer-valued instance can be read back as any fixed-width integer type through a checked conversion.
/// </remarks>
public sealed class BencodeValue
    : BencodeNode
{
    /// <summary>
    /// The kind of scalar this value holds.
    /// </summary>
    private readonly BencodeValueKind _kind;

    /// <summary>
    /// The integer payload, valid when <see cref="_kind" /> is <see cref="BencodeValueKind.Integer" />.
    /// </summary>
    private readonly long _integer;

    /// <summary>
    /// The byte-string payload, valid when <see cref="_kind" /> is <see cref="BencodeValueKind.ByteString" />.
    /// </summary>
    private readonly byte[] _bytes;

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeValue" /> class holding an integer.
    /// </summary>
    /// <param name="value">The integer payload.</param>
    private BencodeValue(long value)
    {
        _kind = BencodeValueKind.Integer;
        _integer = value;
        _bytes = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeValue" /> class holding a byte string.
    /// </summary>
    /// <param name="value">The byte-string payload, taken by reference without copying.</param>
    private BencodeValue(byte[] value)
    {
        _kind = BencodeValueKind.ByteString;
        _bytes = value;
    }

    /// <summary>
    /// Creates a <see cref="BencodeValue" /> holding the supplied 64-bit integer.
    /// </summary>
    /// <param name="value">The integer value.</param>
    /// <returns>A new integer-valued node.</returns>
    public static BencodeValue Create(long value) =>
        new(value);

    /// <summary>
    /// Creates a <see cref="BencodeValue" /> holding the supplied 32-bit integer.
    /// </summary>
    /// <param name="value">The integer value.</param>
    /// <returns>A new integer-valued node.</returns>
    public static BencodeValue Create(int value) =>
        Create((long)value);

    /// <summary>
    /// Creates a <see cref="BencodeValue" /> holding the supplied string as a UTF-8 byte string.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <returns>A new byte-string node.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public static BencodeValue Create(string value)
    {
        ThrowHelper.ThrowIfNull(value);
        return new BencodeValue(Encoding.UTF8.GetBytes(value));
    }

    /// <summary>
    /// Creates a <see cref="BencodeValue" /> holding a copy of the supplied byte array as a byte string.
    /// </summary>
    /// <param name="value">The byte-string content.</param>
    /// <returns>A new byte-string node.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public static BencodeValue Create(byte[] value)
    {
        ThrowHelper.ThrowIfNull(value);
        return new BencodeValue((byte[])value.Clone());
    }

    /// <summary>
    /// Creates a <see cref="BencodeValue" /> holding a copy of the supplied bytes as a byte string.
    /// </summary>
    /// <param name="value">The byte-string content.</param>
    /// <returns>A new byte-string node.</returns>
    public static BencodeValue Create(ReadOnlySpan<byte> value) =>
        new(value.ToArray());

    /// <inheritdoc />
    public override BencodeValueKind GetValueKind() =>
        _kind;

    /// <summary>
    /// Returns the scalar value converted to the requested type.
    /// </summary>
    /// <typeparam name="T">The type to convert the scalar value to.</typeparam>
    /// <returns>The converted scalar value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the stored value cannot be converted to <typeparamref name="T" />.
    /// </exception>
    public new T GetValue<T>()
    {
        if (TryGetValue(out T value))
            return value;

        throw new InvalidOperationException(
            string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_NodeValueConversion, typeof(T), _kind));
    }

    /// <summary>
    /// Attempts to convert the scalar value to the requested type.
    /// </summary>
    /// <typeparam name="T">The type to convert the scalar value to.</typeparam>
    /// <param name="value">
    /// When this method returns <see langword="true" />, the converted value; otherwise the default value of
    /// <typeparamref name="T" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the stored value was converted to <typeparamref name="T" />; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Integer values convert to any fixed-width integer type through a checked conversion that fails when the value is
    /// out of range, and byte strings convert to <see cref="string" /> (UTF-8 decode) or to a copy of the underlying
    /// <see cref="byte" /> array. Any other combination of stored kind and requested type yields
    /// <see langword="false" />.
    /// </remarks>
    public bool TryGetValue<T>(out T value)
    {
        if (_kind == BencodeValueKind.Integer)
            return TryGetInteger(out value);

        return TryGetByteString(out value);
    }

    /// <inheritdoc />
    public override void WriteTo(Utf8BencodeWriter writer)
    {
        if (_kind == BencodeValueKind.Integer)
            writer.WriteInteger(_integer);
        else
            writer.WriteByteString(_bytes);
    }

    /// <inheritdoc />
    public override BencodeNode DeepClone() =>
        _kind == BencodeValueKind.Integer
            ? new BencodeValue(_integer)
            : new BencodeValue((byte[])_bytes.Clone());

    /// <inheritdoc />
    public override string ToString() =>
        _kind == BencodeValueKind.Integer
            ? _integer.ToString(CultureInfo.InvariantCulture)
            : Encoding.UTF8.GetString(_bytes);

    /// <summary>
    /// Attempts to convert the stored integer to the requested type through a checked conversion.
    /// </summary>
    /// <typeparam name="T">The requested type.</typeparam>
    /// <param name="value">When this method returns <see langword="true" />, the converted value.</param>
    /// <returns>
    /// <see langword="true" /> when <typeparamref name="T" /> is a supported integer type and the value is in range.
    /// </returns>
    private bool TryGetInteger<T>(out T value)
    {
        try
        {
            object? boxed = Type.GetTypeCode(typeof(T)) switch
            {
                TypeCode.SByte => checked((sbyte)_integer),
                TypeCode.Byte => checked((byte)_integer),
                TypeCode.Int16 => checked((short)_integer),
                TypeCode.UInt16 => checked((ushort)_integer),
                TypeCode.Int32 => checked((int)_integer),
                TypeCode.UInt32 => checked((uint)_integer),
                TypeCode.Int64 => _integer,
                TypeCode.UInt64 => checked((ulong)_integer),
                _ => null,
            };

            if (boxed is null)
            {
                value = default!;
                return false;
            }

            value = (T)boxed;
            return true;
        }
        catch (OverflowException)
        {
            value = default!;
            return false;
        }
    }

    /// <summary>
    /// Attempts to convert the stored byte string to the requested type.
    /// </summary>
    /// <typeparam name="T">The requested type.</typeparam>
    /// <param name="value">When this method returns <see langword="true" />, the converted value.</param>
    /// <returns>
    /// <see langword="true" /> when <typeparamref name="T" /> is <see cref="string" /> or <see cref="byte" />[].
    /// </returns>
    private bool TryGetByteString<T>(out T value)
    {
        if (typeof(T) == typeof(string))
        {
            value = (T)(object)Encoding.UTF8.GetString(_bytes);
            return true;
        }

        if (typeof(T) == typeof(byte[]))
        {
            value = (T)(object)(byte[])_bytes.Clone();
            return true;
        }

        value = default!;
        return false;
    }
}

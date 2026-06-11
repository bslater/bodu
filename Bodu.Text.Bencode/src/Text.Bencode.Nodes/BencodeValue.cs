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
/// Represents a scalar Bencode (BEP 3) value — either an integer or a byte string — within a node tree.
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
    /// The integer payload, valid when <see cref="_kind" /> is <see cref="BencodeValueKind.Integer" />. When
    /// <see cref="_integerExceedsInt64" /> is set, the field holds the unchecked bit pattern of an unsigned value above
    /// <see cref="long.MaxValue" />.
    /// </summary>
    private readonly long _integer;

    /// <summary>
    /// Whether the stored integer exceeds <see cref="long.MaxValue" /> and is therefore representable only as
    /// <see cref="ulong" />. Values within the signed range are always stored in signed form, so two equal integers
    /// always share one representation.
    /// </summary>
    private readonly bool _integerExceedsInt64;

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
    /// Initializes a new instance of the <see cref="BencodeValue" /> class holding an unsigned integer above
    /// <see cref="long.MaxValue" />.
    /// </summary>
    /// <param name="value">The unsigned integer payload, which must exceed <see cref="long.MaxValue" />.</param>
    /// <param name="exceedsInt64">Discriminates this constructor; always <see langword="true" />.</param>
    private BencodeValue(ulong value, bool exceedsInt64)
    {
        _kind = BencodeValueKind.Integer;
        _integer = unchecked((long)value);
        _integerExceedsInt64 = exceedsInt64;
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
    /// Creates a <see cref="BencodeValue" /> holding the supplied unsigned 64-bit integer, permitting the full
    /// <see cref="ulong" /> range.
    /// </summary>
    /// <param name="value">The unsigned integer value.</param>
    /// <returns>A new integer-valued node.</returns>
    /// <remarks>
    /// Bencode integers are arbitrary-precision per BEP 3, so values between <see cref="long.MaxValue" /> and
    /// <see cref="ulong.MaxValue" /> are valid documents. Such a value reads back through
    /// <c>GetValue&lt;ulong&gt;()</c>; requesting a narrower integer type fails. Values within the signed 64-bit range
    /// are stored identically to <see cref="Create(long)" />.
    /// </remarks>
    public static BencodeValue Create(ulong value) =>
        value <= long.MaxValue
            ? new BencodeValue((long)value)
            : new BencodeValue(value, exceedsInt64: true);

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
        if (_kind != BencodeValueKind.Integer)
            writer.WriteByteString(_bytes);
        else if (_integerExceedsInt64)
            writer.WriteInteger(unchecked((ulong)_integer));
        else
            writer.WriteInteger(_integer);
    }

    /// <inheritdoc />
    public override BencodeNode DeepClone() =>
        _kind == BencodeValueKind.Integer
            ? _integerExceedsInt64
                ? new BencodeValue(unchecked((ulong)_integer), exceedsInt64: true)
                : new BencodeValue(_integer)
            : new BencodeValue((byte[])_bytes.Clone());

    /// <inheritdoc />
    public override string ToString() =>
        _kind != BencodeValueKind.Integer
            ? Encoding.UTF8.GetString(_bytes)
            : _integerExceedsInt64
                ? unchecked((ulong)_integer).ToString(CultureInfo.InvariantCulture)
                : _integer.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Determines whether this integer value equals another, comparing the normalized representation so signed and
    /// unsigned storage of the same number compare equal.
    /// </summary>
    /// <param name="other">The other integer value.</param>
    /// <returns><see langword="true" /> when both values represent the same integer.</returns>
    internal bool IntegerEquals(BencodeValue other) =>
        _integerExceedsInt64 == other._integerExceedsInt64 && _integer == other._integer;

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
        // A value above long.MaxValue is representable only as UInt64; every narrower request fails.
        if (_integerExceedsInt64)
        {
            if (typeof(T) == typeof(ulong))
            {
                value = (T)(object)unchecked((ulong)_integer);
                return true;
            }

            value = default!;
            return false;
        }

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
    /// <see langword="true" /> when <typeparamref name="T" /> is <see cref="string" /> or <see cref="byte" /> [].
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

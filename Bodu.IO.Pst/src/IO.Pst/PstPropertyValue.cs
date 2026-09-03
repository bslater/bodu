// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstPropertyValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace Bodu.IO.Pst;

/// <summary>
/// Represents one property value of a property context or table-context row: the 16-bit property identifier, the raw
/// wire type code, and the resolved little-endian payload with typed accessors over it.
/// </summary>
/// <remarks>
/// <para>
/// This layer carries no MAPI semantics: the wire type is the raw MS-OXCDATA code and the payload is the value's
/// little-endian bytes. Multi-valued and object-typed payloads are surfaced raw — decoding them is a format reader's
/// concern.
/// </para>
/// <para>
/// Each typed accessor requires the matching wire type and a payload of at least the type's width;
/// <see cref="GetString" /> decodes the UTF-16LE string type (<c>0x001F</c>) only — the code-page string type
/// (<c>0x001E</c>) stays bytes, because resolving its code page is a format-layer concern.
/// </para>
/// </remarks>
public readonly struct PstPropertyValue
{
    /// <summary>The resolved value payload.</summary>
    private readonly ReadOnlyMemory<byte> _data;

    /// <summary>
    /// Initializes a new instance of the <see cref="PstPropertyValue" /> struct.
    /// </summary>
    /// <param name="propertyId">The 16-bit property identifier.</param>
    /// <param name="wireType">The 16-bit wire type code.</param>
    /// <param name="data">The resolved little-endian payload.</param>
    internal PstPropertyValue(ushort propertyId, ushort wireType, ReadOnlyMemory<byte> data)
    {
        PropertyId = propertyId;
        WireType = wireType;
        _data = data;
    }

    /// <summary>
    /// Gets the 16-bit property identifier.
    /// </summary>
    /// <value>The property identifier.</value>
    public ushort PropertyId { get; }

    /// <summary>
    /// Gets the raw 16-bit property wire type code.
    /// </summary>
    /// <value>The MS-OXCDATA type code, unchanged from the file.</value>
    public ushort WireType { get; }

    /// <summary>
    /// Gets the resolved value payload.
    /// </summary>
    /// <value>
    /// The value's little-endian bytes: inline values normalized to their natural width, heap- and subnode-resident
    /// values materialized in full. Empty for a null value.
    /// </value>
    public ReadOnlyMemory<byte> RawData => _data;

    /// <summary>
    /// Reads the value as a 16-bit signed integer (wire type <c>0x0002</c>).
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="InvalidOperationException">The wire type or payload width does not match.</exception>
    public short GetInt16() =>
        BinaryPrimitives.ReadInt16LittleEndian(Require(0x0002, 2));

    /// <summary>
    /// Reads the value as a 32-bit signed integer (wire type <c>0x0003</c>, or the 32-bit error code <c>0x000A</c>).
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="InvalidOperationException">The wire type or payload width does not match.</exception>
    public int GetInt32() =>
        BinaryPrimitives.ReadInt32LittleEndian(Require(WireType == 0x000A ? (ushort)0x000A : (ushort)0x0003, 4));

    /// <summary>
    /// Reads the value as a 64-bit signed integer (wire types <c>0x0014</c>, <c>0x0006</c>, and the FILETIME
    /// <c>0x0040</c>).
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="InvalidOperationException">The wire type or payload width does not match.</exception>
    public long GetInt64()
    {
        ushort expected = WireType is 0x0006 or 0x0040 ? WireType : (ushort)0x0014;
        return BinaryPrimitives.ReadInt64LittleEndian(Require(expected, 8));
    }

    /// <summary>
    /// Reads the value as a Boolean (wire type <c>0x000B</c>): any nonzero byte is <see langword="true" />.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="InvalidOperationException">The wire type or payload width does not match.</exception>
    public bool GetBoolean() =>
        Require(0x000B, 1)[0] != 0;

    /// <summary>
    /// Reads the value as a 32-bit floating-point number (wire type <c>0x0004</c>).
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="InvalidOperationException">The wire type or payload width does not match.</exception>
    public float GetSingle() =>
        BinaryPrimitives.ReadSingleLittleEndian(Require(0x0004, 4));

    /// <summary>
    /// Reads the value as a 64-bit floating-point number (wire type <c>0x0005</c>, or the floating time
    /// <c>0x0007</c>).
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="InvalidOperationException">The wire type or payload width does not match.</exception>
    public double GetDouble()
    {
        ushort expected = WireType == 0x0007 ? (ushort)0x0007 : (ushort)0x0005;
        return BinaryPrimitives.ReadDoubleLittleEndian(Require(expected, 8));
    }

    /// <summary>
    /// Reads the value as a GUID (wire type <c>0x0048</c>).
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="InvalidOperationException">The wire type or payload width does not match.</exception>
    public Guid GetGuid() =>
        new(Require(0x0048, 16));

    /// <summary>
    /// Reads the value as a string (the UTF-16LE wire type <c>0x001F</c> only).
    /// </summary>
    /// <returns>The decoded string.</returns>
    /// <exception cref="InvalidOperationException">The wire type does not match.</exception>
    public string GetString()
    {
        if (WireType != 0x001F)
            throw Mismatch(nameof(String));

        return Encoding.Unicode.GetString(_data.Span);
    }

    /// <summary>
    /// Reads the value's payload as a byte array.
    /// </summary>
    /// <returns>A copy of the resolved payload.</returns>
    public byte[] GetBytes() =>
        _data.ToArray();

    /// <summary>
    /// Returns a textual form of the value for diagnostics.
    /// </summary>
    /// <returns>The property identifier, wire type, and payload length.</returns>
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"0x{PropertyId:X4} (0x{WireType:X4}, {_data.Length} bytes)");

    /// <summary>
    /// Validates the wire type and minimum payload width for a typed accessor and returns the payload.
    /// </summary>
    /// <param name="expectedWireType">The wire type the accessor requires.</param>
    /// <param name="size">The minimum payload width in bytes.</param>
    /// <returns>The payload bytes.</returns>
    /// <exception cref="InvalidOperationException">The wire type or payload width does not match.</exception>
    private ReadOnlySpan<byte> Require(ushort expectedWireType, int size)
    {
        if (WireType != expectedWireType || _data.Length < size)
            throw Mismatch(expectedWireType.ToString("X4", CultureInfo.InvariantCulture));

        // Slice to the value's own width so a longer payload decodes its leading bytes rather than failing the
        // fixed-width read.
        return _data.Span.Slice(0, size);
    }

    /// <summary>
    /// Creates the accessor-mismatch exception.
    /// </summary>
    /// <param name="target">The accessor's target description.</param>
    /// <returns>The exception to throw.</returns>
    private InvalidOperationException Mismatch(string target) =>
        new(string.Format(CultureInfo.CurrentCulture, PstResourceStrings.Op_Invalid_PstPropertyValueType, WireType, target));
}

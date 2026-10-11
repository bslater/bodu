// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PropertySetBytes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Text;

namespace Bodu.IO.Compound.PropertySets;

/// <summary>
/// Builds the raw bytes of OLE property sets for tests that need layouts the authoring API cannot produce: unaligned
/// strings, misplaced values, malformed lengths and types the writer does not emit.
/// </summary>
/// <remarks>
/// A set is the 28-byte header ([MS-OLEPS] 2.21), one locator per section, then the sections in order. A section is its
/// size and property count, one identifier and offset pair per property, then each property's bytes exactly as given,
/// one after another with nothing added, so a test controls every byte of every value, alignment included.
/// </remarks>
internal static class PropertySetBytes
{
    /// <summary>The format identifier of the summary information section, <c>FMTID_SummaryInformation</c>.</summary>
    public static readonly Guid SummaryInformation = new("F29F85E0-4FF9-1068-AB91-08002B27B3D9");

    /// <summary>The format identifier of the document summary section, <c>FMTID_DocSummaryInformation</c>.</summary>
    public static readonly Guid DocumentSummaryInformation = new("D5CDD502-2E9C-101B-9397-08002B2CF9AE");

    /// <summary>The format identifier of the user-defined section, <c>FMTID_UserDefinedProperties</c>.</summary>
    public static readonly Guid UserDefinedProperties = new("D5CDD505-2E9C-101B-9397-08002B2CF9AE");

    /// <summary>The size, in bytes, of the property-set header that precedes the section locators.</summary>
    private const int HeaderSize = 28;

    /// <summary>The size, in bytes, of one section locator: a format identifier and an offset.</summary>
    private const int LocatorSize = 20;

    /// <summary>
    /// Builds a property set holding one section.
    /// </summary>
    /// <param name="formatId">The section's format identifier.</param>
    /// <param name="properties">
    /// The section's properties, each an identifier and the bytes stored at its offset.
    /// </param>
    /// <returns>The property-set bytes.</returns>
    public static byte[] Single(Guid formatId, params (int Id, byte[] Value)[] properties) =>
        Sections((formatId, properties, null));

    /// <summary>
    /// Builds a property set holding one section whose size field declares the given value.
    /// </summary>
    /// <param name="formatId">The section's format identifier.</param>
    /// <param name="declaredSize">The value written to the section's size field.</param>
    /// <param name="properties">
    /// The section's properties, each an identifier and the bytes stored at its offset.
    /// </param>
    /// <returns>The property-set bytes.</returns>
    public static byte[] SingleWithDeclaredSize(Guid formatId, int declaredSize, params (int Id, byte[] Value)[] properties) =>
        Sections((formatId, properties, declaredSize));

    /// <summary>
    /// Builds a property set holding the given sections, in order.
    /// </summary>
    /// <param name="sections">
    /// Each section's format identifier, its properties, and the size its size field declares, or
    /// <see langword="null" /> for its actual size.
    /// </param>
    /// <returns>The property-set bytes.</returns>
    public static byte[] Sections(params (Guid FormatId, (int Id, byte[] Value)[] Properties, int? DeclaredSize)[] sections)
    {
        var bodies = new List<byte[]>();
        foreach ((Guid _, (int Id, byte[] Value)[] properties, int? declaredSize) in sections)
            bodies.Add(Section(properties, declaredSize));

        int offset = HeaderSize + (LocatorSize * sections.Length);
        byte[] data = new byte[offset + bodies.Sum(body => body.Length)];
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(0), 0xFFFE);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(4), 0x0105);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(6), 0x0002);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(24), (uint)sections.Length);
        for (int i = 0; i < sections.Length; i++)
        {
            int locator = HeaderSize + (LocatorSize * i);
            _ = sections[i].FormatId.TryWriteBytes(data.AsSpan(locator, 16));
            BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(locator + 16), (uint)offset);
            bodies[i].CopyTo(data, offset);
            offset += bodies[i].Length;
        }

        return data;
    }

    /// <summary>
    /// Builds a typed property value: a type word, two padding bytes, and the value's bytes.
    /// </summary>
    /// <param name="type">The property type, such as <c>0x001E</c> for <c>VT_LPSTR</c>.</param>
    /// <param name="parts">The value's bytes, concatenated in order.</param>
    /// <returns>The typed value.</returns>
    public static byte[] Typed(ushort type, params byte[][] parts)
    {
        byte[] value = new byte[4 + parts.Sum(part => part.Length)];
        BinaryPrimitives.WriteUInt16LittleEndian(value, type);
        int at = 4;
        foreach (byte[] part in parts)
        {
            part.CopyTo(value, at);
            at += part.Length;
        }

        return value;
    }

    /// <summary>
    /// Builds the code-page property value, a <c>VT_I2</c> padded to four bytes.
    /// </summary>
    /// <param name="codePage">The code page.</param>
    /// <returns>The typed value.</returns>
    public static byte[] CodePage(int codePage) =>
        Typed(0x0002, UInt16((ushort)codePage), [0, 0]);

    /// <summary>
    /// Builds a <c>VT_I4</c> property value.
    /// </summary>
    /// <param name="value">The integer.</param>
    /// <returns>The typed value.</returns>
    public static byte[] Int32(int value) =>
        Typed(0x0003, UInt32((uint)value));

    /// <summary>
    /// Encodes an unsigned 16-bit integer little-endian.
    /// </summary>
    /// <param name="value">The integer.</param>
    /// <returns>The two bytes.</returns>
    public static byte[] UInt16(ushort value)
    {
        byte[] bytes = new byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(bytes, value);
        return bytes;
    }

    /// <summary>
    /// Encodes an unsigned 32-bit integer little-endian.
    /// </summary>
    /// <param name="value">The integer.</param>
    /// <returns>The four bytes.</returns>
    public static byte[] UInt32(uint value)
    {
        byte[] bytes = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        return bytes;
    }

    /// <summary>
    /// Encodes a signed 64-bit integer little-endian.
    /// </summary>
    /// <param name="value">The integer.</param>
    /// <returns>The eight bytes.</returns>
    public static byte[] Int64(long value)
    {
        byte[] bytes = new byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(bytes, value);
        return bytes;
    }

    /// <summary>
    /// Encodes text as single-byte Latin-1 characters, the encoding the tests' code page 1252 strings use.
    /// </summary>
    /// <param name="text">The text, ASCII or Latin-1.</param>
    /// <returns>One byte per character.</returns>
    public static byte[] Latin1(string text) =>
        Encoding.Latin1.GetBytes(text);

    /// <summary>
    /// Encodes text as UTF-16LE.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>Two bytes per UTF-16 code unit.</returns>
    public static byte[] Utf16(string text) =>
        Encoding.Unicode.GetBytes(text);

    /// <summary>
    /// Builds one section: its size and property count, the identifier and offset pairs, then the values.
    /// </summary>
    /// <param name="properties">The properties, each an identifier and its bytes.</param>
    /// <param name="declaredSize">
    /// The value to write to the size field, or <see langword="null" /> for the actual size.
    /// </param>
    /// <returns>The section bytes.</returns>
    private static byte[] Section((int Id, byte[] Value)[] properties, int? declaredSize)
    {
        int headerSize = 8 + (8 * properties.Length);
        byte[] section = new byte[headerSize + properties.Sum(property => property.Value.Length)];
        BinaryPrimitives.WriteUInt32LittleEndian(section.AsSpan(0), (uint)(declaredSize ?? section.Length));
        BinaryPrimitives.WriteUInt32LittleEndian(section.AsSpan(4), (uint)properties.Length);
        int offset = headerSize;
        for (int i = 0; i < properties.Length; i++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(section.AsSpan(8 + (8 * i)), (uint)properties[i].Id);
            BinaryPrimitives.WriteUInt32LittleEndian(section.AsSpan(12 + (8 * i)), (uint)offset);
            properties[i].Value.CopyTo(section, offset);
            offset += properties[i].Value.Length;
        }

        return section;
    }
}

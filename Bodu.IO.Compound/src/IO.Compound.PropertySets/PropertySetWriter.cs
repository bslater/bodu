// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PropertySetWriter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace Bodu.IO.Compound.PropertySets;

/// <summary>
/// Serializes an <see cref="OlePropertySet" /> into the OLE property-set byte layout (MS-OLEPS), the exact inverse of
/// <see cref="PropertySetReader" />.
/// </summary>
/// <remarks>
/// The writer emits the property-set header, the section locators, and each section's property identifier/offset table
/// followed by four-byte-aligned typed property values. The section code page is emitted as property identifier 1, and
/// a user-defined section's name dictionary as property identifier 0, so the result reads back identically.
/// </remarks>
internal static class PropertySetWriter
{
    /// <summary>The little-endian byte-order marker.</summary>
    private const ushort ByteOrderMarker = 0xFFFE;

    /// <summary>The fixed property-set header size, in bytes.</summary>
    private const int HeaderSize = 28;

    /// <summary>The size, in bytes, of a single section locator.</summary>
    private const int LocatorSize = 20;

    /// <summary>The property identifier of the name dictionary.</summary>
    private const int DictionaryPropertyId = 0;

    /// <summary>The property identifier of the code-page property.</summary>
    private const int CodePagePropertyId = 1;

    /// <summary>
    /// Serializes a property set to its byte form.
    /// </summary>
    /// <param name="set">The property set to serialize.</param>
    /// <returns>The serialized bytes.</returns>
    internal static byte[] Write(OlePropertySet set)
    {
        byte[][] bodies = new byte[set.Sections.Count][];
        for (int i = 0; i < set.Sections.Count; i++)
            bodies[i] = EncodeSection(set.Sections[i]);

        int total = HeaderSize + (set.Sections.Count * LocatorSize);
        total = Align4(total);
        int[] offsets = new int[bodies.Length];
        for (int i = 0; i < bodies.Length; i++)
        {
            offsets[i] = total;
            total += bodies[i].Length;
            total = Align4(total);
        }

        byte[] result = new byte[total];
        Span<byte> head = result;
        BinaryPrimitives.WriteUInt16LittleEndian(head, ByteOrderMarker);
        BinaryPrimitives.WriteUInt16LittleEndian(head.Slice(2), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(head.Slice(4), 0x00020000);
        _ = set.ClassId.TryWriteBytes(head.Slice(8, 16));
        BinaryPrimitives.WriteUInt32LittleEndian(head.Slice(24), (uint)set.Sections.Count);

        for (int i = 0; i < set.Sections.Count; i++)
        {
            int locator = HeaderSize + (i * LocatorSize);
            _ = set.Sections[i].FormatId.TryWriteBytes(head.Slice(locator, 16));
            BinaryPrimitives.WriteUInt32LittleEndian(head.Slice(locator + 16), (uint)offsets[i]);
            bodies[i].CopyTo(result.AsSpan(offsets[i]));
        }

        return result;
    }

    /// <summary>
    /// Encodes a single section body.
    /// </summary>
    /// <param name="section">The section to encode.</param>
    /// <returns>The section body bytes.</returns>
    private static byte[] EncodeSection(OlePropertySection section)
    {
        Encoding encoding = ResolveEncoding(section.CodePage);

        // Collect the properties to emit: the code page (PID 1), the optional dictionary (PID 0), and the values.
        List<(int Pid, byte[] Bytes)> emitted = new()
        {
            (CodePagePropertyId, EncodeValue(new OlePropertyValue { Type = OlePropertyType.Int16, Value = (short)section.CodePage }, encoding)),
        };

        if (section.PropertyNames.Count > 0)
            emitted.Add((DictionaryPropertyId, EncodeDictionary(section.PropertyNames, encoding)));

        foreach (KeyValuePair<int, OlePropertyValue> property in section.Properties)
        {
            if (property.Key is CodePagePropertyId or DictionaryPropertyId)
                continue;

            emitted.Add((property.Key, EncodeValue(property.Value, encoding)));
        }

        int count = emitted.Count;
        int valueBase = Align4(8 + (count * 8));
        int bodyLength = valueBase;
        foreach ((int _, byte[] bytes) in emitted)
            bodyLength = Align4(bodyLength + bytes.Length);

        byte[] body = new byte[bodyLength];
        BinaryPrimitives.WriteUInt32LittleEndian(body.AsSpan(0), (uint)bodyLength);
        BinaryPrimitives.WriteUInt32LittleEndian(body.AsSpan(4), (uint)count);

        int cursor = valueBase;
        for (int i = 0; i < count; i++)
        {
            int pair = 8 + (i * 8);
            BinaryPrimitives.WriteUInt32LittleEndian(body.AsSpan(pair), (uint)emitted[i].Pid);
            BinaryPrimitives.WriteUInt32LittleEndian(body.AsSpan(pair + 4), (uint)cursor);
            emitted[i].Bytes.CopyTo(body.AsSpan(cursor));
            cursor = Align4(cursor + emitted[i].Bytes.Length);
        }

        return body;
    }

    /// <summary>
    /// Encodes a single typed property value.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="encoding">The encoding for ANSI strings.</param>
    /// <returns>The encoded typed-property-value bytes.</returns>
    /// <exception cref="CompoundFileSerializationException">Thrown when the value type cannot be encoded.</exception>
    private static byte[] EncodeValue(OlePropertyValue value, Encoding encoding)
    {
        if (value.IsVector)
            throw Unsupported(value.Type);

        return value.Type switch
        {
            OlePropertyType.Int16 => Typed(value.Type, ToBytes16(Convert.ToInt16(value.Value, CultureInfo.InvariantCulture))),
            OlePropertyType.Boolean => Typed(value.Type, ToBytes16((short)((value.Value is true) ? -1 : 0))),
            OlePropertyType.Int32 => Typed(value.Type, ToBytes32(Convert.ToInt32(value.Value, CultureInfo.InvariantCulture))),
            OlePropertyType.UInt32 => Typed(value.Type, ToBytes32(unchecked((int)Convert.ToUInt32(value.Value, CultureInfo.InvariantCulture)))),
            OlePropertyType.Float64 => Typed(value.Type, ToBytes64(BitConverter.DoubleToInt64Bits(Convert.ToDouble(value.Value, CultureInfo.InvariantCulture)))),
            OlePropertyType.Int64 => Typed(value.Type, ToBytes64(Convert.ToInt64(value.Value, CultureInfo.InvariantCulture))),
            OlePropertyType.FileTime => Typed(value.Type, ToBytes64(Convert.ToInt64(value.Value, CultureInfo.InvariantCulture))),
            OlePropertyType.AnsiString or OlePropertyType.BinaryString => Typed(value.Type, EncodeCodePageString((string)value.Value!, encoding)),
            OlePropertyType.UnicodeString => Typed(value.Type, EncodeUnicodeString((string)value.Value!)),
            OlePropertyType.Blob or OlePropertyType.ClipboardData => Typed(value.Type, EncodeBlob((byte[])value.Value!)),
            _ => throw Unsupported(value.Type),
        };
    }

    /// <summary>
    /// Prefixes a value body with its four-byte type word and pads the result to a four-byte boundary.
    /// </summary>
    /// <param name="type">The property type.</param>
    /// <param name="body">The encoded value body.</param>
    /// <returns>The typed-property-value bytes.</returns>
    private static byte[] Typed(OlePropertyType type, byte[] body)
    {
        byte[] result = new byte[Align4(4 + body.Length)];
        BinaryPrimitives.WriteUInt16LittleEndian(result, (ushort)type);
        body.CopyTo(result.AsSpan(4));
        return result;
    }

    /// <summary>
    /// Encodes a length-prefixed code-page (ANSI) string including its null terminator.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <param name="encoding">The encoding to apply.</param>
    /// <returns>The encoded bytes.</returns>
    private static byte[] EncodeCodePageString(string value, Encoding encoding)
    {
        byte[] text = encoding.GetBytes(value);
        byte[] result = new byte[4 + text.Length + 1];
        BinaryPrimitives.WriteUInt32LittleEndian(result, (uint)(text.Length + 1));
        text.CopyTo(result.AsSpan(4));
        return result;
    }

    /// <summary>
    /// Encodes a length-prefixed UTF-16 string including its null terminator.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <returns>The encoded bytes.</returns>
    private static byte[] EncodeUnicodeString(string value)
    {
        byte[] text = Encoding.Unicode.GetBytes(value);
        byte[] result = new byte[4 + text.Length + 2];
        BinaryPrimitives.WriteUInt32LittleEndian(result, (uint)(value.Length + 1));
        text.CopyTo(result.AsSpan(4));
        return result;
    }

    /// <summary>
    /// Encodes a length-prefixed binary blob.
    /// </summary>
    /// <param name="value">The blob bytes.</param>
    /// <returns>The encoded bytes.</returns>
    private static byte[] EncodeBlob(byte[] value)
    {
        byte[] result = new byte[4 + value.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(result, (uint)value.Length);
        value.CopyTo(result.AsSpan(4));
        return result;
    }

    /// <summary>
    /// Encodes the user-defined name dictionary (property identifier 0).
    /// </summary>
    /// <param name="names">The property-name dictionary.</param>
    /// <param name="encoding">The encoding for ANSI names.</param>
    /// <returns>The encoded dictionary bytes (without the leading type word).</returns>
    private static byte[] EncodeDictionary(IReadOnlyDictionary<int, string> names, Encoding encoding)
    {
        using MemoryStream buffer = new();
        Span<byte> word = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(word, (uint)names.Count);
        buffer.Write(word);

        foreach (KeyValuePair<int, string> entry in names)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(word, (uint)entry.Key);
            buffer.Write(word);

            byte[] text = encoding.GetBytes(entry.Value);
            BinaryPrimitives.WriteUInt32LittleEndian(word, (uint)(text.Length + 1));
            buffer.Write(word);
            buffer.Write(text);
            buffer.WriteByte(0);
        }

        return buffer.ToArray();
    }

    /// <summary>
    /// Resolves a code-page number to an encoding, matching the reader.
    /// </summary>
    /// <param name="codePage">The code-page number.</param>
    /// <returns>The resolved encoding.</returns>
    private static Encoding ResolveEncoding(int codePage) =>
        codePage switch
        {
            1200 => Encoding.Unicode,
            1201 => Encoding.BigEndianUnicode,
            65001 => Encoding.UTF8,
            _ => Encoding.Latin1,
        };

    /// <summary>
    /// Encodes a 16-bit value to little-endian bytes.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The two-byte little-endian encoding.</returns>
    private static byte[] ToBytes16(short value)
    {
        byte[] bytes = new byte[2];
        BinaryPrimitives.WriteInt16LittleEndian(bytes, value);
        return bytes;
    }

    /// <summary>
    /// Encodes a 32-bit value to little-endian bytes.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The four-byte little-endian encoding.</returns>
    private static byte[] ToBytes32(int value)
    {
        byte[] bytes = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        return bytes;
    }

    /// <summary>
    /// Encodes a 64-bit value to little-endian bytes.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The eight-byte little-endian encoding.</returns>
    private static byte[] ToBytes64(long value)
    {
        byte[] bytes = new byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(bytes, value);
        return bytes;
    }

    /// <summary>
    /// Rounds a value up to the next four-byte boundary.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The aligned value.</returns>
    private static int Align4(int value) =>
        (value + 3) & ~3;

    /// <summary>
    /// Creates an exception for an unsupported property type.
    /// </summary>
    /// <param name="type">The unsupported type.</param>
    /// <returns>The exception to throw.</returns>
    private static CompoundFileSerializationException Unsupported(OlePropertyType type) =>
        new(string.Format(CultureInfo.CurrentCulture, CompoundResourceStrings.Op_Invalid_CompoundPropertySetCodePage, type));
}

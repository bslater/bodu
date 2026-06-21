// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PropertySetReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Text;

namespace Bodu.IO.Compound.PropertySets;

/// <summary>
/// Parses the serialized byte layout of an OLE property set (MS-OLEPS) into an <see cref="OlePropertySet" />.
/// </summary>
/// <remarks>
/// The reader validates the property-set header, walks the section locators, and decodes each section's typed property
/// values, honoring the per-section code page for ANSI strings and the special dictionary property used by user-defined
/// sections. All structural failures are reported as <see cref="CompoundFileFormatException" />.
/// </remarks>
internal static class PropertySetReader
{
    /// <summary>The expected little-endian byte-order marker at the start of a property-set stream.</summary>
    private const ushort ByteOrderMarker = 0xFFFE;

    /// <summary>The fixed size, in bytes, of the property-set header preceding the section locators.</summary>
    private const int HeaderSize = 28;

    /// <summary>The size, in bytes, of a single section locator (FMTID plus offset).</summary>
    private const int LocatorSize = 20;

    /// <summary>The default code page used to decode ANSI strings when a section declares none.</summary>
    private const int DefaultCodePage = 1252;

    /// <summary>The property identifier of the dictionary that names user-defined properties.</summary>
    private const int DictionaryPropertyId = 0;

    /// <summary>The property identifier of the code-page property.</summary>
    private const int CodePagePropertyId = 1;

    /// <summary>The bit that flags a vector value within a property type.</summary>
    private const ushort VectorFlag = 0x1000;

    /// <summary>The mask isolating the base type from the type-and-modifier word.</summary>
    private const ushort TypeMask = 0x0FFF;

    /// <summary>
    /// Reads and parses an OLE property set from its serialized bytes.
    /// </summary>
    /// <param name="data">The property-set bytes.</param>
    /// <returns>The parsed <see cref="OlePropertySet" />.</returns>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the data is not a well-formed property set.
    /// </exception>
    internal static OlePropertySet Read(ReadOnlySpan<byte> data)
    {
        Ensure(data, 0, HeaderSize);
        CompoundThrowHelper.ThrowFormatIf(
            BinaryPrimitives.ReadUInt16LittleEndian(data) != ByteOrderMarker,
            CompoundResourceStrings.Format_Invalid_CompoundPropertySet,
            CompoundFileError.InvalidPropertySet);

        var classId = new Guid(data.Slice(8, 16));
        uint sectionCount = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(24));
        CompoundThrowHelper.ThrowFormatIf(
            sectionCount is 0 or > 2,
            CompoundResourceStrings.Format_Invalid_CompoundPropertySet,
            CompoundFileError.InvalidPropertySet);

        List<OlePropertySection> sections = new((int)sectionCount);
        Guid firstFormatId = Guid.Empty;
        int locatorCursor = HeaderSize;

        for (uint i = 0; i < sectionCount; i++)
        {
            Ensure(data, locatorCursor, LocatorSize);
            var formatId = new Guid(data.Slice(locatorCursor, 16));
            int sectionOffset = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(locatorCursor + 16)));
            locatorCursor += LocatorSize;

            if (i == 0)
                firstFormatId = formatId;

            sections.Add(ParseSection(data, sectionOffset, formatId));
        }

        int codePage = sections.Count > 0 ? sections[0].CodePage : DefaultCodePage;
        return new OlePropertySet(firstFormatId, classId, codePage, sections);
    }

    /// <summary>
    /// Parses a single section at the specified offset.
    /// </summary>
    /// <param name="data">The property-set bytes.</param>
    /// <param name="sectionOffset">The byte offset of the section from the start of the stream.</param>
    /// <param name="formatId">The section format identifier.</param>
    /// <returns>The parsed <see cref="OlePropertySection" />.</returns>
    /// <exception cref="CompoundFileFormatException">Thrown when the section is malformed.</exception>
    private static OlePropertySection ParseSection(ReadOnlySpan<byte> data, int sectionOffset, Guid formatId)
    {
        Ensure(data, sectionOffset, 8);
        int sectionSize = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(sectionOffset)));
        int propertyCount = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(sectionOffset + 4)));
        Ensure(data, sectionOffset, sectionSize);

        (int pid, int offset)[] pairs = new (int, int)[propertyCount];
        int pairCursor = sectionOffset + 8;
        for (int i = 0; i < propertyCount; i++)
        {
            Ensure(data, pairCursor, 8);
            int pid = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(pairCursor)));
            int offset = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(pairCursor + 4)));
            pairs[i] = (pid, offset);
            pairCursor += 8;
        }

        int codePage = ResolveCodePage(data, sectionOffset, pairs);
        Encoding encoding = ResolveEncoding(codePage);

        Dictionary<int, OlePropertyValue> properties = new(propertyCount);
        Dictionary<int, string> propertyNames = new();

        foreach ((int pid, int offset) in pairs)
        {
            int valueOffset = sectionOffset + offset;

            // Decode each property independently: an unsupported or malformed individual value is skipped so the
            // remaining well-formed properties of the section are still surfaced.
            try
            {
                if (pid == DictionaryPropertyId)
                    ParseDictionary(data, valueOffset, encoding, propertyNames);
                else
                    properties[pid] = ParseTypedValue(data, valueOffset, encoding);
            }
            catch (CompoundFileFormatException)
            {
                // Skip the individual property; the section remains usable.
            }
        }

        return new OlePropertySection(formatId, codePage, properties, propertyNames);
    }

    /// <summary>
    /// Resolves the section code page from its code-page property, defaulting when absent.
    /// </summary>
    /// <param name="data">The property-set bytes.</param>
    /// <param name="sectionOffset">The byte offset of the section.</param>
    /// <param name="pairs">The parsed property identifier/offset pairs.</param>
    /// <returns>The resolved code page.</returns>
    private static int ResolveCodePage(ReadOnlySpan<byte> data, int sectionOffset, (int pid, int offset)[] pairs)
    {
        foreach ((int pid, int offset) in pairs)
        {
            if (pid != CodePagePropertyId)
                continue;

            int valueOffset = sectionOffset + offset;
            Ensure(data, valueOffset, 6);

            // The code page is a VT_I2 whose value is interpreted as an unsigned 16-bit code-page number.
            return BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(valueOffset + 4));
        }

        return DefaultCodePage;
    }

    /// <summary>
    /// Parses a typed property value (a scalar or vector PROPVARIANT) at the specified offset.
    /// </summary>
    /// <param name="data">The property-set bytes.</param>
    /// <param name="offset">The byte offset of the typed value.</param>
    /// <param name="encoding">The encoding used to decode ANSI strings.</param>
    /// <returns>The decoded <see cref="OlePropertyValue" />.</returns>
    /// <exception cref="CompoundFileFormatException">Thrown when the value is malformed.</exception>
    private static OlePropertyValue ParseTypedValue(ReadOnlySpan<byte> data, int offset, Encoding encoding)
    {
        Ensure(data, offset, 4);
        ushort rawType = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset));
        bool isVector = (rawType & VectorFlag) != 0;
        var type = (OlePropertyType)(rawType & TypeMask);
        int valueOffset = offset + 4;

        if (!isVector)
        {
            (object? value, _) = ReadScalar(data, valueOffset, type, encoding);
            return new OlePropertyValue { Type = type, IsVector = false, Value = value };
        }

        Ensure(data, valueOffset, 4);
        int count = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(valueOffset)));
        object?[] items = new object?[count];
        int cursor = valueOffset + 4;
        for (int i = 0; i < count; i++)
        {
            (object? value, int consumed) = ReadScalar(data, cursor, type, encoding);
            CompoundThrowHelper.ThrowFormatIf(consumed <= 0, CompoundResourceStrings.Format_Invalid_CompoundPropertySet, CompoundFileError.InvalidPropertySet);
            items[i] = value;
            cursor += Align4(consumed);
        }

        return new OlePropertyValue { Type = type, IsVector = true, Value = items };
    }

    /// <summary>
    /// Reads a single scalar value of the specified type at the given offset.
    /// </summary>
    /// <param name="data">The property-set bytes.</param>
    /// <param name="offset">The byte offset of the scalar value.</param>
    /// <param name="type">The base scalar type to decode.</param>
    /// <param name="encoding">The encoding used to decode ANSI strings.</param>
    /// <returns>The decoded value and the number of bytes consumed (before vector alignment).</returns>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the value is malformed or of an unsupported type.
    /// </exception>
    private static (object? Value, int Consumed) ReadScalar(ReadOnlySpan<byte> data, int offset, OlePropertyType type, Encoding encoding)
    {
        switch (type)
        {
            case OlePropertyType.Empty:
            case OlePropertyType.Null:
                return (null, 0);

            case OlePropertyType.Int16:
                Ensure(data, offset, 2);
                return (BinaryPrimitives.ReadInt16LittleEndian(data.Slice(offset)), 2);

            case OlePropertyType.UInt16:
                Ensure(data, offset, 2);
                return (BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset)), 2);

            case OlePropertyType.Boolean:
                Ensure(data, offset, 2);
                return (BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset)) != 0, 2);

            case OlePropertyType.Int8:
                Ensure(data, offset, 1);
                return ((sbyte)data[offset], 1);

            case OlePropertyType.UInt8:
                Ensure(data, offset, 1);
                return (data[offset], 1);

            case OlePropertyType.Int32:
                Ensure(data, offset, 4);
                return (BinaryPrimitives.ReadInt32LittleEndian(data.Slice(offset)), 4);

            case OlePropertyType.UInt32:
                Ensure(data, offset, 4);
                return (BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset)), 4);

            case OlePropertyType.Float32:
                Ensure(data, offset, 4);
                return (BinaryPrimitives.ReadSingleLittleEndian(data.Slice(offset)), 4);

            case OlePropertyType.Float64:
                Ensure(data, offset, 8);
                return (BinaryPrimitives.ReadDoubleLittleEndian(data.Slice(offset)), 8);

            case OlePropertyType.Int64:
                Ensure(data, offset, 8);
                return (BinaryPrimitives.ReadInt64LittleEndian(data.Slice(offset)), 8);

            case OlePropertyType.UInt64:
                Ensure(data, offset, 8);
                return (BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(offset)), 8);

            case OlePropertyType.Currency:
                Ensure(data, offset, 8);
                return (BinaryPrimitives.ReadInt64LittleEndian(data.Slice(offset)) / 10000m, 8);

            case OlePropertyType.Date:
                Ensure(data, offset, 8);
                return (OleDateToDateTimeOffset(BinaryPrimitives.ReadDoubleLittleEndian(data.Slice(offset))), 8);

            case OlePropertyType.FileTime:
                Ensure(data, offset, 8);
                return (BinaryPrimitives.ReadInt64LittleEndian(data.Slice(offset)), 8);

            case OlePropertyType.AnsiString:
            case OlePropertyType.BinaryString:
                return ReadCodePageString(data, offset, encoding);

            case OlePropertyType.UnicodeString:
                return ReadUnicodeString(data, offset);

            case OlePropertyType.Blob:
            case OlePropertyType.ClipboardData:
                return ReadBlob(data, offset);

            case OlePropertyType.Variant:
                return ReadVariant(data, offset, encoding);

            default:
                CompoundThrowHelper.ThrowFormat(CompoundResourceStrings.Format_Invalid_CompoundPropertySet, CompoundFileError.InvalidPropertySet);
                return (null, 0);
        }
    }

    /// <summary>
    /// Reads a self-describing variant element, decoding the inner type and value.
    /// </summary>
    /// <param name="data">The property-set bytes.</param>
    /// <param name="offset">The byte offset of the variant's inner type word.</param>
    /// <param name="encoding">The encoding used to decode ANSI strings.</param>
    /// <returns>The decoded inner value and the number of bytes consumed, including the four-byte type prefix.</returns>
    private static (object? Value, int Consumed) ReadVariant(ReadOnlySpan<byte> data, int offset, Encoding encoding)
    {
        Ensure(data, offset, 4);
        var innerType = (OlePropertyType)(BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset)) & TypeMask);
        (object? value, int consumed) = ReadScalar(data, offset + 4, innerType, encoding);
        return (value, 4 + consumed);
    }

    /// <summary>
    /// Reads a length-prefixed code-page (ANSI) string.
    /// </summary>
    /// <param name="data">The property-set bytes.</param>
    /// <param name="offset">The byte offset of the length prefix.</param>
    /// <param name="encoding">The encoding used to decode the bytes.</param>
    /// <returns>The decoded string and the number of bytes consumed.</returns>
    private static (object? Value, int Consumed) ReadCodePageString(ReadOnlySpan<byte> data, int offset, Encoding encoding)
    {
        Ensure(data, offset, 4);
        int length = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset)));
        Ensure(data, offset + 4, length);

        string text = encoding.GetString(data.Slice(offset + 4, length)).TrimEnd('\0');
        return (text, 4 + length);
    }

    /// <summary>
    /// Reads a length-prefixed UTF-16 string.
    /// </summary>
    /// <param name="data">The property-set bytes.</param>
    /// <param name="offset">The byte offset of the length prefix.</param>
    /// <returns>The decoded string and the number of bytes consumed.</returns>
    private static (object? Value, int Consumed) ReadUnicodeString(ReadOnlySpan<byte> data, int offset)
    {
        Ensure(data, offset, 4);
        int characters = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset)));
        int bytes = checked(characters * 2);
        Ensure(data, offset + 4, bytes);

        string text = Encoding.Unicode.GetString(data.Slice(offset + 4, bytes)).TrimEnd('\0');
        return (text, 4 + bytes);
    }

    /// <summary>
    /// Reads a length-prefixed binary blob.
    /// </summary>
    /// <param name="data">The property-set bytes.</param>
    /// <param name="offset">The byte offset of the length prefix.</param>
    /// <returns>The blob bytes and the number of bytes consumed.</returns>
    private static (object? Value, int Consumed) ReadBlob(ReadOnlySpan<byte> data, int offset)
    {
        Ensure(data, offset, 4);
        int length = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset)));
        Ensure(data, offset + 4, length);

        return (data.Slice(offset + 4, length).ToArray(), 4 + length);
    }

    /// <summary>
    /// Parses a dictionary property that maps property identifiers to human-readable names.
    /// </summary>
    /// <param name="data">The property-set bytes.</param>
    /// <param name="offset">The byte offset of the dictionary.</param>
    /// <param name="encoding">The encoding used to decode ANSI names.</param>
    /// <param name="names">The dictionary receiving the parsed names.</param>
    /// <exception cref="CompoundFileFormatException">Thrown when the dictionary is malformed.</exception>
    private static void ParseDictionary(ReadOnlySpan<byte> data, int offset, Encoding encoding, Dictionary<int, string> names)
    {
        Ensure(data, offset, 4);
        int entryCount = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset)));
        int cursor = offset + 4;

        bool unicode = ReferenceEquals(encoding, Encoding.Unicode);
        for (int i = 0; i < entryCount; i++)
        {
            Ensure(data, cursor, 8);
            int pid = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(cursor)));
            int length = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(cursor + 4)));
            cursor += 8;

            if (unicode)
            {
                int bytes = checked(length * 2);
                Ensure(data, cursor, bytes);
                names[pid] = Encoding.Unicode.GetString(data.Slice(cursor, bytes)).TrimEnd('\0');
                cursor += Align4(bytes);
            }
            else
            {
                Ensure(data, cursor, length);
                names[pid] = encoding.GetString(data.Slice(cursor, length)).TrimEnd('\0');
                cursor += length;
            }
        }
    }

    /// <summary>
    /// Resolves a code-page number to an <see cref="Encoding" /> without requiring the code-pages provider.
    /// </summary>
    /// <param name="codePage">The code-page number.</param>
    /// <returns>The resolved encoding; falls back to Latin-1 for single-byte code pages.</returns>
    private static Encoding ResolveEncoding(int codePage) =>
        codePage switch
        {
            1200 => Encoding.Unicode,
            1201 => Encoding.BigEndianUnicode,
            65001 => Encoding.UTF8,
            _ => Encoding.Latin1,
        };

    /// <summary>
    /// Converts an OLE automation date to a UTC <see cref="DateTimeOffset" />.
    /// </summary>
    /// <param name="oaDate">The OLE automation date.</param>
    /// <returns>The corresponding time, or <see langword="null" /> when the value is out of range.</returns>
    private static DateTimeOffset? OleDateToDateTimeOffset(double oaDate)
    {
        try
        {
            return new DateTimeOffset(DateTime.SpecifyKind(DateTime.FromOADate(oaDate), DateTimeKind.Utc), TimeSpan.Zero);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>
    /// Rounds a byte count up to the next four-byte boundary.
    /// </summary>
    /// <param name="value">The byte count.</param>
    /// <returns>The aligned byte count.</returns>
    private static int Align4(int value) =>
        (value + 3) & ~3;

    /// <summary>
    /// Validates that a region lies within the data, throwing when it does not.
    /// </summary>
    /// <param name="data">The property-set bytes.</param>
    /// <param name="offset">The region start offset.</param>
    /// <param name="length">The region length.</param>
    /// <exception cref="CompoundFileFormatException">Thrown when the region is out of range.</exception>
    private static void Ensure(ReadOnlySpan<byte> data, int offset, int length) =>
        CompoundThrowHelper.ThrowFormatIf(
            offset < 0 || length < 0 || offset > data.Length - length,
            CompoundResourceStrings.Format_Invalid_CompoundPropertySet,
            CompoundFileError.InvalidPropertySet);
}

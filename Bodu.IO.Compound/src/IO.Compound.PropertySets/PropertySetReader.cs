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
/// sections. All structural failures are reported as <see cref="CompoundFileFormatException" />. Every 32-bit field is
/// read unsigned: a property identifier keeps all 32 bits, and a length, offset or count is compared with the bytes
/// present before it is narrowed, so a value that cannot be represented is malformed like any other out-of-range value.
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

    /// <summary>The bits of the type word that carry <c>VT_ARRAY</c>, <c>VT_BYREF</c> and <c>VT_RESERVED</c>, none of which the reader implements.</summary>
    private const ushort UnsupportedModifierMask = 0xE000;

    /// <summary>The maximum nesting depth permitted for self-describing <c>VT_VARIANT</c> values.</summary>
    /// <remarks>
    /// A property value of type <see cref="OlePropertyType.Variant" /> can itself hold a variant, and a crafted
    /// property set can nest them without bound. Capping the depth converts a would-be
    /// <see cref="StackOverflowException" /> into a catchable <see cref="CompoundFileFormatException" />.
    /// </remarks>
    private const int MaxVariantDepth = 32;

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
            int sectionOffset = Bounded(BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(locatorCursor + 16)), data.Length);
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
        // The declared size must fit in the data, though a value stored past it is still read from its own offset.
        Ensure(data, sectionOffset, 8);
        _ = Bounded(BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(sectionOffset)), data.Length - sectionOffset);
        uint rawPropertyCount = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(sectionOffset + 4));

        // Each property contributes an 8-byte (pid, offset) pair immediately after the section header. Bound the
        // declared count against the bytes actually present before allocating, so a crafted count cannot drive a
        // multi-gigabyte allocation (and OutOfMemoryException) from a tiny stream.
        CompoundThrowHelper.ThrowFormatIf(
            rawPropertyCount > (uint)((data.Length - (sectionOffset + 8)) / 8),
            CompoundResourceStrings.Format_Invalid_CompoundPropertySet,
            CompoundFileError.InvalidPropertySet);
        int propertyCount = (int)rawPropertyCount;

        // A property identifier is an unsigned 32-bit value; one at or above 0x80000000, such as PID_LOCALE, is keyed by
        // its unchecked Int32 value. An offset is checked against the section when its property is read.
        (int Pid, uint Offset)[] pairs = new (int, uint)[propertyCount];
        int pairCursor = sectionOffset + 8;
        for (int i = 0; i < propertyCount; i++)
        {
            Ensure(data, pairCursor, 8);
            int pid = unchecked((int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(pairCursor)));
            uint offset = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(pairCursor + 4));
            pairs[i] = (pid, offset);
            pairCursor += 8;
        }

        int codePage = ResolveCodePage(data, sectionOffset, pairs);
        Encoding encoding = ResolveEncoding(codePage);

        Dictionary<int, OlePropertyValue> properties = new(propertyCount);
        Dictionary<int, string> propertyNames = new();

        foreach ((int pid, uint offset) in pairs)
        {
            // Decode each property independently: an unsupported or malformed individual value, an offset past the end
            // of the data included, is skipped so the remaining well-formed properties of the section are still
            // surfaced.
            try
            {
                int valueOffset = sectionOffset + Bounded(offset, data.Length - sectionOffset);
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
    private static int ResolveCodePage(ReadOnlySpan<byte> data, int sectionOffset, (int Pid, uint Offset)[] pairs)
    {
        foreach ((int pid, uint offset) in pairs)
        {
            if (pid != CodePagePropertyId)
                continue;

            int valueOffset = sectionOffset + Bounded(offset, data.Length - sectionOffset);
            Ensure(data, valueOffset, 6);

            // The code page is a VT_I2 whose value is interpreted as an unsigned 16-bit code-page number.
            return BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(valueOffset + 4));
        }

        return DefaultCodePage;
    }

    /// <summary>
    /// Parses a typed property value (a scalar or vector PROPVARIANT) at the specified offset.
    /// </summary>
    /// <remarks>
    /// A vector's string, blob and variant elements read whether they are padded to a multiple of four bytes or follow
    /// one another unpadded; see <see cref="SkipPadding" />. Its elements of a fixed size follow one another with no
    /// padding, as [MS-OLEPS] 2.15 packs them.
    /// </remarks>
    /// <param name="data">The property-set bytes.</param>
    /// <param name="offset">The byte offset of the typed value.</param>
    /// <param name="encoding">The encoding used to decode ANSI strings.</param>
    /// <returns>The decoded <see cref="OlePropertyValue" />.</returns>
    /// <exception cref="CompoundFileFormatException">Thrown when the value is malformed.</exception>
    private static OlePropertyValue ParseTypedValue(ReadOnlySpan<byte> data, int offset, Encoding encoding)
    {
        Ensure(data, offset, 4);
        ushort rawType = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset));
        ThrowIfUnsupportedModifier(rawType);
        bool isVector = (rawType & VectorFlag) != 0;
        var type = (OlePropertyType)(rawType & TypeMask);
        int valueOffset = offset + 4;

        if (!isVector)
        {
            (object? value, _) = ReadScalar(data, valueOffset, type, encoding, 0);
            return new OlePropertyValue { Type = type, IsVector = false, Value = value };
        }

        Ensure(data, valueOffset, 4);
        uint rawCount = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(valueOffset));

        // Every vector element consumes at least one byte, so a count exceeding the remaining bytes is malformed;
        // bound it before allocating so a crafted count cannot drive a huge allocation from a tiny stream.
        CompoundThrowHelper.ThrowFormatIf(
            rawCount > (uint)(data.Length - (valueOffset + 4)),
            CompoundResourceStrings.Format_Invalid_CompoundPropertySet,
            CompoundFileError.InvalidPropertySet);
        int count = (int)rawCount;
        object?[] items = new object?[count];
        int cursor = valueOffset + 4;
        for (int i = 0; i < count; i++)
        {
            (object? value, int consumed) = ReadScalar(data, cursor, type, encoding, 0);
            CompoundThrowHelper.ThrowFormatIf(consumed <= 0, CompoundResourceStrings.Format_Invalid_CompoundPropertySet, CompoundFileError.InvalidPropertySet);
            items[i] = value;

            // [MS-OLEPS] 2.15 packs the elements of a fixed size, 1- and 2-byte values included, and pads only the end
            // of the vector.
            cursor = HasVariableLength(type) ? SkipPadding(data, cursor, consumed) : cursor + consumed;
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
    /// <param name="depth">The current <c>VT_VARIANT</c> nesting depth used to bound recursion.</param>
    /// <returns>The decoded value and the number of bytes consumed (before vector alignment).</returns>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the value is malformed or of an unsupported type.
    /// </exception>
    private static (object? Value, int Consumed) ReadScalar(ReadOnlySpan<byte> data, int offset, OlePropertyType type, Encoding encoding, int depth)
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
                return ReadVariant(data, offset, encoding, depth);

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
    /// <param name="depth">The current nesting depth used to bound self-describing variant recursion.</param>
    /// <returns>
    /// The decoded inner value and the number of bytes consumed, including the four-byte type prefix.
    /// </returns>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the value is malformed or the variant nesting exceeds <see cref="MaxVariantDepth" />.
    /// </exception>
    private static (object? Value, int Consumed) ReadVariant(ReadOnlySpan<byte> data, int offset, Encoding encoding, int depth)
    {
        CompoundThrowHelper.ThrowFormatIf(
            depth >= MaxVariantDepth,
            CompoundResourceStrings.Format_Invalid_CompoundPropertySet,
            CompoundFileError.InvalidPropertySet);

        Ensure(data, offset, 4);
        ushort rawInnerType = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset));
        ThrowIfUnsupportedModifier(rawInnerType);
        var innerType = (OlePropertyType)(rawInnerType & TypeMask);
        (object? value, int consumed) = ReadScalar(data, offset + 4, innerType, encoding, depth + 1);
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
        int length = Bounded(BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset)), data.Length - (offset + 4));

        string text = encoding.GetString(data.Slice(offset + 4, length)).TrimEnd('\0');
        return (text, 4 + length);
    }

    /// <summary>
    /// Reads a length-prefixed UTF-16 string.
    /// </summary>
    /// <param name="data">The property-set bytes.</param>
    /// <param name="offset">The byte offset of the length prefix.</param>
    /// <returns>The decoded string and the number of bytes consumed.</returns>
    /// <remarks>
    /// Some writers align the string, rather than the property, to a four-byte boundary: for a property that begins two
    /// bytes past one, they put the length two bytes later than [MS-OLEPS] does. When the length at the expected place
    /// runs past the data or frames a string with no terminating null, and the place is two bytes past a boundary, the
    /// string is read from the next boundary if a null-terminated one starts there, as Apache POI reads it; otherwise
    /// it is read from the expected place, or dropped when that runs past the data.
    /// </remarks>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the string runs past the data and no string starts on the next boundary.
    /// </exception>
    private static (object? Value, int Consumed) ReadUnicodeString(ReadOnlySpan<byte> data, int offset)
    {
        Ensure(data, offset, 4);
        bool framed = TryFrameUnicodeString(data, offset, out int bytes, out bool terminated);
        bool rereadable = !(framed && terminated) && offset % 4 == 2;
        if (rereadable
            && TryFrameUnicodeString(data, offset + 2, out int alignedBytes, out bool alignedTerminated)
            && alignedTerminated
            && alignedBytes > 0)
        {
            string aligned = Encoding.Unicode.GetString(data.Slice(offset + 6, alignedBytes)).TrimEnd('\0');
            return (aligned, 6 + alignedBytes);
        }

        CompoundThrowHelper.ThrowFormatIf(!framed, CompoundResourceStrings.Format_Invalid_CompoundPropertySet, CompoundFileError.InvalidPropertySet);
        string text = Encoding.Unicode.GetString(data.Slice(offset + 4, bytes)).TrimEnd('\0');
        return (text, 4 + bytes);
    }

    /// <summary>
    /// Determines whether a UTF-16 string's length prefix at an offset frames a string within the data.
    /// </summary>
    /// <param name="data">The property-set bytes.</param>
    /// <param name="offset">The byte offset of the length prefix.</param>
    /// <param name="bytes">When this method returns <see langword="true" />, the number of bytes of the string.</param>
    /// <param name="terminated">
    /// When this method returns <see langword="true" />, whether the string is empty or ends in a null character.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the length prefix and the characters it counts lie within the data; otherwise
    /// <see langword="false" />.
    /// </returns>
    private static bool TryFrameUnicodeString(ReadOnlySpan<byte> data, int offset, out int bytes, out bool terminated)
    {
        bytes = 0;
        terminated = false;
        if (offset > data.Length - 4)
            return false;

        ulong length = (ulong)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset)) * 2;
        if (length > (ulong)(data.Length - (offset + 4)))
            return false;

        bytes = (int)length;
        terminated = bytes == 0 || (data[offset + 4 + bytes - 2] == 0 && data[offset + 4 + bytes - 1] == 0);
        return true;
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
        int length = Bounded(BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset)), data.Length - (offset + 4));

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
        // Every entry holds at least an identifier and a length, so a count of more entries than that fills the rest of
        // the data is malformed before any entry is read.
        Ensure(data, offset, 4);
        int entryCount = Bounded(BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset)), (data.Length - (offset + 4)) / 8);
        int cursor = offset + 4;

        bool unicode = ReferenceEquals(encoding, Encoding.Unicode);
        for (int i = 0; i < entryCount; i++)
        {
            Ensure(data, cursor, 8);
            int pid = unchecked((int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(cursor)));
            uint length = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(cursor + 4));
            cursor += 8;

            if (unicode)
            {
                int bytes = Bounded((ulong)length * 2, data.Length - cursor);
                names[pid] = Encoding.Unicode.GetString(data.Slice(cursor, bytes)).TrimEnd('\0');
                cursor += Align4(bytes);
            }
            else
            {
                int bytes = Bounded(length, data.Length - cursor);
                names[pid] = encoding.GetString(data.Slice(cursor, bytes)).TrimEnd('\0');
                cursor += bytes;
            }
        }
    }

    /// <summary>
    /// Resolves a section code page to the <see cref="Encoding" /> that decodes and encodes its ANSI strings.
    /// </summary>
    /// <param name="codePage">The code-page number.</param>
    /// <returns>
    /// The UTF-16 or UTF-8 encoding for code pages 1200, 1201 and 65001; otherwise the encoding
    /// <see cref="CodePagesEncodingProvider" /> serves for the code page, or Latin-1 for code page 0 and for a code
    /// page it does not serve.
    /// </returns>
    /// <remarks>
    /// The Windows and other legacy code pages come from the shared framework's
    /// <see cref="CodePagesEncodingProvider" />, queried directly rather than registered with
    /// <see cref="Encoding.RegisterProvider" />, so reading or writing a property set changes nothing process-wide.
    /// Code page 0 names the machine's ANSI code page rather than a code page of its own; it is read as Latin-1, so a
    /// property set decodes the same on every machine. The writer resolves its encoding through this method too, so the
    /// two always agree.
    /// </remarks>
    internal static Encoding ResolveEncoding(int codePage) =>
        codePage switch
        {
            1200 => Encoding.Unicode,
            1201 => Encoding.BigEndianUnicode,
            65001 => Encoding.UTF8,
            0 => Encoding.Latin1,
            _ => CodePagesEncodingProvider.Instance.GetEncoding(codePage) ?? Encoding.Latin1,
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
    /// Determines whether the values of a type vary in length, and so may be padded to a multiple of four bytes.
    /// </summary>
    /// <param name="type">The base type of a vector's elements.</param>
    /// <returns>
    /// <see langword="true" /> for strings, blobs, clipboard data and variants; otherwise <see langword="false" />.
    /// </returns>
    internal static bool HasVariableLength(OlePropertyType type) =>
        type is OlePropertyType.AnsiString
            or OlePropertyType.BinaryString
            or OlePropertyType.UnicodeString
            or OlePropertyType.Blob
            or OlePropertyType.ClipboardData
            or OlePropertyType.Variant;

    /// <summary>
    /// Returns the offset of the vector element that follows a variable-length element and the padding, if any, after
    /// it.
    /// </summary>
    /// <param name="data">The property-set bytes.</param>
    /// <param name="offset">The byte offset of the element.</param>
    /// <param name="consumed">The number of bytes the element's value occupies.</param>
    /// <returns>
    /// The offset past the element and as many of the bytes up to its next four-byte boundary as are zero.
    /// </returns>
    /// <remarks>
    /// [MS-OLEPS] pads every string to a multiple of four bytes, but [MS-OSHARED] stores the strings of a document
    /// summary's document parts (2.3.3.1.9) and heading pairs (2.3.3.1.12) with no padding, so the next element can
    /// begin at once. Padding is zero, and neither a string's length nor a variant's type begins with a zero byte in
    /// the vectors Office writes, so the padding is skipped only while its bytes are zero, as Apache POI does.
    /// </remarks>
    private static int SkipPadding(ReadOnlySpan<byte> data, int offset, int consumed)
    {
        int next = offset + consumed;
        int boundary = Math.Min(offset + Align4(consumed), data.Length);
        while (next < boundary && data[next] == 0)
            next++;

        return next;
    }

    /// <summary>
    /// Rounds a byte count up to the next four-byte boundary.
    /// </summary>
    /// <param name="value">The byte count.</param>
    /// <returns>The aligned byte count.</returns>
    private static int Align4(int value) =>
        (value + 3) & ~3;

    /// <summary>
    /// Throws when a type word carries a modifier the reader does not implement, so that a <c>VT_ARRAY</c>,
    /// <c>VT_BYREF</c> or <c>VT_RESERVED</c> value is dropped like any unsupported type rather than read as a value of
    /// its base type.
    /// </summary>
    /// <param name="rawType">The type word.</param>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when <paramref name="rawType" /> has a bit of <see cref="UnsupportedModifierMask" /> set.
    /// </exception>
    private static void ThrowIfUnsupportedModifier(ushort rawType) =>
        CompoundThrowHelper.ThrowFormatIf(
            (rawType & UnsupportedModifierMask) != 0,
            CompoundResourceStrings.Format_Invalid_CompoundPropertySet,
            CompoundFileError.InvalidPropertySet);

    /// <summary>
    /// Narrows a length, offset or count read from the data, throwing when it exceeds what is available.
    /// </summary>
    /// <param name="value">The value read.</param>
    /// <param name="available">The largest value the data can hold at that point.</param>
    /// <returns>The value as an <see cref="int" />.</returns>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when <paramref name="value" /> exceeds <paramref name="available" />.
    /// </exception>
    private static int Bounded(ulong value, int available)
    {
        CompoundThrowHelper.ThrowFormatIf(
            available < 0 || value > (ulong)available,
            CompoundResourceStrings.Format_Invalid_CompoundPropertySet,
            CompoundFileError.InvalidPropertySet);

        return (int)value;
    }

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

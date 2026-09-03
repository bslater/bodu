// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiValueDecoder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Text;

#if MSG
namespace Bodu.Formats.Outlook.Msg;
#elif OUTLOOK_PST
namespace Bodu.Formats.Outlook.Pst;
#endif

/// <summary>
/// Decodes raw MAPI wire values into the CLR shapes <see cref="MapiProperty" /> documents, independent of the
/// container the bytes came from.
/// </summary>
/// <remarks>
/// <para>
/// Every method is a pure <c>Try*</c>: bytes in, <see cref="object" /> out, <see langword="false" /> for anything the
/// decoder cannot represent — an unsupported type, a malformed payload, or an out-of-range FILETIME. The decoder never
/// throws; each consuming format decides at its own call site whether a <see langword="false" /> result skips the
/// property or raises that format's exception, so validation-level policy and resource strings stay format-local.
/// </para>
/// <para>
/// This file lives in <c>Bodu.Formats.Outlook/shared/</c> and is source-compiled into each Outlook format reader —
/// the fixed-scalar layouts, the packed fixed-width multi-value layout, and FILETIME conversion are identical in a
/// <c>.msg</c> property stream and a PST property/table context. Container-specific layouts (the <c>.msg</c>
/// per-element multi-value streams, the PST count-plus-offset-table multi-value form) stay in their format packages.
/// The consuming project selects the namespace via its <c>DefineConstants</c> (<c>MSG</c> or <c>OUTLOOK_PST</c>).
/// </para>
/// </remarks>
internal static class MapiValueDecoder
{
    /// <summary>The UTF-8 code page, whose payloads may carry a byte order mark.</summary>
    private const int Utf8CodePage = 65001;

    /// <summary>The UTF-8 byte order mark.</summary>
    private static ReadOnlySpan<byte> Utf8ByteOrderMark => [0xEF, 0xBB, 0xBF];

    /// <summary>
    /// Determines whether a base type stores its payload out of line (in a value stream or heap allocation) rather
    /// than inline in the fixed record.
    /// </summary>
    /// <param name="type">The base property type.</param>
    /// <returns><see langword="true" /> for the string, binary, and GUID types.</returns>
    internal static bool IsVariableLength(MapiPropertyType type) =>
        type is MapiPropertyType.Unicode or MapiPropertyType.String8 or MapiPropertyType.Binary or MapiPropertyType.Guid;

    /// <summary>
    /// Decodes a fixed-length value stored inline as an 8-byte little-endian slot.
    /// </summary>
    /// <param name="type">The base property type.</param>
    /// <param name="raw">The 8-byte inline value.</param>
    /// <param name="value">When this method returns <see langword="true" />, the decoded value.</param>
    /// <returns>
    /// <see langword="true" /> when the value decodes; <see langword="false" /> for an unsupported type or an
    /// unrepresentable FILETIME.
    /// </returns>
    internal static bool TryDecodeFixedValue(MapiPropertyType type, ulong raw, out object? value)
    {
        switch (type)
        {
            case MapiPropertyType.Unspecified:
            case MapiPropertyType.Null:
                // PT_UNSPECIFIED and PT_NULL are legal wire types with no payload: the property is present and its
                // value is null, which is not a decoding failure.
                value = null;
                return true;
            case MapiPropertyType.Int16:
                value = (short)(ushort)raw;
                return true;
            case MapiPropertyType.Int32:
            case MapiPropertyType.ErrorCode:
                value = (int)(uint)raw;
                return true;
            case MapiPropertyType.Float:
                value = BitConverter.Int32BitsToSingle((int)(uint)raw);
                return true;
            case MapiPropertyType.Double:
            case MapiPropertyType.AppTime:
                value = BitConverter.Int64BitsToDouble((long)raw);
                return true;
            case MapiPropertyType.Currency:
                value = (long)raw / 10000m;
                return true;
            case MapiPropertyType.Boolean:
                value = (raw & 0xFF) != 0;
                return true;
            case MapiPropertyType.Int64:
                value = (long)raw;
                return true;
            case MapiPropertyType.SystemTime:
                if (raw == 0)
                {
                    // Writers store a zero FILETIME for "no time"; the property is present with a null value.
                    value = null;
                    return true;
                }

                if (TryConvertFileTime(raw, out DateTimeOffset timestamp))
                {
                    value = timestamp;
                    return true;
                }

                value = null;
                return false;
            default:
                value = null;
                return false;
        }
    }

    /// <summary>
    /// Decodes a variable-length payload held in an owned array.
    /// </summary>
    /// <param name="type">The base property type.</param>
    /// <param name="bytes">The payload bytes. Binary payloads are surfaced as this array without copying.</param>
    /// <param name="encoding">The code-page encoding used for <see cref="MapiPropertyType.String8" /> payloads.</param>
    /// <param name="strict">
    /// Whether a structurally odd Unicode payload is rejected (<see langword="true" />) or decoded tolerantly with the
    /// trailing byte dropped (<see langword="false" />).
    /// </param>
    /// <param name="value">When this method returns <see langword="true" />, the decoded value.</param>
    /// <returns>
    /// <see langword="true" /> when the payload decodes; <see langword="false" /> for an unsupported type, a
    /// wrong-length GUID, a <see langword="null" /> payload or encoding, or — under <paramref name="strict" /> — an
    /// odd-length Unicode payload.
    /// </returns>
    internal static bool TryDecodeVariableValue(MapiPropertyType type, byte[] bytes, Encoding encoding, bool strict, out object? value)
    {
        if (bytes is null || encoding is null)
        {
            value = null;
            return false;
        }

        if (type == MapiPropertyType.Binary)
        {
            value = bytes;
            return true;
        }

        return TryDecodeVariableValue(type, bytes.AsSpan(), encoding, strict, out value);
    }

    /// <summary>
    /// Decodes a variable-length payload in place.
    /// </summary>
    /// <param name="type">The base property type.</param>
    /// <param name="bytes">The payload bytes. Binary payloads are copied into a new array.</param>
    /// <param name="encoding">The code-page encoding used for <see cref="MapiPropertyType.String8" /> payloads.</param>
    /// <param name="strict">
    /// Whether a structurally odd Unicode payload is rejected (<see langword="true" />) or decoded tolerantly with the
    /// trailing byte dropped (<see langword="false" />).
    /// </param>
    /// <param name="value">When this method returns <see langword="true" />, the decoded value.</param>
    /// <returns>
    /// <see langword="true" /> when the payload decodes; <see langword="false" /> for an unsupported type, a
    /// wrong-length GUID, a <see langword="null" /> encoding, or — under <paramref name="strict" /> — an odd-length
    /// Unicode payload.
    /// </returns>
    /// <remarks>
    /// Trailing NUL terminators are trimmed at the byte level before the string is materialized, and a UTF-8 code-page
    /// payload that begins with a byte order mark has the mark removed; both are encoding artifacts, not property text.
    /// </remarks>
    internal static bool TryDecodeVariableValue(MapiPropertyType type, ReadOnlySpan<byte> bytes, Encoding encoding, bool strict, out object? value)
    {
        if (encoding is null)
        {
            value = null;
            return false;
        }

        switch (type)
        {
            case MapiPropertyType.Unicode:
                if ((bytes.Length & 1) != 0)
                {
                    if (strict)
                    {
                        value = null;
                        return false;
                    }

                    bytes = bytes.Slice(0, bytes.Length - 1);
                }

                while (bytes.Length >= 2 && bytes[^1] == 0 && bytes[^2] == 0)
                    bytes = bytes.Slice(0, bytes.Length - 2);

                value = Encoding.Unicode.GetString(bytes);
                return true;
            case MapiPropertyType.String8:
                if (encoding.CodePage == Utf8CodePage && bytes.StartsWith(Utf8ByteOrderMark))
                    bytes = bytes.Slice(Utf8ByteOrderMark.Length);

                while (bytes.Length >= 1 && bytes[^1] == 0)
                    bytes = bytes.Slice(0, bytes.Length - 1);

                value = encoding.GetString(bytes);
                return true;
            case MapiPropertyType.Binary:
                value = bytes.ToArray();
                return true;
            case MapiPropertyType.Guid:
                if (bytes.Length != 16)
                {
                    value = null;
                    return false;
                }

                value = new Guid(bytes);
                return true;
            default:
                value = null;
                return false;
        }
    }

    /// <summary>
    /// Decodes a multi-valued fixed-length property whose elements are packed contiguously.
    /// </summary>
    /// <param name="type">The base element type.</param>
    /// <param name="bytes">The packed payload.</param>
    /// <param name="value">When this method returns <see langword="true" />, the decoded array value.</param>
    /// <returns>
    /// <see langword="true" /> when the payload decodes; <see langword="false" /> for an unsupported element type, a
    /// payload that is not a whole number of elements, or an unrepresentable FILETIME element.
    /// </returns>
    internal static bool TryDecodePackedMultiValue(MapiPropertyType type, ReadOnlySpan<byte> bytes, out object? value)
    {
        value = null;
        int elementSize = type switch
        {
            MapiPropertyType.Int16 => 2,
            MapiPropertyType.Int32 => 4,
            MapiPropertyType.Float => 4,
            MapiPropertyType.Double => 8,
            MapiPropertyType.Currency => 8,
            MapiPropertyType.Int64 => 8,
            MapiPropertyType.SystemTime => 8,
            MapiPropertyType.Guid => 16,
            _ => 0,
        };

        if (elementSize == 0 || bytes.Length % elementSize != 0)
            return false;

        int count = bytes.Length / elementSize;
        switch (type)
        {
            case MapiPropertyType.Int16:
                var shorts = new short[count];
                for (int i = 0; i < count; i++)
                    shorts[i] = BinaryPrimitives.ReadInt16LittleEndian(bytes.Slice(i * 2));
                value = shorts;
                return true;
            case MapiPropertyType.Int32:
                var ints = new int[count];
                for (int i = 0; i < count; i++)
                    ints[i] = BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(i * 4));
                value = ints;
                return true;
            case MapiPropertyType.Float:
                var floats = new float[count];
                for (int i = 0; i < count; i++)
                    floats[i] = BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(i * 4)));
                value = floats;
                return true;
            case MapiPropertyType.Double:
                var doubles = new double[count];
                for (int i = 0; i < count; i++)
                    doubles[i] = BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(bytes.Slice(i * 8)));
                value = doubles;
                return true;
            case MapiPropertyType.Currency:
                var decimals = new decimal[count];
                for (int i = 0; i < count; i++)
                    decimals[i] = BinaryPrimitives.ReadInt64LittleEndian(bytes.Slice(i * 8)) / 10000m;
                value = decimals;
                return true;
            case MapiPropertyType.Int64:
                var longs = new long[count];
                for (int i = 0; i < count; i++)
                    longs[i] = BinaryPrimitives.ReadInt64LittleEndian(bytes.Slice(i * 8));
                value = longs;
                return true;
            case MapiPropertyType.SystemTime:
                var timestamps = new DateTimeOffset[count];
                for (int i = 0; i < count; i++)
                {
                    if (!TryConvertFileTime(BinaryPrimitives.ReadUInt64LittleEndian(bytes.Slice(i * 8)), out timestamps[i]))
                        return false;
                }

                value = timestamps;
                return true;
            case MapiPropertyType.Guid:
                var guids = new Guid[count];
                for (int i = 0; i < count; i++)
                    guids[i] = new Guid(bytes.Slice(i * 16, 16));
                value = guids;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Converts a FILETIME value to a UTC time stamp, rejecting zero and out-of-range values.
    /// </summary>
    /// <param name="raw">The raw FILETIME.</param>
    /// <param name="value">When this method returns <see langword="true" />, the converted time stamp with a zero offset.</param>
    /// <returns><see langword="true" /> when the value is representable.</returns>
    /// <remarks>
    /// A FILETIME is defined in UTC, so the result carries <see cref="TimeSpan.Zero" /> regardless of the machine's
    /// time zone; converting through local time would both shift the offset and reject values near the range limits
    /// in zones ahead of UTC.
    /// </remarks>
    internal static bool TryConvertFileTime(ulong raw, out DateTimeOffset value)
    {
        value = default;
        if (raw == 0 || raw > long.MaxValue)
            return false;

        try
        {
            value = new DateTimeOffset(DateTime.FromFileTimeUtc((long)raw).Ticks, TimeSpan.Zero);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }
}

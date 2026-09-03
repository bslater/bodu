// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiNamedPropertyRecords.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

#if MSG
namespace Bodu.Formats.Outlook.Msg;
#elif OUTLOOK_PST
namespace Bodu.Formats.Outlook.Pst;
#endif

/// <summary>
/// Parses the named-property mapping records the Outlook formats share: the <c>NAMEID</c> entry stream, the GUID
/// stream it indexes, and the string stream that holds string-named properties.
/// </summary>
/// <remarks>
/// <para>
/// Entry <c>i</c> of the entry stream defines the property identifier <c>0x8000 + wPropIdx</c>. Each 8-byte record
/// carries a numeric name identifier or a string-stream offset, a kind bit, and a GUID index — <c>1</c> for
/// <c>PS_MAPI</c>, <c>2</c> for <c>PS_PUBLIC_STRINGS</c>, and <c>3 + n</c> for the <c>n</c>-th GUID of the GUID stream.
/// A string name is a 4-byte byte length followed by UTF-16LE text.
/// </para>
/// <para>
/// A record is malformed when its GUID index does not resolve, its string offset or length leaves the string stream,
/// its string name is empty or whitespace, or its property index would place the identifier outside the named range.
/// Under strict parsing a malformed record throws the consuming format's exception; otherwise it is skipped. This file
/// lives in <c>Bodu.Formats.Outlook/shared/</c> and is source-compiled into each Outlook format reader — the record
/// layout is identical in a <c>.msg</c> <c>__nameid_version1.0</c> storage and a PST name-to-id map node — and the
/// consuming project selects the namespace and exception type via its <c>DefineConstants</c>.
/// </para>
/// </remarks>
internal static class MapiNamedPropertyRecords
{
    /// <summary>The lowest property identifier the mapping defines.</summary>
    private const ushort NamedPropertyIdFloor = 0x8000;

    /// <summary>The largest property index that keeps the identifier within the named range.</summary>
    private const ushort MaxPropertyIndex = 0x7FFF;

    /// <summary>The size of one <c>NAMEID</c> record.</summary>
    private const int EntrySize = 8;

    /// <summary>The <c>PS_MAPI</c> property-set GUID (index 1).</summary>
    private static readonly Guid s_psMapi = new("00020328-0000-0000-C000-000000000046");

    /// <summary>The <c>PS_PUBLIC_STRINGS</c> property-set GUID (index 2).</summary>
    private static readonly Guid s_psPublicStrings = new("00020329-0000-0000-C000-000000000046");

    /// <summary>
    /// Parses the mapping records into the two lookup directions.
    /// </summary>
    /// <param name="guids">The GUID-stream payload.</param>
    /// <param name="entries">The entry-stream payload.</param>
    /// <param name="strings">The string-stream payload.</param>
    /// <param name="strict">Whether a malformed record throws instead of being skipped.</param>
    /// <param name="byId">Receives the identifier-to-identity mapping.</param>
    /// <param name="byName">Receives the identity-to-identifier mapping.</param>
    /// <exception cref="OutlookFormatException">
    /// The entry stream is not a whole number of records, or a record is malformed, and <paramref name="strict" /> is
    /// set. The concrete type is the consuming format's exception.
    /// </exception>
    /// <remarks>
    /// When the entry stream is not a whole number of records and <paramref name="strict" /> is not set, nothing is
    /// mapped. A later record that repeats an identifier or an identity replaces the earlier one.
    /// </remarks>
    internal static void Parse(
        ReadOnlySpan<byte> guids,
        ReadOnlySpan<byte> entries,
        ReadOnlySpan<byte> strings,
        bool strict,
        Dictionary<ushort, MapiNamedProperty> byId,
        Dictionary<MapiNamedProperty, ushort> byName)
    {
        if (entries.Length % EntrySize != 0)
        {
            if (strict)
                throw Malformed();

            return;
        }

        int count = entries.Length / EntrySize;
        for (int i = 0; i < count; i++)
        {
            ReadOnlySpan<byte> entry = entries.Slice(i * EntrySize, EntrySize);
            uint idOrOffset = BinaryPrimitives.ReadUInt32LittleEndian(entry);
            ushort indexAndKind = BinaryPrimitives.ReadUInt16LittleEndian(entry.Slice(4));
            ushort propertyIndex = BinaryPrimitives.ReadUInt16LittleEndian(entry.Slice(6));

            bool isString = (indexAndKind & 0x1) != 0;
            int guidIndex = indexAndKind >> 1;

            // An index past 0x7FFF would wrap the identifier into the well-known range and shadow a real property.
            if (propertyIndex > MaxPropertyIndex || !TryResolveGuid(guids, guidIndex, out Guid propertySetId))
            {
                if (strict)
                    throw Malformed();

                continue;
            }

            MapiNamedProperty name;
            if (isString)
            {
                if (!TryReadName(strings, idOrOffset, out string? text))
                {
                    if (strict)
                        throw Malformed();

                    continue;
                }

                name = new MapiNamedProperty(propertySetId, text);
            }
            else
            {
                name = new MapiNamedProperty(propertySetId, idOrOffset);
            }

            var id = (ushort)(NamedPropertyIdFloor + propertyIndex);
            byId[id] = name;
            byName[name] = id;
        }
    }

    /// <summary>
    /// Resolves a GUID index to its property-set GUID.
    /// </summary>
    /// <param name="guids">The GUID-stream payload.</param>
    /// <param name="guidIndex">The index: 1 and 2 are the well-known sets; 3 and above index the stream.</param>
    /// <param name="propertySetId">When this method returns <see langword="true" />, the resolved GUID.</param>
    /// <returns><see langword="true" /> when the index resolves.</returns>
    private static bool TryResolveGuid(ReadOnlySpan<byte> guids, int guidIndex, out Guid propertySetId)
    {
        switch (guidIndex)
        {
            case 1:
                propertySetId = s_psMapi;
                return true;
            case 2:
                propertySetId = s_psPublicStrings;
                return true;
            default:
                long offset = (long)(guidIndex - 3) * 16;
                if (guidIndex < 3 || offset + 16 > guids.Length)
                {
                    propertySetId = default;
                    return false;
                }

                propertySetId = new Guid(guids.Slice((int)offset, 16));
                return true;
        }
    }

    /// <summary>
    /// Reads a string name from the string stream at an offset.
    /// </summary>
    /// <param name="strings">The string-stream payload.</param>
    /// <param name="offset">The byte offset of the length-prefixed UTF-16LE name.</param>
    /// <param name="name">When this method returns <see langword="true" />, the name text.</param>
    /// <returns>
    /// <see langword="true" /> when the offset and length are within the stream and the text can name a property.
    /// </returns>
    private static bool TryReadName(ReadOnlySpan<byte> strings, uint offset, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out string name)
    {
        name = null;

        // The offset and length are 32-bit unsigned fields; the arithmetic is done in 64 bits so a value near the top
        // of the range cannot wrap back inside the stream.
        long start = offset;
        if (start + 4 > strings.Length)
            return false;

        long length = BinaryPrimitives.ReadUInt32LittleEndian(strings.Slice((int)start));
        if (length == 0 || (length & 1) != 0 || start + 4 + length > strings.Length)
            return false;

        string text = System.Text.Encoding.Unicode.GetString(strings.Slice((int)start + 4, (int)length));

        // A whitespace-only name cannot identify a property; treat it as malformed content rather than letting the
        // identity constructor's argument validation escape the reader's exception discipline.
        if (string.IsNullOrWhiteSpace(text))
            return false;

        name = text;
        return true;
    }

    /// <summary>
    /// Creates the malformed-mapping exception for the consuming format.
    /// </summary>
    /// <returns>The exception to throw.</returns>
    private static Exception Malformed() =>
#if MSG
        new OutlookMsgFormatException(OutlookMsgResourceStrings.Format_Invalid_MsgNameId);
#elif OUTLOOK_PST
        new OutlookPstFormatException(OutlookPstResourceStrings.Format_Invalid_PstNamedPropertyMap);
#endif
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstNamedPropertyMap.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.IO.Pst;

namespace Bodu.Formats.Outlook.Pst;

/// <summary>
/// Provides the bidirectional named-property mapping parsed from the store's name-to-id map node (MS-PST §2.4.7).
/// </summary>
/// <remarks>
/// The mapping is file-specific: entry <c>i</c> of the entry stream (property <c>0x0003</c>) defines the property
/// identifier <c>0x8000 + wPropIdx</c>. Each 8-byte <c>NAMEID</c> record carries a numeric name identifier or
/// string-stream offset, a kind bit, and a GUID index — <c>1</c> for <c>PS_MAPI</c>, <c>2</c> for
/// <c>PS_PUBLIC_STRINGS</c>, and <c>3 + n</c> for the <c>n</c>-th GUID of the GUID stream (property <c>0x0002</c>).
/// The hash buckets the node also stores (properties from <c>0x1000</c>) are write-time acceleration and are ignored
/// on read.
/// </remarks>
internal sealed class PstNamedPropertyMap
{
    /// <summary>The lowest property identifier the mapping defines.</summary>
    private const ushort NamedPropertyIdFloor = 0x8000;

    /// <summary>The property identifier of the GUID stream within the name-to-id map node.</summary>
    private const ushort GuidStreamPropertyId = 0x0002;

    /// <summary>The property identifier of the entry stream within the name-to-id map node.</summary>
    private const ushort EntryStreamPropertyId = 0x0003;

    /// <summary>The property identifier of the string stream within the name-to-id map node.</summary>
    private const ushort StringStreamPropertyId = 0x0004;

    /// <summary>The <c>PS_MAPI</c> property-set GUID (index 1).</summary>
    private static readonly Guid s_psMapi = new("00020328-0000-0000-C000-000000000046");

    /// <summary>The <c>PS_PUBLIC_STRINGS</c> property-set GUID (index 2).</summary>
    private static readonly Guid s_psPublicStrings = new("00020329-0000-0000-C000-000000000046");

    /// <summary>Maps a property identifier to its named-property identity.</summary>
    private readonly Dictionary<ushort, MapiNamedProperty> _byId;

    /// <summary>Maps a named-property identity to its property identifier.</summary>
    private readonly Dictionary<MapiNamedProperty, ushort> _byName;

    /// <summary>
    /// Initializes a new instance of the <see cref="PstNamedPropertyMap" /> class.
    /// </summary>
    /// <param name="byId">The identifier-to-identity mapping.</param>
    /// <param name="byName">The identity-to-identifier mapping.</param>
    private PstNamedPropertyMap(Dictionary<ushort, MapiNamedProperty> byId, Dictionary<MapiNamedProperty, ushort> byName)
    {
        _byId = byId;
        _byName = byName;
    }

    /// <summary>
    /// Gets the shared empty mapping, used when the store carries no name-to-id map node.
    /// </summary>
    /// <value>A mapping that resolves nothing.</value>
    internal static PstNamedPropertyMap Empty { get; } = new([], []);

    /// <summary>
    /// Parses the mapping from the name-to-id map node's property context.
    /// </summary>
    /// <param name="context">The name-to-id map node's property context.</param>
    /// <param name="strict">Whether malformed mapping content throws instead of being skipped.</param>
    /// <returns>The parsed mapping.</returns>
    /// <exception cref="OutlookPstFormatException">
    /// The mapping streams are malformed and <paramref name="strict" /> is set.
    /// </exception>
    internal static PstNamedPropertyMap Load(PstPropertyContext context, bool strict)
    {
        byte[] guids = ReadStreamOrEmpty(context, GuidStreamPropertyId);
        byte[] entries = ReadStreamOrEmpty(context, EntryStreamPropertyId);
        byte[] strings = ReadStreamOrEmpty(context, StringStreamPropertyId);

        if (entries.Length % 8 != 0)
            return strict ? throw Malformed() : Empty;

        var byId = new Dictionary<ushort, MapiNamedProperty>();
        var byName = new Dictionary<MapiNamedProperty, ushort>();
        int count = entries.Length / 8;
        for (int i = 0; i < count; i++)
        {
            ReadOnlySpan<byte> entry = entries.AsSpan(i * 8, 8);
            uint idOrOffset = BinaryPrimitives.ReadUInt32LittleEndian(entry);
            ushort indexAndKind = BinaryPrimitives.ReadUInt16LittleEndian(entry.Slice(4));
            ushort propertyIndex = BinaryPrimitives.ReadUInt16LittleEndian(entry.Slice(6));

            bool isString = (indexAndKind & 0x1) != 0;
            int guidIndex = indexAndKind >> 1;
            if (!TryResolveGuid(guids, guidIndex, out Guid propertySetId))
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

        return new PstNamedPropertyMap(byId, byName);
    }

    /// <summary>
    /// Attempts to resolve the property identifier a named-property identity maps to in this store.
    /// </summary>
    /// <param name="name">The named-property identity.</param>
    /// <param name="id">When this method returns <see langword="true" />, the mapped identifier.</param>
    /// <returns><see langword="true" /> when the identity is mapped.</returns>
    internal bool TryGetId(MapiNamedProperty name, out ushort id) =>
        _byName.TryGetValue(name, out id);

    /// <summary>
    /// Attempts to resolve the named-property identity behind a property identifier.
    /// </summary>
    /// <param name="id">The property identifier (at or above <c>0x8000</c>).</param>
    /// <param name="name">When this method returns <see langword="true" />, the identity.</param>
    /// <returns><see langword="true" /> when the identifier is mapped.</returns>
    internal bool TryGetName(ushort id, out MapiNamedProperty name) =>
        _byId.TryGetValue(id, out name);

    /// <summary>
    /// Reads one of the map node's binary streams, or returns an empty payload when the property is absent or not
    /// binary-typed.
    /// </summary>
    /// <param name="context">The name-to-id map node's property context.</param>
    /// <param name="propertyId">The stream's property identifier.</param>
    /// <returns>The stream payload, or an empty array.</returns>
    private static byte[] ReadStreamOrEmpty(PstPropertyContext context, ushort propertyId) =>
        context.TryGetValue(propertyId, out PstPropertyValue value)
            && value.WireType == (ushort)MapiPropertyType.Binary
            ? value.RawData.ToArray()
            : [];

    /// <summary>
    /// Resolves a GUID index to its property-set GUID.
    /// </summary>
    /// <param name="guids">The GUID-stream payload.</param>
    /// <param name="guidIndex">The index: 1 and 2 are the well-known sets; 3 and above index the stream.</param>
    /// <param name="propertySetId">When this method returns <see langword="true" />, the resolved GUID.</param>
    /// <returns><see langword="true" /> when the index resolves.</returns>
    private static bool TryResolveGuid(byte[] guids, int guidIndex, out Guid propertySetId)
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
                int offset = (guidIndex - 3) * 16;
                if (guidIndex < 3 || offset + 16 > guids.Length)
                {
                    propertySetId = default;
                    return false;
                }

                propertySetId = new Guid(guids.AsSpan(offset, 16));
                return true;
        }
    }

    /// <summary>
    /// Reads a string name from the string stream at an offset.
    /// </summary>
    /// <param name="strings">The string-stream payload.</param>
    /// <param name="offset">The byte offset of the length-prefixed UTF-16LE name.</param>
    /// <param name="name">When this method returns <see langword="true" />, the name text.</param>
    /// <returns><see langword="true" /> when the offset and length are within the stream and the text names a property.</returns>
    private static bool TryReadName(byte[] strings, uint offset, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out string name)
    {
        name = null;
        if (offset + 4 > (uint)strings.Length)
            return false;

        uint length = BinaryPrimitives.ReadUInt32LittleEndian(strings.AsSpan((int)offset));
        if (length == 0 || (length & 1) != 0 || offset + 4 + length > (uint)strings.Length)
            return false;

        string text = System.Text.Encoding.Unicode.GetString(strings, (int)offset + 4, (int)length);

        // A whitespace-only name cannot identify a property; treat it as malformed content rather than letting the
        // identity constructor's argument validation escape the reader's exception discipline.
        if (string.IsNullOrWhiteSpace(text))
            return false;

        name = text;
        return true;
    }

    /// <summary>
    /// Creates the malformed-mapping exception.
    /// </summary>
    /// <returns>The exception to throw.</returns>
    private static OutlookPstFormatException Malformed() =>
        new(OutlookPstResourceStrings.Format_Invalid_PstNamedPropertyMap);
}

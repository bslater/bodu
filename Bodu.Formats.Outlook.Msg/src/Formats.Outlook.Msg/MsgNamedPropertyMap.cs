// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgNamedPropertyMap.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.IO.Compound;

namespace Bodu.Formats.Outlook.Msg;

/// <summary>
/// Provides the bidirectional named-property mapping parsed from a message's <c>__nameid_version1.0</c> storage.
/// </summary>
/// <remarks>
/// The mapping is file-specific: entry <c>i</c> of the entry stream defines the property identifier <c>0x8000 + i</c>.
/// Each 8-byte entry carries a name identifier or string-stream offset, a kind bit, and a GUID index — <c>1</c> for
/// <c>PS_MAPI</c>, <c>2</c> for <c>PS_PUBLIC_STRINGS</c>, and <c>3 + n</c> for the <c>n</c>-th GUID of the GUID stream.
/// The per-bucket hash streams the format also stores are write-time acceleration and are ignored on read.
/// </remarks>
internal sealed class MsgNamedPropertyMap
{
    /// <summary>The lowest property identifier the mapping defines.</summary>
    private const ushort NamedPropertyIdFloor = 0x8000;

    /// <summary>The <c>PS_MAPI</c> property-set GUID (index 1).</summary>
    private static readonly Guid s_psMapi = new("00020328-0000-0000-C000-000000000046");

    /// <summary>The <c>PS_PUBLIC_STRINGS</c> property-set GUID (index 2).</summary>
    private static readonly Guid s_psPublicStrings = new("00020329-0000-0000-C000-000000000046");

    /// <summary>Maps a property identifier to its named-property identity.</summary>
    private readonly Dictionary<ushort, MapiNamedProperty> _byId;

    /// <summary>Maps a named-property identity to its property identifier.</summary>
    private readonly Dictionary<MapiNamedProperty, ushort> _byName;

    /// <summary>
    /// Initializes a new instance of the <see cref="MsgNamedPropertyMap" /> class.
    /// </summary>
    /// <param name="byId">The identifier-to-identity mapping.</param>
    /// <param name="byName">The identity-to-identifier mapping.</param>
    private MsgNamedPropertyMap(Dictionary<ushort, MapiNamedProperty> byId, Dictionary<MapiNamedProperty, ushort> byName)
    {
        _byId = byId;
        _byName = byName;
    }

    /// <summary>
    /// Gets the shared empty mapping, used when a message carries no named-property storage.
    /// </summary>
    /// <value>A mapping that resolves nothing.</value>
    internal static MsgNamedPropertyMap Empty { get; } = new(new Dictionary<ushort, MapiNamedProperty>(), new Dictionary<MapiNamedProperty, ushort>());

    /// <summary>
    /// Loads the mapping from a message's root storage.
    /// </summary>
    /// <param name="root">The root storage of the message.</param>
    /// <param name="validationLevel">The validation level governing malformed-content handling.</param>
    /// <returns>The parsed mapping, or <see cref="Empty" /> when the storage is absent.</returns>
    /// <exception cref="OutlookMsgFormatException">
    /// Thrown under <see cref="CompoundValidationLevel.Strict" /> when the mapping streams are malformed.
    /// </exception>
    internal static MsgNamedPropertyMap Load(CompoundStorage root, CompoundValidationLevel validationLevel)
    {
        if (!root.TryOpenStorage(MsgStreamNames.NameIdStorageName, out CompoundStorage? storage))
            return Empty;

        bool strict = validationLevel == CompoundValidationLevel.Strict;
        byte[] guids = ReadStreamOrEmpty(storage, MsgStreamNames.GetSubstgStreamName(0x00020102));
        byte[] entries = ReadStreamOrEmpty(storage, MsgStreamNames.GetSubstgStreamName(0x00030102));
        byte[] strings = ReadStreamOrEmpty(storage, MsgStreamNames.GetSubstgStreamName(0x00040102));

        if (entries.Length % 8 != 0)
            return strict ? throw MalformedNameId() : Empty;

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
                    throw MalformedNameId();

                continue;
            }

            MapiNamedProperty name;
            if (isString)
            {
                if (!TryReadName(strings, idOrOffset, out string? text))
                {
                    if (strict)
                        throw MalformedNameId();

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

        return new MsgNamedPropertyMap(byId, byName);
    }

    /// <summary>
    /// Attempts to resolve the property identifier a named-property identity maps to in this message.
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
    /// Reads a mapping stream, or returns an empty payload when it is absent.
    /// </summary>
    /// <param name="storage">The named-property storage.</param>
    /// <param name="name">The stream name.</param>
    /// <returns>The stream payload, or an empty array.</returns>
    private static byte[] ReadStreamOrEmpty(CompoundStorage storage, string name)
    {
        if (!storage.TryOpenStream(name, out CompoundStream? stream))
            return Array.Empty<byte>();

        using (stream)
            return stream.ReadAllBytes();
    }

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
    /// <returns><see langword="true" /> when the offset and length are within the stream.</returns>
    private static bool TryReadName(byte[] strings, uint offset, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out string name)
    {
        name = null;
        if (offset + 4 > (uint)strings.Length)
            return false;

        uint length = BinaryPrimitives.ReadUInt32LittleEndian(strings.AsSpan((int)offset));
        if (length == 0 || (length & 1) != 0 || offset + 4 + length > (uint)strings.Length)
            return false;

        name = System.Text.Encoding.Unicode.GetString(strings, (int)offset + 4, (int)length);
        return true;
    }

    /// <summary>
    /// Creates the malformed-mapping exception.
    /// </summary>
    /// <returns>The exception to throw.</returns>
    private static OutlookMsgFormatException MalformedNameId() =>
        new(OutlookMsgResourceStrings.Format_Invalid_MsgNameId);
}

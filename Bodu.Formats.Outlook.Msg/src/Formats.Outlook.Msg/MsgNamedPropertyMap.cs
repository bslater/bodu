// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgNamedPropertyMap.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
    /// The container is malformed, or the mapping streams are malformed under
    /// <see cref="CompoundValidationLevel.Strict" />.
    /// </exception>
    internal static MsgNamedPropertyMap Load(CompoundStorage root, CompoundValidationLevel validationLevel)
    {
        if (!MsgContainer.TryOpenStorage(root, MsgStreamNames.NameIdStorageName, out CompoundStorage? storage))
            return Empty;

        bool strict = validationLevel == CompoundValidationLevel.Strict;
        byte[] guids = ReadStreamOrEmpty(storage, MsgStreamNames.GetSubstgStreamName(0x00020102));
        byte[] entries = ReadStreamOrEmpty(storage, MsgStreamNames.GetSubstgStreamName(0x00030102));
        byte[] strings = ReadStreamOrEmpty(storage, MsgStreamNames.GetSubstgStreamName(0x00040102));

        var byId = new Dictionary<ushort, MapiNamedProperty>();
        var byName = new Dictionary<MapiNamedProperty, ushort>();
        MapiNamedPropertyRecords.Parse(guids, entries, strings, strict, byId, byName);

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
    private static byte[] ReadStreamOrEmpty(CompoundStorage storage, string name) =>
        MsgContainer.TryReadStream(storage, name, out byte[]? bytes) ? bytes : [];
}

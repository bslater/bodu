// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstNamedPropertyMap.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
    /// <summary>The property identifier of the GUID stream within the name-to-id map node.</summary>
    private const ushort GuidStreamPropertyId = 0x0002;

    /// <summary>The property identifier of the entry stream within the name-to-id map node.</summary>
    private const ushort EntryStreamPropertyId = 0x0003;

    /// <summary>The property identifier of the string stream within the name-to-id map node.</summary>
    private const ushort StringStreamPropertyId = 0x0004;

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

        var byId = new Dictionary<ushort, MapiNamedProperty>();
        var byName = new Dictionary<MapiNamedProperty, ushort>();
        MapiNamedPropertyRecords.Parse(guids, entries, strings, strict, byId, byName);

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
}

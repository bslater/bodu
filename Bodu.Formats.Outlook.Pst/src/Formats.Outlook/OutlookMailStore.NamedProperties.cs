// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailStore.NamedProperties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Pst;
using Bodu.IO.Pst;

namespace Bodu.Formats.Outlook;

public sealed partial class OutlookMailStore
{
    /// <summary>The lazily loaded named-property mapping.</summary>
    private PstNamedPropertyMap? _namedProperties;

    /// <summary>
    /// Gets the named-property mapping, parsing the store's name-to-id map node on first use.
    /// </summary>
    /// <value>The mapping; empty when the store carries no name-to-id map node.</value>
    /// <exception cref="PstFileException">The container is malformed.</exception>
    /// <exception cref="OutlookPstFormatException">
    /// Under strict validation, the mapping streams are malformed.
    /// </exception>
    private PstNamedPropertyMap NamedPropertyMap =>
        _namedProperties ??= TryGetNode(PstNodeId.NameToIdMap, out PstNode? node)
            ? PstNamedPropertyMap.Load(node.ReadPropertyContext(), Strict)
            : PstNamedPropertyMap.Empty;

    /// <summary>
    /// Attempts to resolve the property identifier a named property maps to in this store.
    /// </summary>
    /// <param name="name">The named-property identity.</param>
    /// <param name="id">
    /// When this method returns <see langword="true" />, the file-specific identifier (at or above <c>0x8000</c>) the
    /// name maps to; combine it with the expected <see cref="MapiPropertyType" /> to address an object's properties.
    /// </param>
    /// <returns><see langword="true" /> when the store maps the name.</returns>
    /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
    /// <exception cref="PstFileException">The container is malformed.</exception>
    /// <exception cref="OutlookPstFormatException">
    /// Under strict validation, the mapping streams are malformed.
    /// </exception>
    /// <remarks>
    /// The mapping is store-wide (MS-PST keeps one name-to-id map per file), so a single resolution applies to every
    /// message and attachment of the session.
    /// </remarks>
    public bool TryGetNamedPropertyId(MapiNamedProperty name, out ushort id)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return NamedPropertyMap.TryGetId(name, out id);
    }

    /// <summary>
    /// Attempts to resolve the named-property identity behind a property tag.
    /// </summary>
    /// <param name="tag">A tag whose identifier is in the named range (at or above <c>0x8000</c>).</param>
    /// <param name="name">When this method returns <see langword="true" />, the identity.</param>
    /// <returns><see langword="true" /> when the store maps the tag's identifier.</returns>
    /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
    /// <exception cref="PstFileException">The container is malformed.</exception>
    /// <exception cref="OutlookPstFormatException">
    /// Under strict validation, the mapping streams are malformed.
    /// </exception>
    public bool TryGetPropertyName(MapiPropertyTag tag, out MapiNamedProperty name)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return NamedPropertyMap.TryGetName(tag.Id, out name);
    }
}

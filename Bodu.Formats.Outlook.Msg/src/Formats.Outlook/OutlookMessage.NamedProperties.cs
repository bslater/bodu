// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMessage.NamedProperties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Msg;

namespace Bodu.Formats.Outlook;

public sealed partial class OutlookMessage
{
    /// <summary>The lazily loaded named-property mapping (root session only; nested sessions delegate).</summary>
    private MsgNamedPropertyMap? _namedProperties;

    /// <summary>
    /// Gets the named-property mapping, loading it from the root storage on first use. Nested messages share the root
    /// session's mapping per MS-OXMSG.
    /// </summary>
    /// <value>The mapping; empty when the message carries no named-property storage.</value>
    private MsgNamedPropertyMap NamedPropertyMap =>
        _root is not null
            ? _root.NamedPropertyMap
            : _namedProperties ??= MsgNamedPropertyMap.Load(_storage, _options.ValidationLevel);

    /// <summary>
    /// Attempts to resolve the property identifier a named property maps to in this message.
    /// </summary>
    /// <param name="name">The named-property identity.</param>
    /// <param name="id">
    /// When this method returns <see langword="true" />, the file-specific identifier (at or above <c>0x8000</c>) the
    /// name maps to; combine it with the expected <see cref="MapiPropertyType" /> to address <see cref="Properties" />.
    /// </param>
    /// <returns><see langword="true" /> when the message maps the name.</returns>
    /// <exception cref="ObjectDisposedException">The message has been disposed.</exception>
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
    /// <returns><see langword="true" /> when the message maps the tag's identifier.</returns>
    /// <exception cref="ObjectDisposedException">The message has been disposed.</exception>
    public bool TryGetPropertyName(MapiPropertyTag tag, out MapiNamedProperty name)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return NamedPropertyMap.TryGetName(tag.Id, out name);
    }
}

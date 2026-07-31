// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiProperty.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

/// <summary>
/// Represents a single decoded MAPI property: a <see cref="MapiPropertyTag" /> paired with its CLR value.
/// </summary>
/// <remarks>
/// <para>
/// The value's CLR type follows the tag's <see cref="MapiPropertyType" />: <see cref="string" /> for
/// <see cref="MapiPropertyType.Unicode" /> and <see cref="MapiPropertyType.String8" />, <see cref="short" /> /
/// <see cref="int" /> / <see cref="long" /> for the integer codes, <see cref="float" /> / <see cref="double" /> for the
/// floating-point codes, <see cref="decimal" /> for <see cref="MapiPropertyType.Currency" />, <see cref="bool" /> for
/// <see cref="MapiPropertyType.Boolean" />, <see cref="DateTimeOffset" /> for
/// <see cref="MapiPropertyType.SystemTime" />, <see cref="Guid" /> for <see cref="MapiPropertyType.Guid" />, and
/// <c>byte[]</c> for <see cref="MapiPropertyType.Binary" />. A multi-valued property carries the corresponding array
/// type.
/// </para>
/// <para>
/// A <see langword="null" /> value is permitted — it represents a property whose payload is absent or, as for
/// <see cref="MapiPropertyType.Object" />, not representable inline.
/// </para>
/// </remarks>
public sealed class MapiProperty
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MapiProperty" /> class.
    /// </summary>
    /// <param name="tag">The property tag.</param>
    /// <param name="value">The decoded value, or <see langword="null" /> when the payload is absent.</param>
    public MapiProperty(MapiPropertyTag tag, object? value)
    {
        Tag = tag;
        Value = value;
    }

    /// <summary>
    /// Gets the property tag.
    /// </summary>
    /// <value>The 32-bit tag identifying the property and its wire type.</value>
    public MapiPropertyTag Tag { get; }

    /// <summary>
    /// Gets the decoded value.
    /// </summary>
    /// <value>
    /// The CLR value described in the type remarks, or <see langword="null" /> when the payload is absent.
    /// </value>
    public object? Value { get; }

    /// <summary>
    /// Returns a textual form of the property for diagnostics.
    /// </summary>
    /// <returns>The tag in its canonical hexadecimal form followed by the value, or <c>null</c> when absent.</returns>
    public override string ToString() =>
        $"{Tag} = {Value ?? "null"}";
}

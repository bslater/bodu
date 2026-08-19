// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NodeConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if BENCODE
namespace Bodu.Text.Bencode.Serialization.Converters;
#elif TOML
namespace Bodu.Text.Toml.Serialization.Converters;
#elif YAML
namespace Bodu.Text.Yaml.Serialization.Converters;
#endif

/// <summary>
/// Converts a mutable DOM node tree to and from the wire format, bridging the document object model to the serializer.
/// </summary>
/// <remarks>
/// The converter accepts every node type — the object, array, and value nodes as well as the <see cref="FormatNode" />
/// base — so it must lead the built-in converter list, ahead of the collection and object factories that would
/// otherwise claim the object node by virtue of its <see cref="System.Collections.Generic.IDictionary{TKey, TValue}" />
/// surface.
/// </remarks>
internal sealed class NodeConverter
    : SharedConverter<FormatNode>
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) =>
        typeof(FormatNode).IsAssignableFrom(typeToConvert);

    /// <inheritdoc />
    public override FormatNode Read(ref FormatReader reader, Type typeToConvert, FormatOptions options) =>
        FormatNode.ReadFrom(ref reader);

    /// <inheritdoc />
    public override void Write(FormatWriter writer, FormatNode value, FormatOptions options)
    {
        if (value is null)
            throw new FormatSerializationException(FormatResourceStrings.Op_NotSupported_NullNode);

        value.WriteTo(writer);
    }
}

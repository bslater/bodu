// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlNodeConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Nodes;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="TomlNode" /> tree to and from TOML, bridging the document object model to the serializer the
/// way <see cref="System.Text.Json.Serialization.JsonConverter{T}" /> bridges
/// <see cref="System.Text.Json.Nodes.JsonNode" /> to <see cref="System.Text.Json.JsonSerializer" />.
/// </summary>
/// <remarks>
/// The converter accepts every node type — <see cref="TomlObject" />, <see cref="TomlArray" />, and
/// <see cref="TomlValue" /> as well as the <see cref="TomlNode" /> base — so it must lead the built-in converter list,
/// ahead of the collection and object factories that would otherwise claim <see cref="TomlObject" /> by virtue of its
/// <see cref="System.Collections.Generic.IDictionary{TKey, TValue}" /> surface.
/// </remarks>
internal sealed class TomlNodeConverter
    : TomlConverter<TomlNode>
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) =>
        typeof(TomlNode).IsAssignableFrom(typeToConvert);

    /// <inheritdoc />
    public override TomlNode Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options) =>
        TomlNode.ReadFrom(ref reader);

    /// <inheritdoc />
    public override void Write(Utf8TomlWriter writer, TomlNode value, TomlSerializerOptions options)
    {
        if (value is null)
            throw new TomlSerializationException(TomlResourceStrings.Op_NotSupported_NullNode);

        value.WriteTo(writer);
    }
}

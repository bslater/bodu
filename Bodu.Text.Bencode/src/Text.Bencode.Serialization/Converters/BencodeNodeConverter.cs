// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeNodeConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Nodes;
using Bodu.Text.Bencode.Reader;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode.Serialization.Converters;

/// <summary>
/// Converts a <see cref="BencodeNode" /> tree to and from Bencode, bridging the document object model to the serializer
/// the way <see cref="System.Text.Json.Serialization.JsonConverter{T}" /> bridges
/// <see cref="System.Text.Json.Nodes.JsonNode" /> to <see cref="System.Text.Json.JsonSerializer" />.
/// </summary>
/// <remarks>
/// The converter accepts every node type — <see cref="BencodeObject" />, <see cref="BencodeArray" />, and
/// <see cref="BencodeValue" /> as well as the <see cref="BencodeNode" /> base — so it must lead the built-in converter
/// list, ahead of the collection and object factories that would otherwise claim <see cref="BencodeObject" /> by virtue
/// of its <see cref="System.Collections.Generic.IDictionary{TKey, TValue}" /> surface.
/// </remarks>
internal sealed class BencodeNodeConverter
    : BencodeConverter<BencodeNode>
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) =>
        typeof(BencodeNode).IsAssignableFrom(typeToConvert);

    /// <inheritdoc />
    public override BencodeNode Read(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options) =>
        BencodeNode.ReadFrom(ref reader);

    /// <inheritdoc />
    public override void Write(Utf8BencodeWriter writer, BencodeNode value, BencodeSerializerOptions options)
    {
        if (value is null)
            throw new BencodeSerializationException(BencodeResourceStrings.Op_NotSupported_NullNode);

        value.WriteTo(writer);
    }
}

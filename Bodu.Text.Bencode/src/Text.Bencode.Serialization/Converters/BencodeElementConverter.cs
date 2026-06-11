// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeElementConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Document;
using Bodu.Text.Bencode.Reader;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode.Serialization.Converters;

/// <summary>
/// Converts a <see cref="BencodeElement" /> to and from Bencode, bridging the read-only document object model to the
/// serializer the way <see cref="System.Text.Json.JsonElement" /> participates in
/// <see cref="System.Text.Json.JsonSerializer" />.
/// </summary>
/// <remarks>
/// On read the value's raw encoded bytes are captured and parsed into a non-pooled internal
/// <see cref="BencodeDocument" /> that backs the returned element — the same lifetime a
/// <see cref="BencodeElement.Clone" /> result has, so the element never requires disposal. The subtree is re-parsed
/// under the serializer's dictionary-key leniency, so a document the serializer accepts is also accepted here.
/// </remarks>
internal sealed class BencodeElementConverter
    : BencodeConverter<BencodeElement>
{
    /// <inheritdoc />
    public override BencodeElement Read(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(options);

        int start = reader.TokenStartIndex;
        reader.Skip();

        BencodeReaderOptions readerOptions = new()
        {
            MaxDepth = options.MaxDepth,
            AllowUnsortedKeys = options.AllowUnsortedKeys,
            AllowDuplicateKeys = options.AllowDuplicateKeys,
        };

        return BencodeDocument.ParseUnpooled(reader.Slice(start, reader.BytesConsumed), readerOptions).RootElement;
    }

    /// <inheritdoc />
    public override void Write(Utf8BencodeWriter writer, BencodeElement value, BencodeSerializerOptions options) =>
        value.WriteTo(writer);
}

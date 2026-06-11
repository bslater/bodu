// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeDocumentConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Document;
using Bodu.Text.Bencode.Reader;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode.Serialization.Converters;

/// <summary>
/// Converts a <see cref="BencodeDocument" /> to and from Bencode, bridging the read-only document object model to the
/// serializer.
/// </summary>
/// <remarks>
/// A document produced by deserialization is owned by the caller, who is responsible for disposing it. The value's
/// subtree is re-parsed under the serializer's dictionary-key leniency, so a document the serializer accepts is also
/// accepted here. Writing a disposed document surfaces the document's own <see cref="ObjectDisposedException" />.
/// </remarks>
internal sealed class BencodeDocumentConverter
    : BencodeConverter<BencodeDocument>
{
    /// <inheritdoc />
    public override BencodeDocument Read(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(options);

        int start = reader.TokenStartIndex;
        reader.Skip();

        BencodeDocumentOptions documentOptions = new()
        {
            MaxDepth = options.MaxDepth,
            AllowUnsortedKeys = options.AllowUnsortedKeys,
            AllowDuplicateKeys = options.AllowDuplicateKeys,
        };

        return BencodeDocument.Parse(reader.Slice(start, reader.BytesConsumed), documentOptions);
    }

    /// <inheritdoc />
    public override void Write(Utf8BencodeWriter writer, BencodeDocument value, BencodeSerializerOptions options)
    {
        if (value is null)
            throw new BencodeSerializationException(BencodeResourceStrings.Op_NotSupported_NullDocument);

        value.WriteTo(writer);
    }
}

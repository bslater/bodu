// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ObjectTypeConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Document;
using Bodu.Text.Bencode.Reader;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode.Serialization.Converters;

/// <summary>
/// Converts a value statically typed as <see cref="object" />: on write the value's runtime type selects the converter,
/// and on read the value surfaces as a <see cref="BencodeElement" />.
/// </summary>
/// <remarks>
/// <para>
/// A value whose runtime type is exactly <see cref="object" /> carries no data, so it writes as an empty dictionary.
/// Any other runtime type dispatches to that type's resolved converter, so a boxed integer, string, collection,
/// dictionary, or plain object writes exactly as it would when statically typed. A <see langword="null" /> value writes
/// nothing, matching the omission of null members Bencode's missing null form imposes.
/// </para>
/// <para>
/// On read the value's subtree is parsed into a <see cref="BencodeElement" /> backed by a non-pooled internal document,
/// so the element never requires disposal.
/// </para>
/// </remarks>
internal sealed class ObjectTypeConverter
    : BencodeConverter<object>
{
    /// <inheritdoc />
    public override object Read(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(options);

        var start = reader.TokenStartIndex;
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
    public override void Write(Utf8BencodeWriter writer, object value, BencodeSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(options);

        if (value is null)
            return;

        // A bare System.Object has no members and would otherwise resolve back to this converter and recurse.
        if (value.GetType() == typeof(object))
        {
            writer.WriteStartDictionary();
            writer.WriteEndDictionary();
            return;
        }

        options.GetConverter(value.GetType()).WriteAsObject(writer, value, options);
    }
}

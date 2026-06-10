// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Bencode.Reader;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode.Serialization.Converters;

/// <summary>
/// Converts a <see cref="string" /> value to and from a Bencode byte string, encoding and decoding the text as UTF-8.
/// </summary>
internal sealed class StringConverter
    : BencodeConverter<string>
{
    /// <inheritdoc />
    public override string Read(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options)
    {
        if (reader.TokenType != BencodeTokenType.ByteString)
        {
            throw new BencodeSerializationException(
                string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_ExpectedByteString, reader.TokenType),
                reader.BytesConsumed);
        }

        return reader.GetString();
    }

    /// <inheritdoc />
    public override void Write(Utf8BencodeWriter writer, string value, BencodeSerializerOptions options) =>
        writer.WriteString(value);
}

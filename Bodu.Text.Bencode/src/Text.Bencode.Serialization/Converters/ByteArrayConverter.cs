// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ByteArrayConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Bencode.Reader;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode.Serialization.Converters;

/// <summary>
/// Converts a <see cref="byte" /> array to and from a Bencode byte string, using the format's native byte-string
/// representation directly.
/// </summary>
internal sealed class ByteArrayConverter
    : BencodeConverter<byte[]>
{
    /// <inheritdoc />
    public override byte[] Read(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options)
    {
        if (reader.TokenType != BencodeTokenType.ByteString)
        {
            throw new BencodeSerializationException(
                string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_ExpectedByteString, reader.TokenType),
                reader.BytesConsumed);
        }

        return reader.GetBytes();
    }

    /// <inheritdoc />
    public override void Write(Utf8BencodeWriter writer, byte[] value, BencodeSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(value);
        writer.WriteByteString(value);
    }
}

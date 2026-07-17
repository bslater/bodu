// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReadOnlyMemoryByteConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Bencode.Reader;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode.Serialization.Converters;

/// <summary>
/// Converts a <see cref="ReadOnlyMemory{T}" /> of <see cref="byte" /> to and from a Bencode byte string, using the
/// format's native byte-string representation directly.
/// </summary>
internal sealed class ReadOnlyMemoryByteConverter
    : BencodeConverter<ReadOnlyMemory<byte>>
{
    /// <inheritdoc />
    public override ReadOnlyMemory<byte> Read(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options)
    {
        if (reader.TokenType != BencodeTokenType.ByteString)
        {
            throw new BencodeSerializationException(
                string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_ExpectedByteString, reader.TokenType),
                reader.TokenStartIndex);
        }

        return new ReadOnlyMemory<byte>(reader.GetBytes());
    }

    /// <inheritdoc />
    public override void Write(Utf8BencodeWriter writer, ReadOnlyMemory<byte> value, BencodeSerializerOptions options) =>
        writer.WriteByteString(value.Span);
}

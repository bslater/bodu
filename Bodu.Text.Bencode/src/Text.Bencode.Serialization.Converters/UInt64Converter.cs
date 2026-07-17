// ---------------------------------------------------------------------------------------------------------------
// <copyright file="UInt64Converter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Bencode.Reader;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode.Serialization.Converters;

/// <summary>
/// Converts a <see cref="ulong" /> to and from a Bencode integer using the reader's and writer's unsigned 64-bit
/// surfaces, so the full [0, <see cref="ulong.MaxValue" />] range round-trips.
/// </summary>
/// <remarks>
/// Bencode integers are arbitrary-precision per BEP 3, so values above <see cref="long.MaxValue" /> are valid
/// documents; this converter reads them through <see cref="Utf8BencodeReader.TryGetUInt64" /> rather than the signed
/// accessor the shared <see cref="IntegerConverter{T}" /> uses. A negative document value surfaces as a
/// <see cref="BencodeSerializationException" />, matching the overflow contract of the other fixed-width integer types.
/// </remarks>
internal sealed class UInt64Converter
    : BencodeConverter<ulong>
{
    /// <inheritdoc />
    public override ulong Read(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options)
    {
        if (reader.TokenType != BencodeTokenType.Integer)
        {
            throw new BencodeSerializationException(
                string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_ExpectedInteger, reader.TokenType),
                reader.TokenStartIndex);
        }

        if (reader.TryGetUInt64(out ulong value))
            return value;

        // The token is a negative integer; report it as an overflow of the target type, mirroring IntegerConverter<T>.
        throw new BencodeSerializationException(
            string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_IntegerOverflow, reader.GetInt64(), typeof(ulong)));
    }

    /// <inheritdoc />
    public override void Write(Utf8BencodeWriter writer, ulong value, BencodeSerializerOptions options) =>
        writer.WriteInteger(value);
}

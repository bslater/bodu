// ---------------------------------------------------------------------------------------------------------------
// <copyright file="UInt128Converter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Bencode.Reader;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode.Serialization.Converters;

/// <summary>
/// Converts a <see cref="UInt128" /> to and from a Bencode integer through the reader's and writer's unsigned 64-bit
/// surfaces, so the full [0, <see cref="ulong.MaxValue" />] range round-trips and a larger value surfaces as a
/// serialization error.
/// </summary>
/// <remarks>
/// Bencode integers are arbitrary-precision per BEP 3, but this implementation reads and writes through the 64-bit
/// surfaces, so a <see cref="UInt128" /> above <see cref="ulong.MaxValue" /> cannot be written — mirroring the way the
/// signed 128-bit type is confined to the signed 64-bit range by <see cref="IntegerConverter{T}" />. A negative
/// document value surfaces as a <see cref="BencodeSerializationException" />, matching the overflow contract of
/// <see cref="UInt64Converter" />.
/// </remarks>
internal sealed class UInt128Converter
    : BencodeConverter<UInt128>
{
    /// <inheritdoc />
    public override UInt128 Read(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options)
    {
        if (reader.TokenType != BencodeTokenType.Integer)
        {
            throw new BencodeSerializationException(
                string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_ExpectedInteger, reader.TokenType),
                reader.BytesConsumed);
        }

        if (reader.TryGetUInt64(out ulong value))
            return value;

        // The token is a negative integer; report it as an overflow of the target type, mirroring UInt64Converter.
        throw new BencodeSerializationException(
            string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_IntegerOverflow, reader.GetInt64(), typeof(UInt128)));
    }

    /// <inheritdoc />
    public override void Write(Utf8BencodeWriter writer, UInt128 value, BencodeSerializerOptions options)
    {
        try
        {
            writer.WriteInteger(ulong.CreateChecked(value));
        }
        catch (OverflowException ex)
        {
            throw new BencodeSerializationException(
                string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_IntegerOverflow, value, typeof(ulong)),
                ex);
        }
    }
}

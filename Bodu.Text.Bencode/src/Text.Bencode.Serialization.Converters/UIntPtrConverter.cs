// ---------------------------------------------------------------------------------------------------------------
// <copyright file="UIntPtrConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Bencode.Reader;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode.Serialization.Converters;

/// <summary>
/// Converts a <see cref="nuint" /> to and from a Bencode integer using the reader's and writer's unsigned 64-bit
/// surfaces, so a 64-bit process round-trips the full native-unsigned range.
/// </summary>
/// <remarks>
/// Routing <see cref="nuint" /> through the shared <see cref="IntegerConverter{T}" /> would confine it to the signed
/// 64-bit surface, throwing for values above <see cref="long.MaxValue" /> that <see cref="ulong" /> — via
/// <see cref="UInt64Converter" /> — accepts. A negative document value, or one above the platform's native-unsigned
/// range, surfaces as a <see cref="BencodeSerializationException" />, matching the overflow contract of the other
/// fixed-width integer types.
/// </remarks>
internal sealed class UIntPtrConverter
    : BencodeConverter<nuint>
{
    /// <inheritdoc />
    public override nuint Read(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options)
    {
        if (reader.TokenType != BencodeTokenType.Integer)
        {
            throw new BencodeSerializationException(
                string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_ExpectedInteger, reader.TokenType),
                reader.TokenStartIndex);
        }

        if (reader.TryGetUInt64(out ulong value))
        {
            try
            {
                return nuint.CreateChecked(value);
            }
            catch (OverflowException ex)
            {
                throw new BencodeSerializationException(
                    string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_IntegerOverflow, value, typeof(nuint)),
                    ex);
            }
        }

        // The token is a negative integer; report it as an overflow of the target type, mirroring IntegerConverter<T>.
        throw new BencodeSerializationException(
            string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_IntegerOverflow, reader.GetInt64(), typeof(nuint)));
    }

    /// <inheritdoc />
    public override void Write(Utf8BencodeWriter writer, nuint value, BencodeSerializerOptions options) =>
        writer.WriteInteger(value);
}

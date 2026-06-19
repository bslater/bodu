// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntegerConverter{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Numerics;
using Bodu.Text.Bencode.Reader;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode.Serialization.Converters;

/// <summary>
/// Converts any fixed-width integer type to and from a Bencode integer, using checked conversions so a value outside
/// the target type's range — or outside the signed 64-bit surface this converter reads and writes through — surfaces as
/// a serialization error rather than silently wrapping. <see cref="ulong" /> is handled by the dedicated
/// <see cref="UInt64Converter" /> instead, which spans the full unsigned range.
/// </summary>
/// <typeparam name="T">The integer type, constrained to <see cref="IBinaryInteger{TSelf}" />.</typeparam>
internal sealed class IntegerConverter<T>
    : BencodeConverter<T>
    where T : IBinaryInteger<T>
{
    /// <inheritdoc />
    public override T Read(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options)
    {
        if (reader.TokenType != BencodeTokenType.Integer)
        {
            throw new BencodeSerializationException(
                string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_ExpectedInteger, reader.TokenType),
                reader.BytesConsumed);
        }

        long value = reader.GetInt64();
        try
        {
            return T.CreateChecked(value);
        }
        catch (OverflowException ex)
        {
            throw new BencodeSerializationException(
                string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_IntegerOverflow, value, typeof(T)),
                ex);
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8BencodeWriter writer, T value, BencodeSerializerOptions options)
    {
        try
        {
            writer.WriteInteger(long.CreateChecked(value));
        }
        catch (OverflowException ex)
        {
            throw new BencodeSerializationException(
                string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_IntegerOverflow, value, typeof(long)),
                ex);
        }
    }
}

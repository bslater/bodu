// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumNumberConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Text.Bencode.Serialization.Converters;

/// <summary>
/// Converts an enumeration value to and from a Bencode integer carrying its underlying numeric value. Mirrors
/// <see cref="System.Text.Json.Serialization.JsonNumberEnumConverter{TEnum}" />.
/// </summary>
/// <typeparam name="T">The enumeration type.</typeparam>
/// <remarks>
/// The numeric value is read and written through a signed 64-bit integer, which is the only integer width Bencode can
/// store; an enumeration whose underlying type is <see cref="ulong" /> with values above <see cref="long.MaxValue" />
/// therefore cannot be represented.
/// </remarks>
internal sealed class EnumNumberConverter<T>
    : BencodeConverter<T>
    where T : struct, Enum
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

        return (T)Enum.ToObject(typeof(T), reader.GetInt64());
    }

    /// <inheritdoc />
    public override void Write(Utf8BencodeWriter writer, T value, BencodeSerializerOptions options) =>
        writer.WriteInteger(Convert.ToInt64(value, CultureInfo.InvariantCulture));
}

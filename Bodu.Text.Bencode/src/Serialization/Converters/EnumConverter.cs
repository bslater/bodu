// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Text.Bencode.Serialization.Converters;

/// <summary>
/// Converts an enumeration value to and from a Bencode byte string holding its member name. Parsing is
/// case-insensitive.
/// </summary>
/// <typeparam name="T">The enumeration type.</typeparam>
internal sealed class EnumConverter<T>
    : BencodeConverter<T>
    where T : struct, Enum
{
    /// <inheritdoc />
    public override T Read(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options)
    {
        if (reader.TokenType != BencodeTokenType.ByteString)
        {
            throw new BencodeSerializationException(
                string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_ExpectedByteString, reader.TokenType),
                reader.BytesConsumed);
        }

        var text = reader.GetString();
        return Enum.TryParse(text, ignoreCase: true, out T value)
            ? value
            : throw new BencodeSerializationException(
                string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_EnumValueNotFound, text, typeof(T)),
                reader.BytesConsumed);
    }

    /// <inheritdoc />
    public override void Write(Utf8BencodeWriter writer, T value, BencodeSerializerOptions options) =>
        writer.WriteString(value.ToString());
}

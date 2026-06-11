// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ByteArrayConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="byte" /> array to and from TOML. Because TOML has no native binary type, the array maps either
/// to an array of integers — one TOML integer per byte — or to a Base64 basic string, selected by
/// <see cref="TomlSerializerOptions.ByteArrayHandling" />.
/// </summary>
/// <remarks>
/// On read the converter accepts both forms regardless of the configured handling: an array of integers is read back
/// into a byte array, and a string is Base64-decoded. This mirrors the way
/// <see cref="System.Text.Json.JsonSerializer" /> reads a byte array from a Base64 string.
/// </remarks>
internal sealed class ByteArrayConverter
    : TomlConverter<byte[]>
{
    /// <inheritdoc />
    public override byte[] Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options)
    {
        if (reader.TokenType == TomlTokenType.String)
        {
            try
            {
                return Convert.FromBase64String(reader.GetString());
            }
            catch (FormatException ex)
            {
                throw new TomlSerializationException(
                    string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_ExpectedByteArray, reader.TokenType),
                    ex);
            }
        }

        if (reader.TokenType != TomlTokenType.StartArray)
        {
            throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_ExpectedByteArray, reader.TokenType));
        }

        List<byte> bytes = [];
        while (reader.Read() && reader.TokenType != TomlTokenType.EndArray)
        {
            if (reader.TokenType != TomlTokenType.Integer)
            {
                throw new TomlSerializationException(
                    string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_ExpectedInteger, reader.TokenType));
            }

            var value = reader.GetInt64();
            if (value is < byte.MinValue or > byte.MaxValue)
            {
                throw new TomlSerializationException(
                    string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_IntegerOverflow, value, typeof(byte)));
            }

            bytes.Add((byte)value);
        }

        return [.. bytes];
    }

    /// <inheritdoc />
    public override void Write(Utf8TomlWriter writer, byte[] value, TomlSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(options);

        if (options.ByteArrayHandling == TomlByteArrayHandling.Base64String)
        {
            writer.WriteString(Convert.ToBase64String(value));
            return;
        }

        writer.WriteStartArray();
        foreach (byte b in value)
            writer.WriteInteger(b);

        writer.WriteEndArray();
    }
}

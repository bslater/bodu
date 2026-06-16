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
/// into a byte array, and a string is Base64-decoded. The <see cref="ReadCore" /> and <see cref="WriteCore" /> helpers
/// carry the shared logic so the memory-of-byte converters apply identical semantics.
/// </remarks>
internal sealed class ByteArrayConverter
    : TomlConverter<byte[]>
{
    /// <inheritdoc />
    public override byte[] Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options) =>
        ReadCore(ref reader);

    /// <inheritdoc />
    public override void Write(Utf8TomlWriter writer, byte[] value, TomlSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(options);

        WriteCore(writer, value, options);
    }

    /// <summary>
    /// Reads binary data from the reader's current value, accepting either a Base64 basic string or a TOML array of
    /// integers within the byte range.
    /// </summary>
    /// <param name="reader">The reader positioned on the value's first token.</param>
    /// <returns>The decoded bytes.</returns>
    /// <exception cref="TomlSerializationException">
    /// Thrown when the value is neither a string nor an array, the string is not valid Base64, an array element is not
    /// an integer, or an element is outside the <see cref="byte" /> range.
    /// </exception>
    internal static byte[] ReadCore(ref TomlDocumentReader reader)
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

            long value = reader.GetInt64();
            if (value is < byte.MinValue or > byte.MaxValue)
            {
                throw new TomlSerializationException(
                    string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_IntegerOverflow, value, typeof(byte)));
            }

            bytes.Add((byte)value);
        }

        return [.. bytes];
    }

    /// <summary>
    /// Writes binary data to the writer in the representation selected by
    /// <see cref="TomlSerializerOptions.ByteArrayHandling" />.
    /// </summary>
    /// <param name="writer">The destination writer.</param>
    /// <param name="value">The bytes to write.</param>
    /// <param name="options">The serializer options that select the representation.</param>
    internal static void WriteCore(Utf8TomlWriter writer, ReadOnlySpan<byte> value, TomlSerializerOptions options)
    {
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

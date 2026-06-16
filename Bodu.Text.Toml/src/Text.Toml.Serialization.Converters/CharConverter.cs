// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CharConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="char" /> value to and from a single-character TOML string.
/// </summary>
internal sealed class CharConverter
    : TomlConverter<char>
{
    /// <inheritdoc />
    public override char Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options)
    {
        if (reader.TokenType != TomlTokenType.String)
        {
            throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_ExpectedString, reader.TokenType));
        }

        string text = reader.GetString();
        return text.Length == 1
            ? text[0]
            : throw new TomlSerializationException(TomlResourceStrings.Op_Invalid_CharLength);
    }

    /// <inheritdoc />
    public override void Write(Utf8TomlWriter writer, char value, TomlSerializerOptions options) =>
        writer.WriteString(value.ToString());
}

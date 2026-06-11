// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GuidConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="Guid" /> value to and from its canonical 36-character TOML string (the <c>D</c> format).
/// </summary>
internal sealed class GuidConverter
    : TomlConverter<Guid>
{
    /// <inheritdoc />
    public override Guid Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options)
    {
        if (reader.TokenType != TomlTokenType.String)
        {
            throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_ExpectedString, reader.TokenType));
        }

        var text = reader.GetString();
        return Guid.TryParseExact(text, "D", out Guid value)
            ? value
            : throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_TypeConversion, text, typeof(Guid)));
    }

    /// <inheritdoc />
    public override void Write(Utf8TomlWriter writer, Guid value, TomlSerializerOptions options) =>
        writer.WriteString(value.ToString("D", CultureInfo.InvariantCulture));
}

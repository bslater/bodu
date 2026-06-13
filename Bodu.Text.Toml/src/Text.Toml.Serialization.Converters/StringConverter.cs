// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="string" /> value to and from a TOML string.
/// </summary>
internal sealed class StringConverter
    : TomlConverter<string>
{
    /// <inheritdoc />
    public override string Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options)
    {
        if (reader.TokenType != TomlTokenType.String)
        {
            throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_ExpectedString, reader.TokenType));
        }

        return reader.GetString();
    }

    /// <inheritdoc />
    public override void Write(Utf8TomlWriter writer, string value, TomlSerializerOptions options) =>
        writer.WriteString(value);
}

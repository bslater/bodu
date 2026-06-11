// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BooleanConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="bool" /> value to and from a TOML Boolean.
/// </summary>
internal sealed class BooleanConverter
    : TomlConverter<bool>
{
    /// <inheritdoc />
    public override bool Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options)
    {
        if (reader.TokenType != TomlTokenType.Boolean)
        {
            throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_ExpectedBoolean, reader.TokenType));
        }

        return reader.GetBoolean();
    }

    /// <inheritdoc />
    public override void Write(Utf8TomlWriter writer, bool value, TomlSerializerOptions options) =>
        writer.WriteBoolean(value);
}

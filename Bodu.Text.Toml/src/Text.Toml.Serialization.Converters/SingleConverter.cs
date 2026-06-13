// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SingleConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="float" /> value to and from a TOML floating-point value, widening to and narrowing from
/// <see cref="double" /> at the format boundary.
/// </summary>
internal sealed class SingleConverter
    : TomlConverter<float>
{
    /// <inheritdoc />
    public override float Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options)
    {
        if (reader.TokenType != TomlTokenType.Float)
        {
            throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_ExpectedFloat, reader.TokenType));
        }

        return (float)reader.GetDouble();
    }

    /// <inheritdoc />
    public override void Write(Utf8TomlWriter writer, float value, TomlSerializerOptions options) =>
        writer.WriteFloat(value);
}

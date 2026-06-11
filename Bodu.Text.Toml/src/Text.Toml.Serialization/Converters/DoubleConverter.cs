// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DoubleConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="double" /> value to and from a TOML floating-point value.
/// </summary>
internal sealed class DoubleConverter
    : TomlConverter<double>
{
    /// <inheritdoc />
    public override double Read(ref Utf8TomlReader reader, Type typeToConvert, TomlSerializerOptions options)
    {
        if (reader.TokenType != TomlTokenType.Float)
        {
            throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_ExpectedFloat, reader.TokenType));
        }

        return reader.GetDouble();
    }

    /// <inheritdoc />
    public override void Write(Utf8TomlWriter writer, double value, TomlSerializerOptions options) =>
        writer.WriteFloat(value);
}

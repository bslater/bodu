// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TimeOnlyConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="System.TimeOnly" /> value to and from a TOML local time.
/// </summary>
internal sealed class TimeOnlyConverter
    : TomlConverter<TimeOnly>
{
    /// <inheritdoc />
    public override TimeOnly Read(ref Utf8TomlReader reader, Type typeToConvert, TomlSerializerOptions options)
    {
        if (reader.TokenType != TomlTokenType.LocalTime)
        {
            throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_ExpectedLocalTime, reader.TokenType));
        }

        return reader.GetTimeOnly();
    }

    /// <inheritdoc />
    public override void Write(Utf8TomlWriter writer, TimeOnly value, TomlSerializerOptions options) =>
        writer.WriteLocalTime(value);
}

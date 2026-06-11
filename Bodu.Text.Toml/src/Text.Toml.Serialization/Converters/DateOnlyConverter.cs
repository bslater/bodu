// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="System.DateOnly" /> value to and from a TOML local date.
/// </summary>
internal sealed class DateOnlyConverter
    : TomlConverter<DateOnly>
{
    /// <inheritdoc />
    public override DateOnly Read(ref Utf8TomlReader reader, Type typeToConvert, TomlSerializerOptions options)
    {
        if (reader.TokenType != TomlTokenType.LocalDate)
        {
            throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_ExpectedLocalDate, reader.TokenType));
        }

        return reader.GetDateOnly();
    }

    /// <inheritdoc />
    public override void Write(Utf8TomlWriter writer, DateOnly value, TomlSerializerOptions options) =>
        writer.WriteLocalDate(value);
}

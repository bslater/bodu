// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeOffsetConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="System.DateTimeOffset" /> value to and from a TOML offset date-time.
/// </summary>
internal sealed class DateTimeOffsetConverter
    : TomlConverter<DateTimeOffset>
{
    /// <inheritdoc />
    public override DateTimeOffset Read(ref Utf8TomlReader reader, Type typeToConvert, TomlSerializerOptions options)
    {
        if (reader.TokenType != TomlTokenType.OffsetDateTime)
        {
            throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_ExpectedOffsetDateTime, reader.TokenType));
        }

        return reader.GetDateTimeOffset();
    }

    /// <inheritdoc />
    public override void Write(Utf8TomlWriter writer, DateTimeOffset value, TomlSerializerOptions options) =>
        writer.WriteOffsetDateTime(value);
}

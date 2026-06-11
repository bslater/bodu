// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="System.DateTime" /> value to and from a TOML date-time, selecting the local or offset form on
/// write according to the value's <see cref="System.DateTime.Kind" />.
/// </summary>
/// <remarks>
/// A <see cref="DateTimeKind.Unspecified" /> value carries no zone relation and is written as a TOML local date-time; a
/// <see cref="DateTimeKind.Utc" /> or <see cref="DateTimeKind.Local" /> value is written as a TOML offset date-time. On
/// read a local date-time is returned with <see cref="DateTimeKind.Unspecified" />, matching the source token.
/// </remarks>
internal sealed class DateTimeConverter
    : TomlConverter<DateTime>
{
    /// <inheritdoc />
    public override DateTime Read(ref Utf8TomlReader reader, Type typeToConvert, TomlSerializerOptions options)
    {
        if (reader.TokenType != TomlTokenType.LocalDateTime)
        {
            throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_ExpectedLocalDateTime, reader.TokenType));
        }

        return reader.GetDateTime();
    }

    /// <inheritdoc />
    public override void Write(Utf8TomlWriter writer, DateTime value, TomlSerializerOptions options)
    {
        if (value.Kind == DateTimeKind.Unspecified)
            writer.WriteLocalDateTime(value);
        else
            writer.WriteOffsetDateTime(new DateTimeOffset(value));
    }
}

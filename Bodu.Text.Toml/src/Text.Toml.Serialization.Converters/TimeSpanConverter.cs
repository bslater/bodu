// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TimeSpanConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="TimeSpan" /> value to and from a TOML string in the invariant, round-trippable constant (
/// <c>"c"</c>) format.
/// </summary>
/// <remarks>
/// TOML has no native duration type, so the value maps to a basic string such as <c>"1.02:03:04.5670000"</c>. On read
/// the string must match the constant format exactly; any other shape surfaces as a serialization error.
/// </remarks>
internal sealed class TimeSpanConverter
    : TomlConverter<TimeSpan>
{
    /// <inheritdoc />
    public override TimeSpan Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options)
    {
        if (reader.TokenType != TomlTokenType.String)
        {
            throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_ExpectedString, reader.TokenType));
        }

        var text = reader.GetString();
        return TimeSpan.TryParseExact(text, "c", CultureInfo.InvariantCulture, TimeSpanStyles.None, out TimeSpan value)
            ? value
            : throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_TypeConversion, text, typeof(TimeSpan)));
    }

    /// <inheritdoc />
    public override void Write(Utf8TomlWriter writer, TimeSpan value, TomlSerializerOptions options) =>
        writer.WriteString(value.ToString("c", CultureInfo.InvariantCulture));
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TimeSpanConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Text.Serialization.Converters;

/// <summary>
/// Converts a <see cref="System.TimeSpan" /> value to and from a string token using the round-trippable constant (
/// <c>c</c>) format, because the formats have no native duration kind.
/// </summary>
internal sealed class TimeSpanConverter
    : FormatConverter<TimeSpan>
{
    /// <inheritdoc />
    public override TimeSpan Read(ISerializationReader reader, Type typeToConvert, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(reader);

        var text = reader.GetString();
        return TimeSpan.TryParseExact(text, "c", CultureInfo.InvariantCulture, out TimeSpan value)
            ? value
            : throw new FormatSerializationException(
                string.Format(CultureInfo.CurrentCulture, SerializationResourceStrings.Op_Invalid_TypeMismatch, SerializationValueKind.String, typeof(TimeSpan)),
                reader.Location);
    }

    /// <inheritdoc />
    public override void Write(ISerializationWriter writer, TimeSpan value, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);
        writer.WriteString(value.ToString("c", CultureInfo.InvariantCulture));
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeOffsetConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Converters;

/// <summary>
/// Converts a <see cref="System.DateTimeOffset" /> value to and from a date-time-with-offset token.
/// </summary>
internal sealed class DateTimeOffsetConverter
    : FormatConverter<DateTimeOffset>
{
    /// <inheritdoc />
    public override DateTimeOffset Read(ISerializationReader reader, Type typeToConvert, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(reader);
        return reader.GetDateTimeOffset();
    }

    /// <inheritdoc />
    public override void Write(ISerializationWriter writer, DateTimeOffset value, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);
        writer.WriteDateTimeOffset(value);
    }
}

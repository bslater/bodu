// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Converters;

/// <summary>
/// Converts a <see cref="System.DateTime" /> value to and from a date-time token.
/// </summary>
internal sealed class DateTimeConverter
    : FormatConverter<DateTime>
{
    /// <inheritdoc />
    public override DateTime Read(ISerializationReader reader, Type typeToConvert, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(reader);
        return reader.GetDateTime();
    }

    /// <inheritdoc />
    public override void Write(ISerializationWriter writer, DateTime value, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);
        writer.WriteDateTime(value);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Converters;

/// <summary>
/// Converts a <see cref="System.DateOnly" /> value to and from a date token.
/// </summary>
internal sealed class DateOnlyConverter
    : FormatConverter<DateOnly>
{
    /// <inheritdoc />
    public override DateOnly Read(ISerializationReader reader, Type typeToConvert, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(reader);
        return reader.GetDateOnly();
    }

    /// <inheritdoc />
    public override void Write(ISerializationWriter writer, DateOnly value, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);
        writer.WriteDateOnly(value);
    }
}

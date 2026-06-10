// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TimeOnlyConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Converters;

/// <summary>
/// Converts a <see cref="System.TimeOnly" /> value to and from a time token.
/// </summary>
internal sealed class TimeOnlyConverter
    : FormatConverter<TimeOnly>
{
    /// <inheritdoc />
    public override TimeOnly Read(ISerializationReader reader, Type typeToConvert, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(reader);
        return reader.GetTimeOnly();
    }

    /// <inheritdoc />
    public override void Write(ISerializationWriter writer, TimeOnly value, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);
        writer.WriteTimeOnly(value);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SingleConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Converters;

/// <summary>
/// Converts a <see cref="float" /> value to and from a floating-point token, widening to and narrowing from
/// <see cref="double" /> at the format boundary.
/// </summary>
internal sealed class SingleConverter
    : FormatConverter<float>
{
    /// <inheritdoc />
    public override float Read(ISerializationReader reader, Type typeToConvert, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(reader);
        return (float)reader.GetDouble();
    }

    /// <inheritdoc />
    public override void Write(ISerializationWriter writer, float value, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);
        writer.WriteDouble(value);
    }
}

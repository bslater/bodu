// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DoubleConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Converters;

/// <summary>
/// Converts a <see cref="double" /> value to and from a floating-point token.
/// </summary>
internal sealed class DoubleConverter
    : FormatConverter<double>
{
    /// <inheritdoc />
    public override double Read(ISerializationReader reader, Type typeToConvert, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(reader);
        return reader.GetDouble();
    }

    /// <inheritdoc />
    public override void Write(ISerializationWriter writer, double value, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);
        writer.WriteDouble(value);
    }
}

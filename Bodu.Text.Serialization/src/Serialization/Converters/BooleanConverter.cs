// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BooleanConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Converters;

/// <summary>
/// Converts a <see cref="bool" /> value to and from a Boolean token.
/// </summary>
internal sealed class BooleanConverter
    : FormatConverter<bool>
{
    /// <inheritdoc />
    public override bool Read(ISerializationReader reader, Type typeToConvert, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(reader);
        return reader.GetBoolean();
    }

    /// <inheritdoc />
    public override void Write(ISerializationWriter writer, bool value, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);
        writer.WriteBoolean(value);
    }
}

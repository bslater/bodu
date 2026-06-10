// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Converters;

/// <summary>
/// Converts a <see cref="string" /> value to and from a string token.
/// </summary>
internal sealed class StringConverter
    : FormatConverter<string>
{
    /// <inheritdoc />
    public override string Read(ISerializationReader reader, Type typeToConvert, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(reader);
        return reader.GetString();
    }

    /// <inheritdoc />
    public override void Write(ISerializationWriter writer, string value, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);
        writer.WriteString(value);
    }
}

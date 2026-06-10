// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CharConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Converters;

/// <summary>
/// Converts a <see cref="char" /> value to and from a single-character string token.
/// </summary>
internal sealed class CharConverter
    : FormatConverter<char>
{
    /// <inheritdoc />
    public override char Read(ISerializationReader reader, Type typeToConvert, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(reader);

        var text = reader.GetString();
        return text.Length == 1
            ? text[0]
            : throw new FormatSerializationException(SerializationResourceStrings.Op_Invalid_CharLength, reader.Location);
    }

    /// <inheritdoc />
    public override void Write(ISerializationWriter writer, char value, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);
        writer.WriteString(value.ToString());
    }
}

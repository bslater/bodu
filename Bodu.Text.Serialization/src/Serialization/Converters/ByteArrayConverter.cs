// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ByteArrayConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Converters;

/// <summary>
/// Converts a <see cref="byte" /> array to and from a binary token, using the format's native representation for bytes.
/// </summary>
/// <remarks>
/// A format that has no binary kind (such as TOML) maps a binary token onto its own representation; this converter
/// leaves that choice to the writer rather than imposing an encoding here.
/// </remarks>
internal sealed class ByteArrayConverter
    : FormatConverter<byte[]>
{
    /// <inheritdoc />
    public override byte[] Read(ISerializationReader reader, Type typeToConvert, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(reader);
        return reader.GetBytes().ToArray();
    }

    /// <inheritdoc />
    public override void Write(ISerializationWriter writer, byte[] value, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);
        ThrowHelper.ThrowIfNull(value);
        writer.WriteBytes(value);
    }
}

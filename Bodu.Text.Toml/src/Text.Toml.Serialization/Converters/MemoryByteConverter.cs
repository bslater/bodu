// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MemoryByteConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="Memory{T}" /> of <see cref="byte" /> to and from TOML using the byte-array representation
/// selected by <see cref="TomlSerializerOptions.ByteArrayHandling" />, mirroring the way
/// <see cref="System.Text.Json.JsonSerializer" /> maps memory-of-byte to a Base64 string.
/// </summary>
/// <remarks>
/// The converter shares its read and write logic with the <see cref="byte" />-array converter, so both the Base64
/// string and integer-array forms are accepted on read regardless of the configured handling.
/// </remarks>
internal sealed class MemoryByteConverter
    : TomlConverter<Memory<byte>>
{
    /// <inheritdoc />
    public override Memory<byte> Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options) =>
        new(ByteArrayConverter.ReadCore(ref reader));

    /// <inheritdoc />
    public override void Write(Utf8TomlWriter writer, Memory<byte> value, TomlSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(options);

        ByteArrayConverter.WriteCore(writer, value.Span, options);
    }
}

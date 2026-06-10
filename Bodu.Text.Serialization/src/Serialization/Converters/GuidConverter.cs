// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GuidConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Text.Serialization.Converters;

/// <summary>
/// Converts a <see cref="Guid" /> value to and from its canonical 36-character string token (the <c>D</c> format).
/// </summary>
internal sealed class GuidConverter
    : FormatConverter<Guid>
{
    /// <inheritdoc />
    public override Guid Read(ISerializationReader reader, Type typeToConvert, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(reader);

        var text = reader.GetString();
        return Guid.TryParseExact(text, "D", out Guid value)
            ? value
            : throw new FormatSerializationException(
                string.Format(CultureInfo.CurrentCulture, SerializationResourceStrings.Op_Invalid_TypeMismatch, SerializationValueKind.String, typeof(Guid)),
                reader.Location);
    }

    /// <inheritdoc />
    public override void Write(ISerializationWriter writer, Guid value, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);
        writer.WriteString(value.ToString("D", CultureInfo.InvariantCulture));
    }
}

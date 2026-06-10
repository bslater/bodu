// ---------------------------------------------------------------------------------------------------------------
// <copyright file="UriConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Text.Serialization.Converters;

/// <summary>
/// Converts a <see cref="Uri" /> value to and from a string token holding its original string form.
/// </summary>
internal sealed class UriConverter
    : FormatConverter<Uri>
{
    /// <inheritdoc />
    public override Uri Read(ISerializationReader reader, Type typeToConvert, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(reader);

        var text = reader.GetString();
        return Uri.TryCreate(text, UriKind.RelativeOrAbsolute, out Uri? value)
            ? value
            : throw new FormatSerializationException(
                string.Format(CultureInfo.CurrentCulture, SerializationResourceStrings.Op_Invalid_TypeMismatch, SerializationValueKind.String, typeof(Uri)),
                reader.Location);
    }

    /// <inheritdoc />
    public override void Write(ISerializationWriter writer, Uri value, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);
        ThrowHelper.ThrowIfNull(value);
        writer.WriteString(value.OriginalString);
    }
}

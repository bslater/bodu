// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Text.Serialization.Converters;

/// <summary>
/// Converts an enumeration value to and from a string token holding its member name. Parsing is case-insensitive.
/// </summary>
/// <typeparam name="T">The enumeration type.</typeparam>
internal sealed class EnumConverter<T>
    : FormatConverter<T>
    where T : struct, Enum
{
    /// <inheritdoc />
    public override T Read(ISerializationReader reader, Type typeToConvert, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(reader);

        var text = reader.GetString();
        return Enum.TryParse(text, ignoreCase: true, out T value)
            ? value
            : throw new FormatSerializationException(
                string.Format(CultureInfo.CurrentCulture, SerializationResourceStrings.Op_Invalid_EnumValueNotFound, text, typeof(T)),
                reader.Location);
    }

    /// <inheritdoc />
    public override void Write(ISerializationWriter writer, T value, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);
        writer.WriteString(value.ToString());
    }
}

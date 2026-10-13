// ---------------------------------------------------------------------------------------------------------------
// <copyright file="UriConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="Uri" /> to and from a YAML string holding its original string form. Reading accepts an
/// absolute or a relative URI; a null scalar reads as <see langword="null" />.
/// </summary>
internal sealed class UriConverter
    : YamlConverter<Uri>
{
    /// <inheritdoc />
    /// <exception cref="YamlSerializationException">
    /// The value is not a scalar, or its text is neither an absolute nor a relative URI.
    /// </exception>
    public override Uri Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options)
    {
        if (reader.TokenType == YamlTokenType.Null)
            return null!;

        string text = ScalarCoercion.ReaderScalarText(ref reader);
        return Uri.TryCreate(text, UriKind.RelativeOrAbsolute, out Uri? value)
            ? value
            : throw new YamlSerializationException(
                string.Format(CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlValueConversion, typeof(Uri)));
    }

    /// <inheritdoc />
    public override void Write(Utf8YamlWriter writer, Uri value, YamlSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(value);
        writer.WriteString(value.OriginalString);
    }
}

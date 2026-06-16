// ---------------------------------------------------------------------------------------------------------------
// <copyright file="UriConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="Uri" /> value to and from a TOML string holding its original string form.
/// </summary>
internal sealed class UriConverter
    : TomlConverter<Uri>
{
    /// <inheritdoc />
    public override Uri Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options)
    {
        if (reader.TokenType != TomlTokenType.String)
        {
            throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_ExpectedString, reader.TokenType));
        }

        string text = reader.GetString();
        return Uri.TryCreate(text, UriKind.RelativeOrAbsolute, out Uri? value)
            ? value
            : throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_TypeConversion, text, typeof(Uri)));
    }

    /// <inheritdoc />
    public override void Write(Utf8TomlWriter writer, Uri value, TomlSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(value);
        writer.WriteString(value.OriginalString);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="VersionConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="Version" /> value to and from a TOML string holding its component form.
/// </summary>
/// <remarks>
/// On read the string must contain the version text with no leading or trailing whitespace; a padded or otherwise
/// unparsable string surfaces as a serialization error.
/// </remarks>
internal sealed class VersionConverter
    : TomlConverter<Version>
{
    /// <inheritdoc />
    public override Version Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options)
    {
        if (reader.TokenType != TomlTokenType.String)
        {
            throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_ExpectedString, reader.TokenType));
        }

        string text = reader.GetString();
        if (text.Length > 0 && (char.IsWhiteSpace(text[0]) || char.IsWhiteSpace(text[^1])))
        {
            throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_TypeConversion, text, typeof(Version)));
        }

        return Version.TryParse(text, out Version? value)
            ? value
            : throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_TypeConversion, text, typeof(Version)));
    }

    /// <inheritdoc />
    public override void Write(Utf8TomlWriter writer, Version value, TomlSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(value);
        writer.WriteString(value.ToString());
    }
}

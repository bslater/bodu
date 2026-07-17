// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CharConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="char" /> to and from a single-character YAML scalar. A null scalar reads as the type default.
/// </summary>
internal sealed class CharConverter
    : YamlConverter<char>
{
    /// <inheritdoc />
    public override char Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options)
    {
        if (reader.TokenType == YamlTokenType.Null)
            return default;

        string s = ScalarCoercion.ReaderScalarText(ref reader);
        if (s.Length != 1)
        {
            throw new YamlSerializationException(string.Format(
                CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlCharLength, s));
        }

        return s[0];
    }

    /// <inheritdoc />
    public override void Write(Utf8YamlWriter writer, char value, YamlSerializerOptions options) =>
        writer.WriteString(value.ToString());
}

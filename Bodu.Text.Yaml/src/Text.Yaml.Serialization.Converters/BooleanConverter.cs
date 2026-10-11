// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BooleanConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="bool" /> to and from a YAML boolean scalar. A null scalar reads as the type default.
/// </summary>
/// <remarks>
/// Only a boolean scalar binds: a string, number, sequence, or mapping is reported as a binding failure, which the
/// binder places at the value under its path, rather than read through the reader's typed accessor, whose
/// <see cref="InvalidOperationException" /> would escape the binding.
/// </remarks>
internal sealed class BooleanConverter
    : YamlConverter<bool>
{
    /// <inheritdoc />
    /// <exception cref="YamlSerializationException">
    /// The value is a scalar other than a boolean or null, or is not a scalar.
    /// </exception>
    public override bool Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options) =>
        reader.TokenType switch
        {
            YamlTokenType.Boolean => reader.GetBoolean(),
            YamlTokenType.Null => false,
            YamlTokenType.String or YamlTokenType.Integer or YamlTokenType.Float => throw new YamlSerializationException(
                string.Format(CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlValueConversion, typeof(bool))),
            _ => throw new YamlSerializationException(YamlResourceStrings.Op_Invalid_YamlExpectedScalar),
        };

    /// <inheritdoc />
    public override void Write(Utf8YamlWriter writer, bool value, YamlSerializerOptions options) =>
        writer.WriteBoolean(value);
}

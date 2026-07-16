// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DoubleConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="double" /> to and from a YAML float scalar. Reading accepts any numeric or string scalar and
/// parses its invariant text; a null scalar reads as the type default.
/// </summary>
internal sealed class DoubleConverter
    : YamlConverter<double>
{
    /// <inheritdoc />
    public override double Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options) =>
        reader.TokenType == YamlTokenType.Null
            ? default
            : double.Parse(ScalarCoercion.ReaderScalarText(ref reader), NumberStyles.Float, CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public override void Write(Utf8YamlWriter writer, double value, YamlSerializerOptions options) =>
        writer.WriteDouble(value);
}

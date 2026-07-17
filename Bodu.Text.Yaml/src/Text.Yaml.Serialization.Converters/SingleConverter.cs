// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SingleConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="float" /> to and from a YAML float scalar, widening exactly to a double on write. Reading
/// accepts any numeric or string scalar and parses its invariant text; a null scalar reads as the type default.
/// </summary>
internal sealed class SingleConverter
    : YamlConverter<float>
{
    /// <inheritdoc />
    public override float Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options) =>
        reader.TokenType == YamlTokenType.Null
            ? default
            : float.Parse(ScalarCoercion.ReaderScalarText(ref reader), NumberStyles.Float, CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public override void Write(Utf8YamlWriter writer, float value, YamlSerializerOptions options) =>
        writer.WriteDouble(value);
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DecimalConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="decimal" /> to and from a YAML scalar. A decimal carries more precision than a double, so it
/// writes as its exact invariant text (quoted, because that text resolves as a float) and round-trips without precision
/// loss; a null scalar reads as the type default.
/// </summary>
internal sealed class DecimalConverter
    : YamlConverter<decimal>
{
    /// <inheritdoc />
    public override decimal Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options) =>
        reader.TokenType == YamlTokenType.Null
            ? default
            : decimal.Parse(ScalarCoercion.ReaderScalarText(ref reader), NumberStyles.Float, CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public override void Write(Utf8YamlWriter writer, decimal value, YamlSerializerOptions options) =>
        writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
}

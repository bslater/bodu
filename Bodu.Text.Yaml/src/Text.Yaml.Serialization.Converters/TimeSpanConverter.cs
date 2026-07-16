// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TimeSpanConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="TimeSpan" /> to and from its invariant constant-format YAML string form. A null scalar reads
/// as the type default.
/// </summary>
internal sealed class TimeSpanConverter
    : YamlConverter<TimeSpan>
{
    /// <inheritdoc />
    public override TimeSpan Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options) =>
        reader.TokenType == YamlTokenType.Null
            ? default
            : TimeSpan.Parse(ScalarCoercion.ReaderScalarText(ref reader), CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public override void Write(Utf8YamlWriter writer, TimeSpan value, YamlSerializerOptions options) =>
        writer.WriteString(value.ToString());
}

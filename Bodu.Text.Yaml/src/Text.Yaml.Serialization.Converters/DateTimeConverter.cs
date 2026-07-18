// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="DateTime" /> to and from its ISO 8601 round-trip (<c>"o"</c>) YAML string form, preserving the
/// value's <see cref="DateTimeKind" />. A null scalar reads as the type default.
/// </summary>
internal sealed class DateTimeConverter
    : YamlConverter<DateTime>
{
    /// <inheritdoc />
    public override DateTime Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options) =>
        reader.TokenType == YamlTokenType.Null
            ? default
            : DateTime.Parse(ScalarCoercion.ReaderScalarText(ref reader), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    /// <inheritdoc />
    public override void Write(Utf8YamlWriter writer, DateTime value, YamlSerializerOptions options) =>
        writer.WriteString(value.ToString("o", CultureInfo.InvariantCulture));
}

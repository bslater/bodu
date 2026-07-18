// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GuidConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="Guid" /> to and from its hyphenated YAML string form. A null scalar reads as the type default.
/// </summary>
internal sealed class GuidConverter
    : YamlConverter<Guid>
{
    /// <inheritdoc />
    public override Guid Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options) =>
        reader.TokenType == YamlTokenType.Null
            ? default
            : Guid.Parse(ScalarCoercion.ReaderScalarText(ref reader));

    /// <inheritdoc />
    public override void Write(Utf8YamlWriter writer, Guid value, YamlSerializerOptions options) =>
        writer.WriteString(value.ToString());
}

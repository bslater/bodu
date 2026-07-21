// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="string" /> to and from a YAML scalar. Reading accepts any scalar kind — YAML's implicit typing
/// may have resolved the source text to an integer, float, or boolean — and re-reads it as its invariant text; a null
/// scalar reads as <see langword="null" />.
/// </summary>
internal sealed class StringConverter
    : YamlConverter<string>
{
    /// <inheritdoc />
    public override string Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options) =>
        reader.TokenType == YamlTokenType.Null ? null! : ScalarCoercion.ReaderScalarText(ref reader);

    /// <inheritdoc />
    public override void Write(Utf8YamlWriter writer, string value, YamlSerializerOptions options) =>
        writer.WriteString(value);
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BooleanConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="bool" /> to and from a YAML boolean scalar. A null scalar reads as the type default.
/// </summary>
internal sealed class BooleanConverter
    : YamlConverter<bool>
{
    /// <inheritdoc />
    public override bool Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options) =>
        reader.TokenType != YamlTokenType.Null && reader.GetBoolean();

    /// <inheritdoc />
    public override void Write(Utf8YamlWriter writer, bool value, YamlSerializerOptions options) =>
        writer.WriteBoolean(value);
}

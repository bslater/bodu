// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlElementConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Document;
using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="YamlElement" /> to and from YAML, bridging the read-only document object model to the
/// serializer.
/// </summary>
/// <remarks>
/// On read the value's subtree is viewed through an internal <see cref="YamlDocument" /> that backs the returned
/// element. That document shares the reader's immutable row store and is never disposed; it is reclaimed by garbage
/// collection together with the element, so the element never requires disposal.
/// </remarks>
internal sealed class YamlElementConverter
    : YamlConverter<YamlElement>
{
    /// <inheritdoc />
    public override YamlElement Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options) =>
        YamlDocument.ParseValue(ref reader).RootElement;

    /// <inheritdoc />
    public override void Write(Utf8YamlWriter writer, YamlElement value, YamlSerializerOptions options) =>
        value.WriteTo(writer);
}

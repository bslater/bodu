// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Document;
using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="YamlDocument" /> to and from YAML, bridging the read-only document object model to the
/// serializer.
/// </summary>
/// <remarks>
/// A document produced by deserialization shares the reader's immutable row store and holds no pooled resources, so
/// disposal is optional. Writing a disposed document surfaces the document's own
/// <see cref="ObjectDisposedException" />.
/// </remarks>
internal sealed class YamlDocumentConverter
    : YamlConverter<YamlDocument>
{
    /// <inheritdoc />
    public override YamlDocument Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options) =>
        YamlDocument.ParseValue(ref reader);

    /// <inheritdoc />
    public override void Write(Utf8YamlWriter writer, YamlDocument value, YamlSerializerOptions options)
    {
        if (value is null)
            throw new YamlSerializationException(YamlResourceStrings.Op_NotSupported_NullDocument);

        value.RootElement.WriteTo(writer);
    }
}

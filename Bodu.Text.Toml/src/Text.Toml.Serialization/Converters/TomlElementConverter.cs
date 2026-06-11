// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlElementConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Document;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="TomlElement" /> to and from TOML, bridging the read-only document object model to the
/// serializer.
/// </summary>
/// <remarks>
/// On read the value's subtree is parsed into an internal <see cref="TomlDocument" /> that backs the returned element.
/// That document is never disposed; it is reclaimed by garbage collection together with the element, so the element
/// never requires disposal. Were <see cref="TomlDocument" /> ever to move to pooled storage, this converter would need
/// to clone into unpooled memory instead.
/// </remarks>
internal sealed class TomlElementConverter
    : TomlConverter<TomlElement>
{
    /// <inheritdoc />
    public override TomlElement Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options) =>
        TomlDocument.ParseValue(ref reader).RootElement;

    /// <inheritdoc />
    public override void Write(Utf8TomlWriter writer, TomlElement value, TomlSerializerOptions options) =>
        value.WriteTo(writer);
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlDocumentConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Document;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="TomlDocument" /> to and from TOML, bridging the read-only document object model to the
/// serializer.
/// </summary>
/// <remarks>
/// A document produced by deserialization is owned by the caller, who is responsible for disposing it. Writing a
/// disposed document surfaces the document's own <see cref="ObjectDisposedException" />.
/// </remarks>
internal sealed class TomlDocumentConverter
    : TomlConverter<TomlDocument>
{
    /// <inheritdoc />
    public override TomlDocument Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options) =>
        TomlDocument.ParseValue(ref reader);

    /// <inheritdoc />
    public override void Write(Utf8TomlWriter writer, TomlDocument value, TomlSerializerOptions options)
    {
        if (value is null)
            throw new TomlSerializationException(TomlResourceStrings.Op_NotSupported_NullDocument);

        value.RootElement.WriteTo(writer);
    }
}

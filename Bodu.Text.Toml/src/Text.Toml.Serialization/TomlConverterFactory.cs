// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlConverterFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Serialization;

/// <summary>
/// Produces a <see cref="TomlConverter" /> for a family of types that cannot be served by a single
/// <see cref="TomlConverter{T}" /> — for example every <see cref="Nullable{T}" />, every enumeration, or every
/// collection. Mirrors <see cref="System.Text.Json.Serialization.JsonConverterFactory" />.
/// </summary>
/// <remarks>
/// The serializer calls <see cref="TomlConverter.CanConvert(Type)" /> to decide whether the factory applies, then
/// <see cref="CreateConverter" /> to obtain the converter for the specific closed type being serialized. A factory is
/// never asked to read or write a value itself.
/// </remarks>
public abstract class TomlConverterFactory
    : TomlConverter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlConverterFactory" /> class.
    /// </summary>
    protected TomlConverterFactory()
    {
    }

    /// <summary>
    /// Creates a converter for the specified type.
    /// </summary>
    /// <param name="typeToConvert">The closed type to create a converter for.</param>
    /// <param name="options">The serializer options in effect.</param>
    /// <returns>A converter that handles <paramref name="typeToConvert" />.</returns>
    public abstract TomlConverter CreateConverter(Type typeToConvert, TomlSerializerOptions options);

    /// <inheritdoc />
    internal sealed override object? ReadAsObject(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options) =>
        throw new InvalidOperationException();

    /// <inheritdoc />
    internal sealed override void WriteAsObject(Utf8TomlWriter writer, object? value, TomlSerializerOptions options) =>
        throw new InvalidOperationException();
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FormatConverterFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Produces a <see cref="FormatConverter" /> for a family of types that cannot be served by a single
/// <see cref="FormatConverter{T}" /> — for example every <see cref="Nullable{T}" />, every enumeration, or every
/// collection. Mirrors <see cref="System.Text.Json.Serialization.JsonConverterFactory" />.
/// </summary>
/// <remarks>
/// The serializer calls <see cref="FormatConverter.CanConvert(Type)" /> to decide whether the factory applies, then
/// <see cref="CreateConverter" /> to obtain the converter for the specific closed type being serialized. A factory is
/// never asked to read or write a value itself.
/// </remarks>
public abstract class FormatConverterFactory
    : FormatConverter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormatConverterFactory" /> class.
    /// </summary>
    protected FormatConverterFactory()
    {
    }

    /// <summary>
    /// Creates a converter for the specified type.
    /// </summary>
    /// <param name="typeToConvert">The closed type to create a converter for.</param>
    /// <param name="options">The serializer options in effect.</param>
    /// <returns>A converter that handles <paramref name="typeToConvert" />.</returns>
    public abstract FormatConverter CreateConverter(Type typeToConvert, FormatSerializerOptions options);

    /// <inheritdoc />
    internal sealed override object? ReadAsObject(ISerializationReader reader, Type typeToConvert, FormatSerializerOptions options) =>
        throw new InvalidOperationException();

    /// <inheritdoc />
    internal sealed override void WriteAsObject(ISerializationWriter writer, object? value, FormatSerializerOptions options) =>
        throw new InvalidOperationException();
}

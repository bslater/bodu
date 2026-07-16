// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlConverterFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Serialization;

/// <summary>
/// Produces a <see cref="YamlConverter" /> for a family of types that cannot be served by a single
/// <see cref="YamlConverter{T}" /> — for example every <see cref="Nullable{T}" />, every enumeration, or every
/// collection.
/// </summary>
/// <remarks>
/// The serializer calls <see cref="YamlConverter.CanConvert(Type)" /> to decide whether the factory applies, then
/// <see cref="CreateConverter" /> to obtain the converter for the specific closed type being serialized. A factory is
/// never asked to read or write a value itself.
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// public sealed class StackConverterFactory : YamlConverterFactory
/// {
///     public override bool CanConvert(Type typeToConvert) =>
///         typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Stack<>);
///
///     public override YamlConverter CreateConverter(Type typeToConvert, YamlSerializerOptions options) =>
///         (YamlConverter)Activator.CreateInstance(
///             typeof(StackConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]))!;
/// }
///]]>
/// </code>
/// </example>
public abstract class YamlConverterFactory
    : YamlConverter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="YamlConverterFactory" /> class.
    /// </summary>
    protected YamlConverterFactory()
    {
    }

    /// <summary>
    /// Creates a converter for the specified type.
    /// </summary>
    /// <param name="typeToConvert">The closed type to create a converter for.</param>
    /// <param name="options">The serializer options in effect.</param>
    /// <returns>A converter that handles <paramref name="typeToConvert" />.</returns>
    public abstract YamlConverter CreateConverter(Type typeToConvert, YamlSerializerOptions options);

    /// <inheritdoc />
    internal sealed override object? ReadAsObject(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options) =>
        throw new InvalidOperationException();

    /// <inheritdoc />
    internal sealed override void WriteAsObject(Utf8YamlWriter writer, object? value, YamlSerializerOptions options) =>
        throw new InvalidOperationException();
}

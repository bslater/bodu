// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeConverterFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Reader;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode.Serialization;

/// <summary>
/// Produces a <see cref="BencodeConverter" /> for a family of types that cannot be served by a single
/// <see cref="BencodeConverter{T}" /> — for example every <see cref="Nullable{T}" />, every enumeration, or every
/// collection.
/// </summary>
/// <remarks>
/// The serializer calls <see cref="BencodeConverter.CanConvert(Type)" /> to decide whether the factory applies, then
/// <see cref="CreateConverter" /> to obtain the converter for the specific closed type being serialized. A factory is
/// never asked to read or write a value itself.
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// public sealed class StackConverterFactory : BencodeConverterFactory
/// {
///     public override bool CanConvert(Type typeToConvert) =>
///         typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Stack<>);
///
///     public override BencodeConverter CreateConverter(Type typeToConvert, BencodeSerializerOptions options) =>
///         (BencodeConverter)Activator.CreateInstance(
///             typeof(StackConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]))!;
/// }
///]]>
/// </code>
/// </example>
public abstract class BencodeConverterFactory
    : BencodeConverter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeConverterFactory" /> class.
    /// </summary>
    protected BencodeConverterFactory()
    {
    }

    /// <summary>
    /// Creates a converter for the specified type.
    /// </summary>
    /// <param name="typeToConvert">The closed type to create a converter for.</param>
    /// <param name="options">The serializer options in effect.</param>
    /// <returns>A converter that handles <paramref name="typeToConvert" />.</returns>
    public abstract BencodeConverter CreateConverter(Type typeToConvert, BencodeSerializerOptions options);

    /// <inheritdoc />
    internal sealed override object? ReadAsObject(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options) =>
        throw new InvalidOperationException();

    /// <inheritdoc />
    internal sealed override void WriteAsObject(Utf8BencodeWriter writer, object? value, BencodeSerializerOptions options) =>
        throw new InvalidOperationException();
}

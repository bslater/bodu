// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlConverterOfT.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Serialization;

/// <summary>
/// Converts a value of type <typeparamref name="T" /> to and from the TOML reader and writer. This is the base authors
/// derive to customize how a specific type is serialized.
/// </summary>
/// <typeparam name="T">The type the converter handles.</typeparam>
/// <example>
///<![CDATA[
/// public sealed class BooleanConverter : TomlConverter<bool>
/// {
///     public override bool Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options) =>
///         reader.GetInt64() != 0;
///
///     public override void Write(Utf8TomlWriter writer, bool value, TomlSerializerOptions options) =>
///         writer.WriteInteger(value ? 1 : 0);
/// }
///]]>
/// </example>
public abstract class TomlConverter<T>
    : TomlConverter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlConverter{T}" /> class.
    /// </summary>
    protected TomlConverter()
    {
    }

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert == typeof(T);

    /// <summary>
    /// Reads and converts a value of type <typeparamref name="T" /> from the reader.
    /// </summary>
    /// <param name="reader">The reader positioned on the first token of the value.</param>
    /// <param name="typeToConvert">The requested type.</param>
    /// <param name="options">The serializer options in effect.</param>
    /// <returns>The deserialized value.</returns>
    /// <remarks>
    /// On entry the reader is positioned on the value's first token. On return it must be positioned on the value's
    /// last token, so the caller can advance past the value with a single <see cref="TomlDocumentReader.Read" />.
    /// </remarks>
    public abstract T Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options);

    /// <summary>
    /// Writes a value of type <typeparamref name="T" /> to the writer.
    /// </summary>
    /// <param name="writer">The destination writer.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="options">The serializer options in effect.</param>
    public abstract void Write(Utf8TomlWriter writer, T value, TomlSerializerOptions options);

    /// <inheritdoc />
    internal sealed override object? ReadAsObject(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options) =>
        Read(ref reader, typeToConvert, options);

    /// <inheritdoc />
    internal sealed override void WriteAsObject(Utf8TomlWriter writer, object? value, TomlSerializerOptions options) =>
        Write(writer, (T)value!, options);
}

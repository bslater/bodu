// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FormatConverterOfT.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Converts a value of type <typeparamref name="T" /> to and from the format-neutral reader and writer. This is the
/// base authors derive to customize how a specific type is serialized, mirroring
/// <see cref="System.Text.Json.Serialization.JsonConverter{T}" />.
/// </summary>
/// <typeparam name="T">The type the converter handles.</typeparam>
/// <example>
///<![CDATA[
/// public sealed class PointConverter : FormatConverter<Point>
/// {
///     public override Point Read(ISerializationReader reader, Type typeToConvert, FormatSerializerOptions options)
///     {
///         string text = reader.GetString();
///         string[] parts = text.Split(',');
///         return new Point(int.Parse(parts[0]), int.Parse(parts[1]));
///     }
///
///     public override void Write(ISerializationWriter writer, Point value, FormatSerializerOptions options) =>
///         writer.WriteString($"{value.X},{value.Y}");
/// }
///]]>
/// </example>
public abstract class FormatConverter<T>
    : FormatConverter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormatConverter{T}" /> class.
    /// </summary>
    protected FormatConverter()
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
    /// last token, so the caller can advance past the value with a single <see cref="ISerializationReader.Read" />.
    /// </remarks>
    public abstract T Read(ISerializationReader reader, Type typeToConvert, FormatSerializerOptions options);

    /// <summary>
    /// Writes a value of type <typeparamref name="T" /> to the writer.
    /// </summary>
    /// <param name="writer">The destination writer.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="options">The serializer options in effect.</param>
    public abstract void Write(ISerializationWriter writer, T value, FormatSerializerOptions options);

    /// <inheritdoc />
    internal sealed override object? ReadAsObject(ISerializationReader reader, Type typeToConvert, FormatSerializerOptions options) =>
        Read(reader, typeToConvert, options);

    /// <inheritdoc />
    internal sealed override void WriteAsObject(ISerializationWriter writer, object? value, FormatSerializerOptions options) =>
        Write(writer, (T)value!, options);
}

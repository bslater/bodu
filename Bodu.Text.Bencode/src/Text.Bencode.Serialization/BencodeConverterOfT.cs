// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeConverterOfT.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Reader;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode.Serialization;

/// <summary>
/// Converts a value of type <typeparamref name="T" /> to and from the Bencode reader and writer. This is the base
/// authors derive to customize how a specific type is serialized.
/// </summary>
/// <typeparam name="T">The type the converter handles.</typeparam>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// public sealed class VersionConverter : BencodeConverter<Version>
/// {
///     public override Version Read(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options) =>
///         Version.Parse(reader.GetString());
///
///     public override void Write(Utf8BencodeWriter writer, Version value, BencodeSerializerOptions options) =>
///         writer.WriteString(value.ToString());
/// }
///]]>
/// </code>
/// </example>
public abstract class BencodeConverter<T>
    : BencodeConverter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeConverter{T}" /> class.
    /// </summary>
    protected BencodeConverter()
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
    /// last token, so the caller can advance past the value with a single <see cref="Utf8BencodeReader.Read" />.
    /// </remarks>
    public abstract T Read(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options);

    /// <summary>
    /// Writes a value of type <typeparamref name="T" /> to the writer.
    /// </summary>
    /// <param name="writer">The destination writer.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="options">The serializer options in effect.</param>
    public abstract void Write(Utf8BencodeWriter writer, T value, BencodeSerializerOptions options);

    /// <inheritdoc />
    internal sealed override object? ReadAsObject(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options) =>
        Read(ref reader, typeToConvert, options);

    /// <inheritdoc />
    internal sealed override void WriteAsObject(Utf8BencodeWriter writer, object? value, BencodeSerializerOptions options)
    {
        // Once a failure has been recorded, stop dispatching so the recursion unwinds through normal returns rather
        // than entering another converter on an exhausted stack.
        if (writer.WriteStack is { HasFailure: true })
            return;

        Write(writer, (T)value!, options);
    }
}

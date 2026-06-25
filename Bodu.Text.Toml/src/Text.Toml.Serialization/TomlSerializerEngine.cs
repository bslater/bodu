// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerEngine.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Serialization;

/// <summary>
/// Drives serialization and deserialization between a strongly-typed value and the TOML reader and writer, resolving
/// the root converter from the options and delegating to it.
/// </summary>
/// <remarks>
/// This type is the bridge the <see cref="TomlSerializer" /> facade calls once it has built a reader over the source
/// bytes or a writer over its output.
/// </remarks>
internal static class TomlSerializerEngine
{
    /// <summary>
    /// Serializes a value of type <typeparamref name="T" /> to the writer.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="writer">The destination writer.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    internal static void Serialize<T>(Utf8TomlWriter writer, T value, TomlSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(options);

        options.MakeReadOnly();

        // Attach the serializer write state so the converters record an over-deep graph or a reference cycle
        // cooperatively and unwind through returns; the single recorded failure is thrown once here, at the shallow
        // root boundary, rather than from the deepest recursive frame.
        var state = new TomlWriteStack();
        writer.AttachWriteStack(state);

        TomlConverter converter = options.GetConverter(typeof(T));
        converter.WriteAsObject(writer, value, options);

        state.ThrowIfFailed();
    }

    /// <summary>
    /// Deserializes a value of type <typeparamref name="T" /> from the reader.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    /// <param name="reader">The reader over the source bytes.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="TomlSerializationException">
    /// Thrown when the document cannot be bound to <typeparamref name="T" />.
    /// </exception>
    internal static T Deserialize<T>(ref TomlDocumentReader reader, TomlSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(options);

        options.MakeReadOnly();
        TomlConverter converter = options.GetConverter(typeof(T));

        if (!reader.Read())
            throw new TomlSerializationException(string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_UnexpectedEndOfInput, typeof(T)));

        return (T)converter.ReadAsObject(ref reader, typeof(T), options) !;
    }
}

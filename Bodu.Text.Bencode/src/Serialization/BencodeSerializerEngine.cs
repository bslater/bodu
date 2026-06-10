// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerEngine.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Text.Bencode.Serialization;

/// <summary>
/// Drives serialization and deserialization between a strongly-typed value and the Bencode reader and writer, resolving
/// the root converter from the options and delegating to it.
/// </summary>
/// <remarks>
/// This type is the bridge the <see cref="BencodeSerializer" /> facade calls once it has built a reader over the source
/// bytes or a writer over its output.
/// </remarks>
internal static class BencodeSerializerEngine
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
    internal static void Serialize<T>(Utf8BencodeWriter writer, T value, BencodeSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(options);

        options.MakeReadOnly();
        BencodeConverter converter = options.GetConverter(typeof(T));
        converter.WriteAsObject(writer, value, options);
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
    /// <exception cref="BencodeSerializationException">
    /// Thrown when the document cannot be bound to <typeparamref name="T" />.
    /// </exception>
    internal static T Deserialize<T>(ref Utf8BencodeReader reader, BencodeSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(options);

        options.MakeReadOnly();
        BencodeConverter converter = options.GetConverter(typeof(T));

        if (!reader.Read())
            throw new BencodeSerializationException(string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_UnexpectedEndOfInput, typeof(T)));

        return (T)converter.ReadAsObject(ref reader, typeof(T), options) !;
    }
}

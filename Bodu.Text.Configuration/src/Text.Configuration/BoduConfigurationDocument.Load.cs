// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationDocument.Load.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO;
using System.Text;

namespace Bodu.Text.Configuration;

public sealed partial class BoduConfigurationDocument
{
    /// <summary>
    /// Loads a configuration document from the file at <paramref name="path" /> using the default Bodu
    /// parse options and UTF-8 encoding with BOM detection.
    /// </summary>
    /// <param name="path">The file path to load.</param>
    /// <returns>A populated <see cref="BoduConfigurationDocument" />.</returns>
    /// <exception cref="ArgumentException"><paramref name="path" /> is <see langword="null" />, empty, or
    /// whitespace.</exception>
    /// <exception cref="BoduConfigurationParseException">The file could not be parsed.</exception>
    public static BoduConfigurationDocument Load(string path) =>
        Load(path, options: null);

    /// <summary>
    /// Loads a configuration document from the file at <paramref name="path" />.
    /// </summary>
    /// <param name="path">The file path to load.</param>
    /// <param name="options">The parse options to apply, or <see langword="null" /> for the defaults.</param>
    /// <returns>A populated <see cref="BoduConfigurationDocument" />.</returns>
    /// <exception cref="ArgumentException"><paramref name="path" /> is <see langword="null" />, empty, or
    /// whitespace.</exception>
    /// <exception cref="BoduConfigurationParseException">The file could not be parsed.</exception>
    public static BoduConfigurationDocument Load(string path, BoduConfigurationParseOptions? options)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(path);

        BoduConfigurationParseOptions effective = options ?? BoduConfigurationParseOptions.Bodu;

        using FileStream stream = File.OpenRead(path);
        using StreamReader reader = new(stream, effective.DefaultEncoding, detectEncodingFromByteOrderMarks: true);
        return new BoduConfigurationReader(effective).Read(reader, path);
    }

    /// <summary>
    /// Loads a configuration document from the supplied stream.
    /// </summary>
    /// <param name="stream">The stream to read from. Must support reading.</param>
    /// <param name="options">The parse options to apply, or <see langword="null" /> for the defaults.</param>
    /// <param name="encoding">The encoding to use when the stream lacks a byte order mark, or
    /// <see langword="null" /> to use the parse options' default encoding.</param>
    /// <param name="leaveOpen">When <see langword="true" />, the stream remains open after parsing.</param>
    /// <returns>A populated <see cref="BoduConfigurationDocument" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="stream" /> does not support reading.</exception>
    /// <exception cref="BoduConfigurationParseException">The stream could not be parsed.</exception>
    public static BoduConfigurationDocument Load(
        Stream stream,
        BoduConfigurationParseOptions? options = null,
        Encoding? encoding = null,
        bool leaveOpen = false)
    {
        ThrowHelper.ThrowIfNull(stream);
        if (!stream.CanRead)
            throw new ArgumentException("Stream does not support reading.", nameof(stream));

        BoduConfigurationParseOptions effective = options ?? BoduConfigurationParseOptions.Bodu;
        Encoding effectiveEncoding = encoding ?? effective.DefaultEncoding;

        using StreamReader reader = new(stream, effectiveEncoding, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: leaveOpen);
        return new BoduConfigurationReader(effective).Read(reader, path: null);
    }

    /// <summary>
    /// Loads a configuration document from the supplied text reader.
    /// </summary>
    /// <param name="reader">The reader to consume.</param>
    /// <param name="options">The parse options to apply, or <see langword="null" /> for the defaults.</param>
    /// <returns>A populated <see cref="BoduConfigurationDocument" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="reader" /> is <see langword="null" />.</exception>
    /// <exception cref="BoduConfigurationParseException">The text could not be parsed.</exception>
    public static BoduConfigurationDocument Load(TextReader reader, BoduConfigurationParseOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(reader);
        return new BoduConfigurationReader(options ?? BoduConfigurationParseOptions.Bodu).Read(reader, path: null);
    }
}

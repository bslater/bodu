// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Toml.Stream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Text.Formats;

namespace Bodu.Text.Toml;

public static partial class Toml
{
    /// <summary>
    /// The UTF-8 encoding used to read and write TOML streams, configured to emit no byte-order mark.
    /// </summary>
    private static readonly UTF8Encoding s_utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

    /// <summary>
    /// Parses a TOML document by reading <paramref name="source" /> to its end as UTF-8 text.
    /// </summary>
    /// <param name="source">The readable stream containing the UTF-8 TOML bytes.</param>
    /// <returns>The root <see cref="TomlTable" /> of the parsed document.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source" /> does not support reading.</exception>
    /// <exception cref="TomlFormatException">Thrown when the stream contents are not a valid TOML document.</exception>
    /// <remarks>
    /// The stream is read to its end, decoded as UTF-8 (a leading byte-order mark is ignored), and parsed. The stream
    /// is not closed.
    /// </remarks>
    public static TomlTable Parse(Stream source)
    {
        ThrowHelper.ThrowIfNull(source);
        TextThrowHelper.ThrowIfStreamNotReadable(source);

        using MemoryStream buffer = new();
        source.CopyTo(buffer);
        return Parse(Decode(buffer));
    }

    /// <summary>
    /// Asynchronously parses a TOML document by reading <paramref name="source" /> to its end as UTF-8 text.
    /// </summary>
    /// <param name="source">The readable stream containing the UTF-8 TOML bytes.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the read.</param>
    /// <returns>A task that completes with the root <see cref="TomlTable" /> of the parsed document.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source" /> does not support reading.</exception>
    /// <exception cref="TomlFormatException">Thrown when the stream contents are not a valid TOML document.</exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="cancellationToken" /> is signalled before the read completes.
    /// </exception>
    /// <remarks>
    /// The stream is read to its end, decoded as UTF-8 (a leading byte-order mark is ignored), and parsed. The stream
    /// is not closed.
    /// </remarks>
    public static async ValueTask<TomlTable> ParseAsync(Stream source, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(source);
        TextThrowHelper.ThrowIfStreamNotReadable(source);

        await using MemoryStream buffer = new();
        await source.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return Parse(Decode(buffer));
    }

    /// <summary>
    /// Formats <paramref name="document" /> and writes the result to <paramref name="destination" /> as UTF-8 text.
    /// </summary>
    /// <param name="document">The document to format.</param>
    /// <param name="destination">The writable stream that receives the UTF-8 bytes.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="document" /> or <paramref name="destination" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> does not support writing.
    /// </exception>
    /// <remarks>
    /// The encoded text carries no byte-order mark. The stream is not closed.
    /// </remarks>
    public static void Format(TomlTable document, Stream destination)
    {
        ThrowHelper.ThrowIfNull(document);
        ThrowHelper.ThrowIfNull(destination);
        TextThrowHelper.ThrowIfStreamNotWritable(destination);

        var bytes = s_utf8.GetBytes(Format(document));
        destination.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Asynchronously formats <paramref name="document" /> and writes the result to <paramref name="destination" /> as
    /// UTF-8 text.
    /// </summary>
    /// <param name="document">The document to format.</param>
    /// <param name="destination">The writable stream that receives the UTF-8 bytes.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the write.</param>
    /// <returns>A task that completes once the encoded bytes have been written.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="document" /> or <paramref name="destination" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> does not support writing.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="cancellationToken" /> is signalled before the write completes.
    /// </exception>
    /// <remarks>
    /// The encoded text carries no byte-order mark. The stream is not closed.
    /// </remarks>
    public static async ValueTask FormatAsync(TomlTable document, Stream destination, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(document);
        ThrowHelper.ThrowIfNull(destination);
        TextThrowHelper.ThrowIfStreamNotWritable(destination);

        var bytes = s_utf8.GetBytes(Format(document));
        await destination.WriteAsync(bytes.AsMemory(), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Decodes the contents of <paramref name="buffer" /> as UTF-8 text, ignoring a leading byte-order mark.
    /// </summary>
    /// <param name="buffer">The buffered bytes.</param>
    /// <returns>The decoded text.</returns>
    private static string Decode(MemoryStream buffer)
    {
        ReadOnlySpan<byte> bytes = buffer.GetBuffer().AsSpan(0, (int)buffer.Length);
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            bytes = bytes[3..];

        return s_utf8.GetString(bytes);
    }
}

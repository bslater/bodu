// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamExtensions.WriteAllBytes.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class StreamExtensions
{
    /// <summary>
    /// Writes the entire contents of <paramref name="bytes" /> to <paramref name="stream" /> at its current position.
    /// </summary>
    /// <param name="stream">The stream to write to. Must not be <see langword="null" />.</param>
    /// <param name="bytes">The bytes to write.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="stream" /> does not support writing.
    /// </exception>
    /// <remarks>
    /// The write begins at the current position; the stream is neither flushed nor closed by this method.
    /// </remarks>
    public static void WriteAllBytes(this Stream stream, ReadOnlySpan<byte> bytes)
    {
        ThrowHelper.ThrowIfNull(stream);

        stream.Write(bytes);
    }
}

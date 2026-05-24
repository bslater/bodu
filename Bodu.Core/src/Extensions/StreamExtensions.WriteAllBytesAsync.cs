// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamExtensions.WriteAllBytesAsync.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class StreamExtensions
{
    /// <summary>
    /// Asynchronously writes the entire contents of <paramref name="bytes" /> to <paramref name="stream" /> at its
    /// current position.
    /// </summary>
    /// <param name="stream">The stream to write to. Must not be <see langword="null" />.</param>
    /// <param name="bytes">The bytes to write.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>A task that completes when the write has finished.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Argument validation runs synchronously, so a <see langword="null" /> <paramref name="stream" /> faults at the
    /// call site rather than on the returned task. The stream is neither flushed nor closed by this method.
    /// </remarks>
    public static Task WriteAllBytesAsync(this Stream stream, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(stream);

        return stream.WriteAsync(bytes, cancellationToken).AsTask();
    }
}

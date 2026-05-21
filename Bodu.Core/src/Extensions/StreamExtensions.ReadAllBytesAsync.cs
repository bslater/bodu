// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamExtensions.ReadAllBytesAsync.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class StreamExtensions
{
    /// <summary>
    /// Asynchronously reads <paramref name="stream" /> from its current position to the end and returns the bytes as an
    /// array.
    /// </summary>
    /// <param name="stream">The stream to read. Must not be <see langword="null" />.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>
    /// A task that completes with a new array containing every byte from the current position to the end of
    /// <paramref name="stream" />, or an empty array when no bytes remain.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Argument validation runs synchronously, so a <see langword="null" /> <paramref name="stream" /> faults at the
    /// call site rather than on the returned task. See <see cref="ReadAllBytes(Stream)" /> for the buffering strategy.
    /// </remarks>
    public static Task<byte[]> ReadAllBytesAsync(this Stream stream, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(stream);

        return ReadAllBytesCoreAsync(stream, cancellationToken);

        static async Task<byte[]> ReadAllBytesCoreAsync(Stream source, CancellationToken token)
        {
            if (source.CanSeek)
            {
                long remaining = source.Length - source.Position;
                if (remaining <= 0L)
                    return Array.Empty<byte>();

                if (remaining <= int.MaxValue)
                {
                    int count = (int)remaining;
                    byte[] buffer = new byte[count];
                    int offset = 0;

                    int read;
                    while (offset < count &&
                           (read = await source.ReadAsync(buffer.AsMemory(offset, count - offset), token).ConfigureAwait(false)) > 0)
                        offset += read;

                    if (offset != count)
                        Array.Resize(ref buffer, offset);

                    return buffer;
                }
            }

            using MemoryStream memory = new();
            await source.CopyToAsync(memory, token).ConfigureAwait(false);
            return memory.ToArray();
        }
    }
}

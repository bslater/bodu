// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamExtensions.ReadAllBytes.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class StreamExtensions
{
    /// <summary>
    /// Reads <paramref name="stream" /> from its current position to the end and returns the bytes as an array.
    /// </summary>
    /// <param name="stream">The stream to read. Must not be <see langword="null" />.</param>
    /// <returns>
    /// A new array containing every byte from the current position to the end of <paramref name="stream" />, or an
    /// empty array when no bytes remain.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="stream" /> does not support reading.
    /// </exception>
    /// <remarks>
    /// When <paramref name="stream" /> is seekable a single buffer sized to the remaining length is allocated;
    /// otherwise the content is accumulated through an intermediate <see cref="MemoryStream" />. The read begins at the
    /// current position and the stream is left open.
    /// </remarks>
    public static byte[] ReadAllBytes(this Stream stream)
    {
        ThrowHelper.ThrowIfNull(stream);

        if (stream.CanSeek)
        {
            var remaining = stream.Length - stream.Position;
            if (remaining <= 0L)
                return Array.Empty<byte>();

            if (remaining <= int.MaxValue)
            {
                var count = (int)remaining;
                var buffer = new byte[count];
                var offset = 0;

                int read;
                while (offset < count && (read = stream.Read(buffer, offset, count - offset)) > 0)
                    offset += read;

                if (offset != count)
                    Array.Resize(ref buffer, offset);

                return buffer;
            }
        }

        using MemoryStream memory = new();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}

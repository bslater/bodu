// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundDataSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// Provides random access to the raw bytes of a compound file, abstracting whether the content is held in memory or
/// read on demand from a seekable stream.
/// </summary>
/// <remarks>
/// The in-memory implementation returns spans directly over its backing array (zero-copy); the stream implementation
/// reads the requested range into a caller-supplied buffer. This lets the sector reader serve both a buffered file and
/// a bounded-memory streaming file through one interface.
/// </remarks>
internal abstract class CompoundDataSource
    : IDisposable
{
    /// <summary>
    /// Gets the total length, in bytes, of the underlying content.
    /// </summary>
    /// <returns>The content length.</returns>
    public abstract long Length { get; }

    /// <summary>
    /// Returns a read-only span over <paramref name="length" /> bytes starting at <paramref name="offset" />.
    /// </summary>
    /// <param name="offset">The absolute byte offset.</param>
    /// <param name="length">The number of bytes to expose.</param>
    /// <param name="scratch">
    /// A caller-owned buffer of at least <paramref name="length" /> bytes; a stream source reads into it and returns a
    /// span over it, while an in-memory source ignores it and returns a span over its own storage.
    /// </param>
    /// <returns>A span over the requested bytes.</returns>
    public abstract ReadOnlySpan<byte> GetSpan(long offset, int length, Span<byte> scratch);

    /// <summary>
    /// Reads exactly <c>destination.Length</c> bytes starting at <paramref name="offset" /> into the destination.
    /// </summary>
    /// <param name="offset">The absolute byte offset.</param>
    /// <param name="destination">The buffer that receives the bytes.</param>
    public abstract void Read(long offset, Span<byte> destination);

    /// <inheritdoc />
    public virtual void Dispose()
    {
        // The default source owns no disposable resources.
    }
}

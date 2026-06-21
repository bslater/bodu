// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStreamEntry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// Represents a stream within a compound file — a named, file-like entry with an opaque byte payload — and provides
/// access to its bytes and metadata.
/// </summary>
/// <remarks>
/// <para>
/// This type is the directory node for a stream, the managed counterpart of an <c>IStream</c>-bearing element. It is
/// the persistent identity and metadata view; the transient, disposable read cursor over the bytes is produced by
/// <see cref="Open" />, mirroring the relationship between <see cref="System.IO.Compression.ZipArchiveEntry" /> and its
/// <c>Open</c> method.
/// </para>
/// <para>
/// A writable cursor (<c>OpenWrite</c>) is reserved for a future read-write implementation and is not yet declared.
/// </para>
/// </remarks>
public sealed class CompoundStreamEntry
{
    /// <summary>The owning compound file used to materialize the payload.</summary>
    private readonly CompoundFile _file;

    /// <summary>The directory entry this stream wraps.</summary>
    private readonly CompoundDirectoryEntry _entry;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundStreamEntry" /> class.
    /// </summary>
    /// <param name="file">The owning compound file.</param>
    /// <param name="entry">The directory entry the stream wraps.</param>
    internal CompoundStreamEntry(CompoundFile file, CompoundDirectoryEntry entry)
    {
        _file = file;
        _entry = entry;
    }

    /// <summary>
    /// Gets the name of the stream as stored in the directory.
    /// </summary>
    /// <returns>The stream name, with any control prefix preserved.</returns>
    public string Name => _entry.Name;

    /// <summary>
    /// Gets the declared length, in bytes, of the stream payload.
    /// </summary>
    /// <returns>The stream length in bytes.</returns>
    public long Length => _entry.Size;

    /// <summary>
    /// Gets the metadata snapshot for this stream.
    /// </summary>
    /// <returns>A <see cref="CompoundEntryInfo" /> describing the stream.</returns>
    public CompoundEntryInfo Stat => _entry.ToEntryInfo();

    /// <summary>
    /// Opens a read-only, seekable view over the stream payload.
    /// </summary>
    /// <returns>A <see cref="CompoundStream" /> positioned at the start of the payload.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the owning file has been disposed.</exception>
    /// <exception cref="CompoundFileFormatException">Thrown when the stream's sector chain is malformed.</exception>
    public CompoundStream Open() =>
        _file.OpenStream(_entry);

    /// <summary>
    /// Materializes the entire stream payload into a contiguous read-only buffer.
    /// </summary>
    /// <returns>A <see cref="ReadOnlyMemory{T}" /> spanning the materialized bytes.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the owning file has been disposed.</exception>
    /// <exception cref="CompoundFileFormatException">Thrown when the stream's sector chain is malformed.</exception>
    public ReadOnlyMemory<byte> ReadAllBytes() =>
        _file.Materialize(_entry);
}

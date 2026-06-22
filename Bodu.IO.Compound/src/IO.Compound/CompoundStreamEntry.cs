// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStreamEntry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound.Internal;

namespace Bodu.IO.Compound;

/// <summary>
/// Represents a stream within a compound file — a named, file-like entry with an opaque byte payload — and provides
/// access to its bytes and metadata.
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/io-compound-stream-access.svg" alt="CompoundStreamEntry is the directory node for a stream; its Open method returns a CompoundStream read cursor. For a buffered compound file the payload is materialized into an in-memory byte array at open time, whereas for a streaming file the cursor reads the stream's sector chain on demand. ReadAllBytes materializes the whole payload in one call."/>
/// </para>
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
/// <example>
/// The following example enumerates the streams under the root storage and reports the size of each, then reads one
/// stream in full.
/// <code language="csharp">
///<![CDATA[
/// using Bodu.IO.Compound;
///
/// using CompoundFile file = CompoundFile.Open(File.OpenRead("message.msg"));
/// foreach (CompoundStreamEntry entry in file.RootStorage.EnumerateStreams())
///     Console.WriteLine($"{entry.Name}: {entry.Length} bytes");
///
/// CompoundStreamEntry body = file.RootStorage.OpenStream("__substg1.0_1000001F");
/// ReadOnlyMemory<byte> bytes = body.ReadAllBytes();
///]]>
/// </code>
/// </example>
public sealed class CompoundStreamEntry
{
    /// <summary>The owning compound file used to materialize the payload.</summary>
    private readonly CompoundFile _file;

    /// <summary>The directory entry this stream wraps.</summary>
    private readonly CfbDirectoryEntry _entry;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundStreamEntry" /> class.
    /// </summary>
    /// <param name="file">The owning compound file.</param>
    /// <param name="entry">The directory entry the stream wraps.</param>
    internal CompoundStreamEntry(CompoundFile file, CfbDirectoryEntry entry)
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
    /// <remarks>
    /// Prefer this over <see cref="ReadAllBytes" /> when the payload is consumed incrementally or is large enough that
    /// holding it whole in memory is undesirable; the returned cursor reads sectors on demand when its owning file was
    /// opened in streaming mode. Dispose the returned <see cref="CompoundStream" /> when finished.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown when the owning file has been disposed.</exception>
    /// <exception cref="CompoundFileFormatException">Thrown when the stream's sector chain is malformed.</exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// using CompoundStream stream = entry.Open();
    /// using var reader = new StreamReader(stream, Encoding.Unicode);
    /// string text = reader.ReadToEnd();
    ///]]>
    /// </code>
    /// </example>
    public CompoundStream Open() =>
        _file.OpenStream(_entry);

    /// <summary>
    /// Materializes the entire stream payload into a contiguous read-only buffer.
    /// </summary>
    /// <returns>A <see cref="ReadOnlyMemory{T}" /> spanning the materialized bytes.</returns>
    /// <remarks>
    /// This is a convenience over <see cref="Open" /> for small streams that are processed in one pass; it always reads
    /// the whole payload into memory regardless of how the owning file was opened. Prefer <see cref="Open" /> for large
    /// payloads.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown when the owning file has been disposed.</exception>
    /// <exception cref="CompoundFileFormatException">Thrown when the stream's sector chain is malformed.</exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// ReadOnlyMemory<byte> payload = entry.ReadAllBytes();
    /// int magic = BinaryPrimitives.ReadInt32LittleEndian(payload.Span);
    ///]]>
    /// </code>
    /// </example>
    public ReadOnlyMemory<byte> ReadAllBytes() =>
        _file.Materialize(_entry);
}

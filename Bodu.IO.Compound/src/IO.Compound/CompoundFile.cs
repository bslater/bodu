// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFile.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Bodu.IO.Compound.PropertySets;

namespace Bodu.IO.Compound;

/// <summary>
/// Reads an OLE2 / Compound File Binary (CFB) container, exposing its storage hierarchy, the metadata of every entry,
/// and the byte payload of each named stream.
/// </summary>
/// <remarks>
/// <para>
/// A compound file is a structured-storage envelope — effectively a small file system embedded in a single file — used
/// by legacy Microsoft Office formats (<c>.xls</c>, <c>.doc</c>, <c>.ppt</c>, <c>.msg</c>) and other technologies. This
/// type is the managed counterpart of the COM <c>StgOpenStorage</c> entry point: navigation begins at
/// <see cref="RootStorage" /> and descends through nested <see cref="CompoundStorage" /> objects to the
/// <see cref="CompoundStreamEntry" /> leaves.
/// </para>
/// <para>
/// The entire source is buffered into memory when the file is opened, so access after opening never touches the
/// original source. Instances are read-only and safe to share across threads once opened.
/// </para>
/// <para>
/// Only <see cref="CompoundFileMode.Read" /> is supported by the current release. Creation and mutation (<c>Create</c>,
/// <c>Commit</c>, and <c>Revert</c>) are reserved for a future read-write implementation.
/// </para>
/// </remarks>
public sealed class CompoundFile
    : IDisposable
{
    /// <summary>The eight-byte length of the compound-file signature.</summary>
    private const int SignatureLength = 8;

    /// <summary>The source stream retained only so it can be disposed according to the <c>leaveOpen</c> contract.</summary>
    private readonly Stream _source;

    /// <summary>Whether the source stream should be left open when this instance is disposed.</summary>
    private readonly bool _leaveOpen;

    /// <summary>The parsed header.</summary>
    private readonly CompoundFileHeader _header;

    /// <summary>The sector reader used to materialize stream payloads.</summary>
    private readonly CompoundSectorReader _sectors;

    /// <summary>The parsed directory and storage hierarchy.</summary>
    private readonly CompoundDirectory _directory;

    /// <summary>Whether this instance has been disposed.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundFile" /> class from buffered content.
    /// </summary>
    /// <param name="source">The source stream, retained for disposal.</param>
    /// <param name="leaveOpen">Whether to leave <paramref name="source" /> open on dispose.</param>
    /// <param name="mode">The requested access mode.</param>
    /// <param name="data">The full buffered compound-file content.</param>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the content is not a well-formed compound file.
    /// </exception>
    private CompoundFile(Stream source, bool leaveOpen, CompoundFileMode mode, byte[] data)
    {
        _source = source;
        _leaveOpen = leaveOpen;
        Mode = mode;
        _header = CompoundFileHeader.Parse(data);
        _sectors = new CompoundSectorReader(data, _header);

        byte[] directoryBytes = _sectors.ReadChainToEnd(_header.FirstDirectorySector);
        _directory = new CompoundDirectory(directoryBytes, _header);

        if (_directory.Root.Size > 0)
            _sectors.InitializeMiniStream(_directory.Root.StartSector, _directory.Root.Size);

        RootStorage = new CompoundStorage(this, _directory.Root);
    }

    /// <summary>
    /// Gets the mode the compound file was opened with.
    /// </summary>
    /// <returns>
    /// The <see cref="CompoundFileMode" /> supplied to <see cref="Open(Stream, CompoundFileMode, bool)" />.
    /// </returns>
    public CompoundFileMode Mode { get; }

    /// <summary>
    /// Gets the root storage that anchors the compound file's directory hierarchy.
    /// </summary>
    /// <returns>The root <see cref="CompoundStorage" />.</returns>
    public CompoundStorage RootStorage { get; }

    /// <summary>
    /// Opens a compound file over the supplied stream, buffering its content into memory.
    /// </summary>
    /// <param name="stream">The stream containing the compound file; read from its current position to the end.</param>
    /// <param name="mode">The access mode; only <see cref="CompoundFileMode.Read" /> is currently supported.</param>
    /// <param name="leaveOpen">
    /// <see langword="true" /> to leave <paramref name="stream" /> open when the returned instance is disposed;
    /// otherwise <see langword="false" />.
    /// </param>
    /// <returns>An open <see cref="CompoundFile" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="mode" /> is not <see cref="CompoundFileMode.Read" />.
    /// </exception>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the stream content is not a well-formed compound file.
    /// </exception>
    public static CompoundFile Open(Stream stream, CompoundFileMode mode = CompoundFileMode.Read, bool leaveOpen = false)
    {
        ThrowHelper.ThrowIfNull(stream);
        if (mode != CompoundFileMode.Read)
        {
            throw new NotSupportedException(
                string.Format(CultureInfo.CurrentCulture, CompoundResourceStrings.Op_NotSupported_CompoundFileWriteMode, mode));
        }

        return new CompoundFile(stream, leaveOpen, mode, ReadAllBytes(stream));
    }

    /// <summary>
    /// Determines whether the supplied stream begins with the compound-file (OLE2) signature without parsing the file.
    /// </summary>
    /// <param name="stream">A seekable stream to inspect; its position is restored before the method returns.</param>
    /// <returns>
    /// <see langword="true" /> when the stream's leading bytes are the compound-file signature; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="stream" /> is not seekable.</exception>
    public static bool IsCompoundFile(Stream stream)
    {
        ThrowHelper.ThrowIfNull(stream);
        if (!stream.CanSeek)
            throw new ArgumentException(CompoundResourceStrings.Arg_Invalid_CompoundStreamNotSeekable, nameof(stream));

        long origin = stream.Position;
        try
        {
            Span<byte> head = stackalloc byte[SignatureLength];
            int read = stream.ReadAtLeast(head, SignatureLength, throwOnEndOfStream: false);
            return read == SignatureLength && IsCompoundFile(head);
        }
        finally
        {
            stream.Position = origin;
        }
    }

    /// <summary>
    /// Determines whether the supplied bytes begin with the compound-file (OLE2) signature.
    /// </summary>
    /// <param name="data">The bytes to inspect.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="data" /> begins with the compound-file signature; otherwise
    /// <see langword="false" />.
    /// </returns>
    public static bool IsCompoundFile(ReadOnlySpan<byte> data) =>
        data.Length >= SignatureLength && data.Slice(0, SignatureLength).SequenceEqual(CompoundFileHeader.Signature);

    /// <summary>
    /// Attempts to read the standard summary-information property set from the root storage.
    /// </summary>
    /// <param name="summary">
    /// When this method returns <see langword="true" />, the parsed <see cref="SummaryInformation" />; otherwise
    /// <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the root storage contains a summary-information stream; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown when the file has been disposed.</exception>
    /// <exception cref="CompoundFileFormatException">Thrown when the property-set stream is malformed.</exception>
    public bool TryGetSummaryInformation([MaybeNullWhen(false)] out SummaryInformation summary)
    {
        if (RootStorage.TryOpenPropertySet(SummaryInformation.StreamName, out OlePropertySet? set))
        {
            summary = new SummaryInformation(set);
            return true;
        }

        summary = null;
        return false;
    }

    /// <summary>
    /// Attempts to read the document-summary-information property set from the root storage.
    /// </summary>
    /// <param name="summary">
    /// When this method returns <see langword="true" />, the parsed <see cref="DocumentSummaryInformation" />;
    /// otherwise <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the root storage contains a document-summary-information stream; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown when the file has been disposed.</exception>
    /// <exception cref="CompoundFileFormatException">Thrown when the property-set stream is malformed.</exception>
    public bool TryGetDocumentSummaryInformation([MaybeNullWhen(false)] out DocumentSummaryInformation summary)
    {
        if (RootStorage.TryOpenPropertySet(DocumentSummaryInformation.StreamName, out OlePropertySet? set))
        {
            summary = new DocumentSummaryInformation(set);
            return true;
        }

        summary = null;
        return false;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (!_leaveOpen)
            _source.Dispose();
    }

    /// <summary>
    /// Resolves the directory entry with the specified stream identifier.
    /// </summary>
    /// <param name="sid">The stream identifier to resolve.</param>
    /// <returns>The entry, or <see langword="null" /> when the slot is unallocated.</returns>
    internal CompoundDirectoryEntry? GetEntry(int sid) =>
        _directory.GetEntry(sid);

    /// <summary>
    /// Materializes the byte payload of a stream entry, choosing the mini-FAT or regular FAT based on its size.
    /// </summary>
    /// <param name="entry">The stream entry to materialize.</param>
    /// <returns>The materialized payload, exactly <see cref="CompoundDirectoryEntry.Size" /> bytes long.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the file has been disposed.</exception>
    /// <exception cref="CompoundFileFormatException">Thrown when the stream's sector chain is malformed.</exception>
    internal byte[] Materialize(CompoundDirectoryEntry entry)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return entry.Size < _header.MiniStreamCutoff
            ? _sectors.ReadMiniChain(entry.StartSector, entry.Size)
            : _sectors.ReadChain(entry.StartSector, entry.Size);
    }

    /// <summary>
    /// Reads the full content of a stream into a byte array.
    /// </summary>
    /// <param name="stream">The stream to read from its current position to the end.</param>
    /// <returns>The buffered bytes.</returns>
    private static byte[] ReadAllBytes(Stream stream)
    {
        if (stream is MemoryStream existing)
            return existing.ToArray();

        using MemoryStream buffer = new();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}

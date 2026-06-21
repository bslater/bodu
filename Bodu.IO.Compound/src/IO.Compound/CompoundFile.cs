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
/// By default the entire source is buffered into memory when the file is opened, so access after opening never touches
/// the original source, and instances are read-only and safe to share across threads. Opening with
/// <c>buffered: false</c> instead reads sectors on demand from a seekable stream — bounding memory for large files — in
/// which case the stream must stay open for the instance's lifetime and reads are serialized rather than parallel.
/// </para>
/// <para>
/// Only <see cref="CompoundFileMode.Read" /> is supported by the current release. Creation and mutation (<c>Create</c>,
/// <c>Commit</c>, and <c>Revert</c>) are reserved for a future read-write implementation.
/// </para>
/// </remarks>
/// <example>
/// The following example opens a compound file, walks its top-level entries, and reads one named stream. The instance
/// is disposed by the <c>using</c> declaration, which also closes the source stream unless <c>leaveOpen</c> is set.
/// <code language="csharp">
///<![CDATA[
/// using Bodu.IO.Compound;
///
/// using CompoundFile file = CompoundFile.Open(File.OpenRead("book.xls"));
/// foreach (CompoundEntryInfo info in file.RootStorage.EnumerateEntries())
///     Console.WriteLine($"{info.Type}: {info.Name}");
///
/// if (file.RootStorage.TryOpenStream("Workbook", out CompoundStreamEntry? workbook))
/// {
///     using CompoundStream stream = workbook.Open();
///     // ... read the BIFF records ...
/// }
///]]>
/// </code>
/// </example>
public sealed class CompoundFile
    : IDisposable
{
    /// <summary>The eight-byte length of the compound-file signature.</summary>
    private const int SignatureLength = 8;

    /// <summary>The number of header bytes required to parse the header.</summary>
    private const int HeaderLength = 512;

    /// <summary>The source stream retained only so it can be disposed according to the <c>leaveOpen</c> contract.</summary>
    private readonly Stream _source;

    /// <summary>Whether the source stream should be left open when this instance is disposed.</summary>
    private readonly bool _leaveOpen;

    /// <summary>The parsed header.</summary>
    private readonly CompoundFileHeader _header;

    /// <summary>The random-access byte source backing the reader.</summary>
    private readonly CompoundDataSource _dataSource;

    /// <summary>The sector reader used to materialize stream payloads.</summary>
    private readonly CompoundSectorReader _sectors;

    /// <summary>The parsed directory and storage hierarchy.</summary>
    private readonly CompoundDirectory _directory;

    /// <summary>Whether stream payloads are read on demand rather than from a fully buffered file.</summary>
    private readonly bool _streaming;

    /// <summary>Whether this instance has been disposed.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundFile" /> class over a byte source.
    /// </summary>
    /// <param name="source">The source stream, retained for disposal.</param>
    /// <param name="leaveOpen">Whether to leave <paramref name="source" /> open on dispose.</param>
    /// <param name="mode">The requested access mode.</param>
    /// <param name="dataSource">The random-access byte source.</param>
    /// <param name="streaming">Whether large stream payloads are read on demand.</param>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the content is not a well-formed compound file.
    /// </exception>
    private CompoundFile(Stream source, bool leaveOpen, CompoundFileMode mode, CompoundDataSource dataSource, bool streaming)
    {
        _source = source;
        _leaveOpen = leaveOpen;
        Mode = mode;
        _dataSource = dataSource;
        _streaming = streaming;

        int headLength = (int)Math.Min(HeaderLength, dataSource.Length);
        Span<byte> head = stackalloc byte[HeaderLength];
        dataSource.Read(0, head.Slice(0, headLength));
        _header = CompoundFileHeader.Parse(head.Slice(0, headLength));
        _sectors = new CompoundSectorReader(dataSource, _header);

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
    /// The <see cref="CompoundFileMode" /> supplied to <see cref="Open(Stream, CompoundFileMode, bool, bool)" />.
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
    /// <param name="buffered">
    /// <see langword="true" /> (the default) to read the whole file into memory at open time; <see langword="false" />
    /// to read sectors on demand from the seekable <paramref name="stream" />, bounding memory for large files. When
    /// <see langword="false" />, the stream must remain open and unmodified for the lifetime of the returned instance.
    /// </param>
    /// <returns>An open <see cref="CompoundFile" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="buffered" /> is <see langword="false" /> and <paramref name="stream" /> is not
    /// seekable.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="mode" /> is not <see cref="CompoundFileMode.Read" />.
    /// </exception>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the stream content is not a well-formed compound file.
    /// </exception>
    /// <example>
    /// The default opens the file fully buffered, so the source can be closed immediately after opening:
    /// <code language="csharp">
    ///<![CDATA[
    /// using CompoundFile file = CompoundFile.Open(File.OpenRead("book.xls"));
    ///]]>
    /// </code>
    /// To bound memory for a large file, open it in streaming mode and keep the source open for the file's lifetime:
    /// <code language="csharp">
    ///<![CDATA[
    /// using FileStream source = File.OpenRead("large.msg");
    /// using CompoundFile file = CompoundFile.Open(source, buffered: false);
    ///]]>
    /// </code>
    /// </example>
    public static CompoundFile Open(Stream stream, CompoundFileMode mode = CompoundFileMode.Read, bool leaveOpen = false, bool buffered = true)
    {
        ThrowHelper.ThrowIfNull(stream);
        if (mode != CompoundFileMode.Read)
        {
            throw new NotSupportedException(
                string.Format(CultureInfo.CurrentCulture, CompoundResourceStrings.Op_NotSupported_CompoundFileWriteMode, mode));
        }

        if (buffered)
            return new CompoundFile(stream, leaveOpen, mode, new CompoundArrayDataSource(ReadAllBytes(stream)), streaming: false);

        if (!stream.CanSeek)
            throw new ArgumentException(CompoundResourceStrings.Arg_Invalid_CompoundStreamNotSeekable, nameof(stream));

        return new CompoundFile(stream, leaveOpen, mode, new CompoundStreamDataSource(stream), streaming: true);
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
    /// <example>
    /// Probe a file cheaply before committing to a full open:
    /// <code language="csharp">
    ///<![CDATA[
    /// using FileStream source = File.OpenRead(path);
    /// if (CompoundFile.IsCompoundFile(source))
    /// {
    ///     using CompoundFile file = CompoundFile.Open(source, leaveOpen: true);
    ///     // ...
    /// }
    ///]]>
    /// </code>
    /// </example>
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
        _dataSource.Dispose();
        if (!_leaveOpen)
            _source.Dispose();
    }

    /// <summary>
    /// Opens a read cursor over a stream entry, returning a lazily-read view for large streams when the file is opened
    /// in streaming mode and a fully-materialized view otherwise.
    /// </summary>
    /// <param name="entry">The stream entry to open.</param>
    /// <returns>A <see cref="CompoundStream" /> positioned at the start of the payload.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the file has been disposed.</exception>
    /// <exception cref="CompoundFileFormatException">Thrown when the stream's sector chain is malformed.</exception>
    internal CompoundStream OpenStream(CompoundDirectoryEntry entry)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_streaming || entry.Size < _header.MiniStreamCutoff)
            return new CompoundStream(entry.Name, Materialize(entry));

        uint[] chain = _sectors.GetSectorChain(entry.StartSector);
        return new CompoundStream(entry.Name, _sectors, chain, entry.Size, _header.SectorSize);
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

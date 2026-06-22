// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFile.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Bodu.IO.Compound.Builders;
using Bodu.IO.Compound.Internal;
using Bodu.IO.Compound.PropertySets;

namespace Bodu.IO.Compound;

/// <summary>
/// Reads an OLE2 / Compound File Binary (CFB) container, exposing its storage hierarchy, the metadata of every entry,
/// and the byte payload of each named stream.
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/io-compound-structure.svg" alt="A compound file is a structured-storage envelope: one physical file that begins with the OLE2 signature D0 CF 11 E0 and holds a header, allocation tables (FAT and mini-FAT), a directory, and sectors. CompoundFile.Open parses that container and exposes a logical hierarchy. Navigation starts at RootStorage and descends through nested CompoundStorage containers to CompoundStream leaves; a CompoundStorage is the managed counterpart of the COM IStorage interface and a CompoundStream is the counterpart of IStream."/>
/// </para>
/// <para>
/// A compound file is a structured-storage envelope — effectively a small file system embedded in a single file — used
/// by legacy Microsoft Office formats (<c>.xls</c>, <c>.doc</c>, <c>.ppt</c>, <c>.msg</c>) and other technologies. This
/// type is the managed counterpart of the COM <c>StgOpenStorage</c> entry point: navigation begins at
/// <see cref="RootStorage" /> and descends through nested <see cref="CompoundStorage" /> objects to the
/// <see cref="CompoundStream" /> leaves.
/// </para>
/// <para>
/// By default the entire source is buffered into memory when the file is opened, so access after opening never touches
/// the original source, and instances are read-only and safe to share across threads. Opening with
/// <c>buffered: false</c> instead reads sectors on demand from a seekable stream — bounding memory for large files — in
/// which case the stream must stay open for the instance's lifetime and reads are serialized rather than parallel.
/// </para>
/// <para>
/// Opening with <see cref="FileAccess.Read" /> yields a read-only file.
/// <see cref="Create(System.IO.Stream, bool, Bodu.IO.Compound.Builders.CompoundBuildOptions)" /> (or a creating
/// <see cref="FileMode" />) yields a writable file whose edits are staged in memory and written to the destination only
/// by <see cref="Commit" />; in-place update of an existing file is not yet supported.
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
///     Console.WriteLine($"{info.EntryType}: {info.Name} ({info.Length} bytes)");
///
/// if (file.RootStorage.TryOpenStream("Workbook", out CompoundStream? workbook))
/// {
///     using (workbook)
///         // ... read the BIFF records from the workbook stream ...
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
    private readonly CfbHeader _header;

    /// <summary>The random-access byte source backing the reader.</summary>
    private readonly CfbDataSource _dataSource;

    /// <summary>The sector reader used to materialize stream payloads.</summary>
    private readonly CfbSectorReader _sectors;

    /// <summary>The parsed directory and storage hierarchy.</summary>
    private readonly CfbDirectory _directory;

    /// <summary>Whether stream payloads are read on demand rather than from a fully buffered file.</summary>
    private readonly bool _streaming;

    /// <summary>The mutable staging tree for a writable file, or <see langword="null" /> when read-only.</summary>
    private readonly CompoundStorageBuilder? _staging;

    /// <summary>The destination written by <see cref="Commit" /> for a writable file, or <see langword="null" /> when read-only.</summary>
    private readonly Stream? _destination;

    /// <summary>The build options applied when serializing the staging tree.</summary>
    private readonly CompoundBuildOptions _buildOptions;

    /// <summary>Whether the staging tree carries uncommitted edits.</summary>
    private bool _dirty;

    /// <summary>Whether this instance has been disposed.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundFile" /> class over a byte source.
    /// </summary>
    /// <param name="source">The source stream, retained for disposal.</param>
    /// <param name="leaveOpen">Whether to leave <paramref name="source" /> open on dispose.</param>
    /// <param name="access">The access level the file was opened with.</param>
    /// <param name="dataSource">The random-access byte source.</param>
    /// <param name="streaming">Whether large stream payloads are read on demand.</param>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the content is not a well-formed compound file.
    /// </exception>
    private CompoundFile(Stream source, bool leaveOpen, FileAccess access, CfbDataSource dataSource, bool streaming)
    {
        _source = source;
        _leaveOpen = leaveOpen;
        Access = access;
        _dataSource = dataSource;
        _streaming = streaming;

        int headLength = (int)Math.Min(HeaderLength, dataSource.Length);
        Span<byte> head = stackalloc byte[HeaderLength];
        dataSource.Read(0, head.Slice(0, headLength));
        _header = CfbHeader.Parse(head.Slice(0, headLength));
        _sectors = new CfbSectorReader(dataSource, _header);

        byte[] directoryBytes = _sectors.ReadChainToEnd(_header.FirstDirectorySector);
        _directory = new CfbDirectory(directoryBytes, _header);

        if (_directory.Root.Size > 0)
            _sectors.InitializeMiniStream(_directory.Root.StartSector, _directory.Root.Size);

        RootStorage = new CompoundStorage(this, _directory.Root);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundFile" /> class as a writable file over a staging tree.
    /// </summary>
    /// <param name="destination">The destination written by <see cref="Commit" />.</param>
    /// <param name="leaveOpen">Whether to leave <paramref name="destination" /> open on dispose.</param>
    /// <param name="access">The access level the file was opened with.</param>
    /// <param name="staging">The mutable staging tree that backs the file's hierarchy.</param>
    /// <param name="options">The build options applied when serializing the staging tree.</param>
    private CompoundFile(Stream destination, bool leaveOpen, FileAccess access, CompoundStorageBuilder staging, CompoundBuildOptions options)
    {
        _source = destination;
        _destination = destination;
        _leaveOpen = leaveOpen;
        Access = access;
        _staging = staging;
        _buildOptions = options;

        // The read pipeline is unused in write mode; these fields are never dereferenced while staging.
        _dataSource = null!;
        _header = null!;
        _sectors = null!;
        _directory = null!;

        RootStorage = new CompoundStorage(this, staging);
    }

    /// <summary>
    /// Gets the access level the compound file was opened with.
    /// </summary>
    /// <returns>
    /// The <see cref="FileAccess" /> supplied at open time. The current release supports read access only.
    /// </returns>
    public FileAccess Access { get; }

    /// <summary>
    /// Gets a value indicating whether the compound file can be read.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when the file was opened with read access; otherwise <see langword="false" />.
    /// </returns>
    public bool CanRead => (Access & FileAccess.Read) != 0;

    /// <summary>
    /// Gets a value indicating whether the compound file can be written.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when the file was opened with write access; otherwise <see langword="false" />.
    /// </returns>
    public bool CanWrite => (Access & FileAccess.Write) != 0;

    /// <summary>
    /// Gets a value indicating whether the file has staged edits that have not been committed.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when a writable file has uncommitted changes; otherwise <see langword="false" />.
    /// </returns>
    public bool IsDirty => _dirty;

    /// <summary>
    /// Gets the root storage that anchors the compound file's directory hierarchy.
    /// </summary>
    /// <returns>The root <see cref="CompoundStorage" />.</returns>
    public CompoundStorage RootStorage { get; }

    /// <summary>
    /// Opens an existing compound file over the supplied stream for read-only access, buffering its content into memory
    /// by default.
    /// </summary>
    /// <param name="stream">The stream containing the compound file; read from its current position to the end.</param>
    /// <param name="leaveOpen">
    /// <see langword="true" /> to leave <paramref name="stream" /> open when the returned instance is disposed;
    /// otherwise <see langword="false" />.
    /// </param>
    /// <param name="buffered">
    /// <see langword="true" /> (the default) to read the whole file into memory at open time; <see langword="false" />
    /// to read sectors on demand from the seekable <paramref name="stream" />, bounding memory for large files. When
    /// <see langword="false" />, the stream must remain open and unmodified for the lifetime of the returned instance.
    /// </param>
    /// <returns>An open, read-only <see cref="CompoundFile" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="buffered" /> is <see langword="false" /> and <paramref name="stream" /> is not
    /// seekable.
    /// </exception>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the stream content is not a well-formed compound file.
    /// </exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// using CompoundFile file = CompoundFile.Open(File.OpenRead("book.xls"));
    /// using FileStream source = File.OpenRead("large.msg");
    /// using CompoundFile streamed = CompoundFile.Open(source, buffered: false);
    ///]]>
    /// </code>
    /// </example>
    public static CompoundFile Open(Stream stream, bool leaveOpen = false, bool buffered = true) =>
        OpenReadCore(stream, leaveOpen, buffered);

    /// <summary>
    /// Opens a compound file over the supplied stream with BCL-style <see cref="FileMode" /> and
    /// <see cref="FileAccess" /> semantics, mirroring <c>System.IO.Packaging.Package.Open</c>.
    /// </summary>
    /// <param name="stream">The stream containing the compound file, or the destination for a writable file.</param>
    /// <param name="mode">
    /// The file mode. <see cref="FileMode.Open" /> with <see cref="FileAccess.Read" /> opens for reading;
    /// <see cref="FileMode.Create" />, <see cref="FileMode.CreateNew" />, or <see cref="FileMode.OpenOrCreate" /> with
    /// write access starts a new, empty writable file.
    /// </param>
    /// <param name="access">The access level. Write access requires a creating <paramref name="mode" />.</param>
    /// <param name="leaveOpen">
    /// <see langword="true" /> to leave <paramref name="stream" /> open when the returned instance is disposed;
    /// otherwise <see langword="false" />.
    /// </param>
    /// <param name="buffered">
    /// <see langword="true" /> (the default) to read the whole file into memory at open time; <see langword="false" />
    /// to read sectors on demand from the seekable <paramref name="stream" />. Ignored for writable files.
    /// </param>
    /// <returns>An open <see cref="CompoundFile" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when the combination of <paramref name="mode" /> and <paramref name="access" /> is not supported, such as
    /// opening an existing file for in-place update.
    /// </exception>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the stream content is not a well-formed compound file.
    /// </exception>
    public static CompoundFile Open(Stream stream, FileMode mode, FileAccess access, bool leaveOpen = false, bool buffered = true)
    {
        ThrowHelper.ThrowIfNull(stream);

        if (mode == FileMode.Open && access == FileAccess.Read)
            return OpenReadCore(stream, leaveOpen, buffered);

        if ((access & FileAccess.Write) != 0 && mode is FileMode.Create or FileMode.CreateNew or FileMode.OpenOrCreate)
            return CreateCore(stream, leaveOpen, access, default);

        throw new NotSupportedException(
            string.Format(CultureInfo.CurrentCulture, CompoundResourceStrings.Op_NotSupported_CompoundFileWriteMode, $"{mode}/{access}"));
    }

    /// <summary>
    /// Creates a new, empty writable compound file whose staged contents are written to the supplied destination by
    /// <see cref="Commit" />.
    /// </summary>
    /// <param name="destination">The stream the committed compound file is written to.</param>
    /// <param name="leaveOpen">
    /// <see langword="true" /> to leave <paramref name="destination" /> open when the returned instance is disposed;
    /// otherwise <see langword="false" />.
    /// </param>
    /// <param name="options">The build options applied when the staging tree is serialized.</param>
    /// <returns>A new writable <see cref="CompoundFile" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="destination" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Edits made through <see cref="RootStorage" /> are staged in memory; nothing is written to
    /// <paramref name="destination" /> until <see cref="Commit" /> is called. Disposing the file without committing
    /// discards the staged edits.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// using FileStream output = File.Create("book.xls");
    /// using CompoundFile file = CompoundFile.Create(output);
    /// using (CompoundStream stream = file.RootStorage.CreateStream("Workbook"))
    ///     stream.Write(workbookBytes);
    /// file.Commit();
    ///]]>
    /// </code>
    /// </example>
    public static CompoundFile Create(Stream destination, bool leaveOpen = false, CompoundBuildOptions options = default)
    {
        ThrowHelper.ThrowIfNull(destination);

        return CreateCore(destination, leaveOpen, FileAccess.ReadWrite, options);
    }

    /// <summary>
    /// Creates a new, empty writable compound file whose staged contents are written to the file at the specified path
    /// by <see cref="Commit" />.
    /// </summary>
    /// <param name="path">The path of the file the committed compound file is written to.</param>
    /// <param name="options">The build options applied when the staging tree is serialized.</param>
    /// <returns>A new writable <see cref="CompoundFile" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="path" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// The returned instance owns the created file and closes it when disposed. Edits are staged in memory until
    /// <see cref="Commit" /> is called; disposing without committing leaves the file empty.
    /// </remarks>
    public static CompoundFile Create(string path, CompoundBuildOptions options = default)
    {
        ThrowHelper.ThrowIfNull(path);

        FileStream stream = File.Create(path);
        try
        {
            return CreateCore(stream, leaveOpen: false, FileAccess.ReadWrite, options);
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Creates a writable compound file over an empty staging tree.
    /// </summary>
    /// <param name="destination">The destination the staging tree is committed to.</param>
    /// <param name="leaveOpen">Whether to leave <paramref name="destination" /> open on dispose.</param>
    /// <param name="access">The access level the file is opened with.</param>
    /// <param name="options">The build options applied when serializing the staging tree.</param>
    /// <returns>A new writable <see cref="CompoundFile" />.</returns>
    private static CompoundFile CreateCore(Stream destination, bool leaveOpen, FileAccess access, CompoundBuildOptions options) =>
        new(destination, leaveOpen, access, CompoundStorageBuilder.CreateRoot(), options);

    /// <summary>
    /// Opens a read-only compound file over the supplied stream, choosing the buffered or streaming data source.
    /// </summary>
    /// <param name="stream">The stream containing the compound file.</param>
    /// <param name="leaveOpen">Whether to leave <paramref name="stream" /> open when the instance is disposed.</param>
    /// <param name="buffered">Whether to buffer the whole file into memory at open time.</param>
    /// <returns>An open, read-only <see cref="CompoundFile" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="buffered" /> is <see langword="false" /> and <paramref name="stream" /> is not
    /// seekable.
    /// </exception>
    private static CompoundFile OpenReadCore(Stream stream, bool leaveOpen, bool buffered)
    {
        ThrowHelper.ThrowIfNull(stream);

        if (buffered)
            return new CompoundFile(stream, leaveOpen, FileAccess.Read, new CfbArrayDataSource(ReadAllBytes(stream)), streaming: false);

        if (!stream.CanSeek)
            throw new ArgumentException(CompoundResourceStrings.Arg_Invalid_CompoundStreamNotSeekable, nameof(stream));

        return new CompoundFile(stream, leaveOpen, FileAccess.Read, new CfbStreamDataSource(stream), streaming: true);
    }

    /// <summary>
    /// Opens an existing compound file at the specified path for read-only access, buffering its content into memory.
    /// </summary>
    /// <param name="path">The path of the compound file to open.</param>
    /// <returns>An open, read-only <see cref="CompoundFile" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="path" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FileNotFoundException">Thrown when no file exists at <paramref name="path" />.</exception>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the file content is not a well-formed compound file.
    /// </exception>
    /// <remarks>
    /// The returned instance owns the opened file and closes it when disposed. The file is opened with
    /// <see cref="FileShare.Read" />, so it does not lock out other readers.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// using CompoundFile file = CompoundFile.OpenRead("book.xls");
    /// foreach (CompoundEntryInfo info in file.RootStorage.EnumerateEntries())
    ///     Console.WriteLine($"{info.EntryType}: {info.Name}");
    ///]]>
    /// </code>
    /// </example>
    public static CompoundFile OpenRead(string path)
    {
        ThrowHelper.ThrowIfNull(path);

        FileStream stream = File.OpenRead(path);
        try
        {
            return OpenReadCore(stream, leaveOpen: false, buffered: true);
        }
        catch
        {
            stream.Dispose();
            throw;
        }
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
        data.Length >= SignatureLength && data.Slice(0, SignatureLength).SequenceEqual(CfbHeader.Signature);

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

    /// <summary>
    /// Writes the staged contents of a writable file to its destination, clearing the dirty state.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the file is read-only.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the file has been disposed.</exception>
    /// <exception cref="CompoundFileSerializationException">
    /// Thrown when the staging tree cannot be represented.
    /// </exception>
    /// <remarks>
    /// The destination is truncated and the whole container is rewritten from the staging tree. When the destination is
    /// not seekable, the committed bytes are appended at the current position.
    /// </remarks>
    public void Commit()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        RequireWritable();

        if (_destination!.CanSeek)
        {
            _destination.Position = 0;
            _destination.SetLength(0);
        }

        CompoundContainerLayout.WriteTo(_destination, _staging!, _buildOptions);
        _destination.Flush();
        _dirty = false;
    }

    /// <summary>
    /// Writes the staged contents of a writable file to its destination, clearing the dirty state.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the file is read-only.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the file has been disposed.</exception>
    /// <exception cref="CompoundFileSerializationException">
    /// Thrown when the staging tree cannot be represented.
    /// </exception>
    /// <remarks>
    /// This is an alias for <see cref="Commit" />, named for parity with stream-style consumers.
    /// </remarks>
    public void Flush() =>
        Commit();

    /// <summary>
    /// Discards all staged edits, restoring the writable file to an empty root storage.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the file is read-only.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the file has been disposed.</exception>
    public void Revert()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        RequireWritable();

        _staging!.Clear();
        _dirty = false;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (_staging is null)
            _dataSource.Dispose();

        if (!_leaveOpen)
            _source.Dispose();
    }

    /// <summary>
    /// Marks the writable file as having uncommitted staged edits.
    /// </summary>
    internal void MarkDirty() =>
        _dirty = true;

    /// <summary>
    /// Throws when this file is read-only.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the file is read-only.</exception>
    private void RequireWritable()
    {
        if (_staging is null)
            throw new InvalidOperationException(CompoundResourceStrings.Op_Invalid_CompoundFileReadOnly);
    }

    /// <summary>
    /// Opens a read cursor over a stream entry, returning a lazily-read view for large streams when the file is opened
    /// in streaming mode and a fully-materialized view otherwise.
    /// </summary>
    /// <param name="entry">The stream entry to open.</param>
    /// <returns>A <see cref="CompoundStream" /> positioned at the start of the payload.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the file has been disposed.</exception>
    /// <exception cref="CompoundFileFormatException">Thrown when the stream's sector chain is malformed.</exception>
    internal CompoundStream OpenStream(CfbDirectoryEntry entry)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        CompoundEntryInfo info = entry.ToEntryInfo();
        if (!_streaming || entry.Size < _header.MiniStreamCutoff)
            return new CompoundStream(info, Materialize(entry));

        uint[] chain = _sectors.GetSectorChain(entry.StartSector);
        return new CompoundStream(info, _sectors, chain, entry.Size, _header.SectorSize);
    }

    /// <summary>
    /// Resolves the directory entry with the specified stream identifier.
    /// </summary>
    /// <param name="sid">The stream identifier to resolve.</param>
    /// <returns>The entry, or <see langword="null" /> when the slot is unallocated.</returns>
    internal CfbDirectoryEntry? GetEntry(int sid) =>
        _directory.GetEntry(sid);

    /// <summary>
    /// Materializes the byte payload of a stream entry, choosing the mini-FAT or regular FAT based on its size.
    /// </summary>
    /// <param name="entry">The stream entry to materialize.</param>
    /// <returns>The materialized payload, exactly <see cref="CfbDirectoryEntry.Size" /> bytes long.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the file has been disposed.</exception>
    /// <exception cref="CompoundFileFormatException">Thrown when the stream's sector chain is malformed.</exception>
    internal byte[] Materialize(CfbDirectoryEntry entry)
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

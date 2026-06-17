// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundBinaryFile.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Bodu.IO.Compound;

/// <summary>
/// Reads an OLE2 / Compound File Binary (CFB) container, exposing its directory entries and the byte payload of each
/// named stream.
/// </summary>
/// <remarks>
/// <para>
/// A compound file is a structured-storage envelope — effectively a small file system embedded in a single file — used
/// by legacy Microsoft Office formats (<c>.xls</c>, <c>.doc</c>, <c>.ppt</c>, <c>.msg</c>) and other technologies. This
/// reader understands only the container: it surfaces storages and streams by name and materializes a stream's bytes
/// from its sector chain. It applies no interpretation to the contents of any stream.
/// </para>
/// <para>
/// The entire source is buffered into memory when the file is opened, so stream access after opening never touches the
/// original source. Instances are read-only and safe to share across threads once opened.
/// </para>
/// </remarks>
public sealed class CompoundBinaryFile
    : IDisposable
{
    /// <summary>The fixed size, in bytes, of a single directory entry.</summary>
    private const int DirectoryEntrySize = 128;

    /// <summary>The source stream retained only so it can be disposed according to the <c>leaveOpen</c> contract.</summary>
    private readonly Stream _source;

    /// <summary>Whether the source stream should be left open when this instance is disposed.</summary>
    private readonly bool _leaveOpen;

    /// <summary>The parsed header.</summary>
    private readonly CompoundFileHeader _header;

    /// <summary>The sector reader used to materialize stream payloads.</summary>
    private readonly CompoundSectorReader _sectors;

    /// <summary>The stream entries indexed by ordinal name for fast lookup.</summary>
    private readonly Dictionary<string, CompoundDirectoryEntry> _streamsByName;

    /// <summary>Whether this instance has been disposed.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundBinaryFile" /> class from buffered content.
    /// </summary>
    /// <param name="source">The source stream, retained for disposal.</param>
    /// <param name="leaveOpen">Whether to leave <paramref name="source" /> open on dispose.</param>
    /// <param name="data">The full buffered compound-file content.</param>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the content is not a well-formed compound file.
    /// </exception>
    private CompoundBinaryFile(Stream source, bool leaveOpen, byte[] data)
    {
        _source = source;
        _leaveOpen = leaveOpen;
        _header = CompoundFileHeader.Parse(data);
        _sectors = new CompoundSectorReader(data, _header);

        var entries = ReadDirectory();
        Entries = entries;
        _streamsByName = BuildStreamIndex(entries);

        CompoundDirectoryEntry? root = entries.FirstOrDefault(static e => e.Type == CompoundDirectoryEntryType.RootStorage);
        if (root is not null && root.Size > 0)
            _sectors.InitializeMiniStream(root.StartSector, root.Size);
    }

    /// <summary>
    /// Gets every allocated directory entry in the compound file, in directory order.
    /// </summary>
    /// <returns>
    /// A read-only list of <see cref="CompoundDirectoryEntry" /> instances, including storages and the root.
    /// </returns>
    public IReadOnlyList<CompoundDirectoryEntry> Entries { get; }

    /// <summary>
    /// Opens a compound file over the supplied stream, buffering its content into memory.
    /// </summary>
    /// <param name="stream">The stream containing the compound file; read from its current position to the end.</param>
    /// <param name="leaveOpen">
    /// <see langword="true" /> to leave <paramref name="stream" /> open when the returned instance is disposed;
    /// otherwise <see langword="false" />.
    /// </param>
    /// <returns>An open <see cref="CompoundBinaryFile" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the stream content is not a well-formed compound file.
    /// </exception>
    public static CompoundBinaryFile Open(Stream stream, bool leaveOpen = false)
    {
        ThrowHelper.ThrowIfNull(stream);

        return new CompoundBinaryFile(stream, leaveOpen, ReadAllBytes(stream));
    }

    /// <summary>
    /// Attempts to open the named stream and materialize its bytes.
    /// </summary>
    /// <param name="name">The stream name to look up, compared using ordinal (case-sensitive) equality.</param>
    /// <param name="stream">
    /// When this method returns <see langword="true" />, the opened <see cref="CompoundStream" />; otherwise
    /// <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when a stream with the given name exists; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when the file has been disposed.</exception>
    /// <exception cref="CompoundFileFormatException">Thrown when the stream's sector chain is malformed.</exception>
    public bool TryGetStream(string name, [MaybeNullWhen(false)] out CompoundStream stream)
    {
        ThrowHelper.ThrowIfNull(name);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_streamsByName.TryGetValue(name, out CompoundDirectoryEntry? entry))
        {
            stream = MaterializeStream(entry);
            return true;
        }

        stream = null;
        return false;
    }

    /// <summary>
    /// Opens the named stream and materializes its bytes.
    /// </summary>
    /// <param name="name">The stream name to look up, compared using ordinal (case-sensitive) equality.</param>
    /// <returns>The opened <see cref="CompoundStream" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when the file has been disposed.</exception>
    /// <exception cref="CompoundStreamNotFoundException">Thrown when no stream with the given name exists.</exception>
    /// <exception cref="CompoundFileFormatException">Thrown when the stream's sector chain is malformed.</exception>
    public CompoundStream GetStream(string name) =>
        TryGetStream(name, out CompoundStream? stream)
            ? stream
            : throw CompoundStreamNotFoundException.ForName(name);

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

    /// <summary>
    /// Builds a name-to-entry index over the stream entries.
    /// </summary>
    /// <param name="entries">The parsed directory entries.</param>
    /// <returns>
    /// A dictionary mapping each stream's name to its entry; the first entry wins on a duplicate name.
    /// </returns>
    private static Dictionary<string, CompoundDirectoryEntry> BuildStreamIndex(IReadOnlyList<CompoundDirectoryEntry> entries)
    {
        Dictionary<string, CompoundDirectoryEntry> index = new(StringComparer.Ordinal);
        foreach (CompoundDirectoryEntry entry in entries)
        {
            if (entry.Type == CompoundDirectoryEntryType.Stream)
                _ = index.TryAdd(entry.Name, entry);
        }

        return index;
    }

    /// <summary>
    /// Materializes the byte payload of a stream entry, choosing the mini-FAT or regular FAT based on its size.
    /// </summary>
    /// <param name="entry">The stream entry to materialize.</param>
    /// <returns>A read-only <see cref="CompoundStream" /> over the entry's bytes.</returns>
    private CompoundStream MaterializeStream(CompoundDirectoryEntry entry)
    {
        byte[] bytes = entry.Size < _header.MiniStreamCutoff
            ? _sectors.ReadMiniChain(entry.StartSector, entry.Size)
            : _sectors.ReadChain(entry.StartSector, entry.Size);

        return new CompoundStream(entry.Name, bytes);
    }

    /// <summary>
    /// Reads and parses every allocated directory entry.
    /// </summary>
    /// <returns>The parsed directory entries in directory order.</returns>
    private List<CompoundDirectoryEntry> ReadDirectory()
    {
        byte[] directory = _sectors.ReadChainToEnd(_header.FirstDirectorySector);
        int count = directory.Length / DirectoryEntrySize;
        List<CompoundDirectoryEntry> entries = new(count);

        for (int i = 0; i < count; i++)
        {
            ReadOnlySpan<byte> record = directory.AsSpan(i * DirectoryEntrySize, DirectoryEntrySize);

            int nameLength = BinaryPrimitives.ReadUInt16LittleEndian(record.Slice(64));
            if (nameLength is 0 or > 64)
                continue;

            var type = (CompoundDirectoryEntryType)record[66];
            if (type is not CompoundDirectoryEntryType.Storage
                and not CompoundDirectoryEntryType.Stream
                and not CompoundDirectoryEntryType.RootStorage)
            {
                continue;
            }

            int characters = (nameLength / 2) - 1;
            string name = characters > 0 ? Encoding.Unicode.GetString(record.Slice(0, characters * 2)) : string.Empty;
            uint startSector = BinaryPrimitives.ReadUInt32LittleEndian(record.Slice(116));
            ulong rawSize = BinaryPrimitives.ReadUInt64LittleEndian(record.Slice(120));

            // Version-3 files (512-byte sectors) store the size in the low 32 bits; the high DWORD must be ignored.
            long size = _header.SectorSize == 512 ? (long)(rawSize & 0xFFFFFFFF) : (long)rawSize;

            entries.Add(new CompoundDirectoryEntry(name, type, startSector, size));
        }

        return entries;
    }
}

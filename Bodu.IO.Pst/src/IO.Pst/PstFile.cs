// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstFile.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.IO.Pst.Internal;

namespace Bodu.IO.Pst;

/// <summary>
/// Provides a disposable, read-only session over a Unicode-format PST file's node database: the header facts, the node
/// directory, and per-node data and subnode access.
/// </summary>
/// <remarks>
/// <para>
/// This is the container layer of MS-PST — it speaks node identifiers and raw payloads, decoding the format's permute
/// and cyclic content encodings transparently. Folder, message, and property semantics belong to the higher layers
/// built on top of it.
/// </para>
/// <para>
/// Reads seek the source stream on demand; the file is never buffered whole. The session is single-threaded: its
/// members must not be called concurrently.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.IO.Pst;
///
/// using var file = PstFile.OpenRead("archive.pst");
///
/// foreach (PstNodeInfo info in file.EnumerateNodes())
///     Console.WriteLine($"{info.NodeId} ({info.NodeId.Type}): {info.DataLength} bytes");
///]]>
/// </code>
/// </example>
/// </remarks>
public sealed class PstFile
    : IDisposable
{
    /// <summary>The validated random-access source.</summary>
    private readonly PstSource _source;

    /// <summary>The stream the session reads, disposed with the session unless left open.</summary>
    private readonly Stream _stream;

    /// <summary>Whether the source stream is left open on dispose.</summary>
    private readonly bool _leaveOpen;

    /// <summary>Whether this session has been disposed.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PstFile" /> class.
    /// </summary>
    /// <param name="source">The validated source.</param>
    /// <param name="stream">The underlying stream.</param>
    /// <param name="leaveOpen">Whether the stream is left open on dispose.</param>
    private PstFile(PstSource source, Stream stream, bool leaveOpen)
    {
        _source = source;
        _stream = stream;
        _leaveOpen = leaveOpen;
    }

    /// <summary>
    /// Gets the file's format variant.
    /// </summary>
    /// <value>Always <see cref="PstFileFormat.Unicode" /> — other recognized variants fail to open.</value>
    /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
    public PstFileFormat Format
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            return _source.Header.Format;
        }
    }

    /// <summary>
    /// Gets the content-encoding method applied to the file's block data.
    /// </summary>
    /// <value>The declared method; data is decoded transparently on read.</value>
    /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
    public PstCryptMethod CryptMethod
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            return _source.Header.CryptMethod;
        }
    }

    /// <summary>
    /// Opens a PST file from a path.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>The open session.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="path" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="PstFileFormatException">The file is not a structurally valid PST file.</exception>
    /// <exception cref="PstUnsupportedFormatException">The file uses a recognized but unsupported variant.</exception>
    public static PstFile OpenRead(string path)
    {
        ThrowHelper.ThrowIfNull(path);

        FileStream stream = File.OpenRead(path);
        try
        {
            return Open(stream, PstFileOptions.Default, leaveOpen: false);
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Opens a PST file from a stream with default options.
    /// </summary>
    /// <param name="stream">The readable, seekable stream positioned at the file start.</param>
    /// <param name="leaveOpen"><see langword="true" /> to leave the stream open when the session is disposed.</param>
    /// <returns>The open session.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">The stream is not readable and seekable.</exception>
    /// <exception cref="PstFileFormatException">The stream is not a structurally valid PST file.</exception>
    /// <exception cref="PstUnsupportedFormatException">The file uses a recognized but unsupported variant.</exception>
    public static PstFile OpenRead(Stream stream, bool leaveOpen = false) =>
        Open(stream, PstFileOptions.Default, leaveOpen);

    /// <summary>
    /// Opens a PST file from a stream with explicit options.
    /// </summary>
    /// <param name="stream">The readable, seekable stream positioned at the file start.</param>
    /// <param name="options">The reader options.</param>
    /// <param name="leaveOpen"><see langword="true" /> to leave the stream open when the session is disposed.</param>
    /// <returns>The open session.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="stream" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">The stream is not readable and seekable.</exception>
    /// <exception cref="PstFileFormatException">The stream is not a structurally valid PST file.</exception>
    /// <exception cref="PstUnsupportedFormatException">The file uses a recognized but unsupported variant.</exception>
    public static PstFile Open(Stream stream, PstFileOptions options, bool leaveOpen = false)
    {
        ThrowHelper.ThrowIfNull(stream);
        ThrowHelper.ThrowIfNull(options);
        if (!stream.CanRead || !stream.CanSeek) throw new ArgumentException(PstResourceStrings.Arg_Invalid_PstStreamNotReadableSeekable, nameof(stream));

        var headerBytes = new byte[PstHeader.UnicodeHeaderSize];
        int read = stream.ReadAtLeast(headerBytes, headerBytes.Length, throwOnEndOfStream: false);
        PstHeader header = PstHeader.Parse(headerBytes.AsSpan(0, read), options.ValidationLevel);

        return new PstFile(new PstSource(stream, header, options.ValidationLevel, options.BlockCacheSize), stream, leaveOpen);
    }

    /// <summary>
    /// Determines whether a stream begins with the PST magics; its position is restored before returning.
    /// </summary>
    /// <param name="stream">The readable, seekable stream to sniff.</param>
    /// <returns><see langword="true" /> when the stream looks like a PST file of any variant.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    public static bool IsPstFile(Stream stream)
    {
        ThrowHelper.ThrowIfNull(stream);

        if (!stream.CanRead || !stream.CanSeek)
            return false;

        long position = stream.Position;
        try
        {
            var magic = new byte[12];
            int read = stream.ReadAtLeast(magic, magic.Length, throwOnEndOfStream: false);
            return read == magic.Length && PstHeader.IsPstHeader(magic);
        }
        finally
        {
            stream.Position = position;
        }
    }

    /// <summary>
    /// Enumerates every node in the node B-tree, in identifier order.
    /// </summary>
    /// <returns>The node directory facts, resolved lazily.</returns>
    /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
    /// <exception cref="PstFileFormatException">A tree page or block fails validation while walking.</exception>
    public IEnumerable<PstNodeInfo> EnumerateNodes()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        foreach (PstNbtEntry entry in PstBTree.EnumerateNodes(_source))
            yield return ToInfo(entry);
    }

    /// <summary>
    /// Retrieves a node by identifier.
    /// </summary>
    /// <param name="id">The node identifier.</param>
    /// <returns>The node.</returns>
    /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
    /// <exception cref="PstFileException">No node with the identifier exists.</exception>
    public PstNode GetNode(PstNodeId id)
    {
        if (TryGetNode(id, out PstNode? node))
            return node;

        throw new PstFileException(string.Format(CultureInfo.CurrentCulture, PstResourceStrings.IO_KeyNotFound_PstNode, id));
    }

    /// <summary>
    /// Attempts to retrieve a node by identifier.
    /// </summary>
    /// <param name="id">The node identifier.</param>
    /// <param name="node">When this method returns <see langword="true" />, the node.</param>
    /// <returns><see langword="true" /> when the node exists.</returns>
    /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
    public bool TryGetNode(PstNodeId id, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out PstNode node)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (PstBTree.TryFindNode(_source, id.Value, out PstNbtEntry entry))
        {
            node = new PstNode(this, entry);
            return true;
        }

        node = null;
        return false;
    }

    /// <summary>
    /// Releases the session and, unless it was left open, the source stream.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (!_leaveOpen)
            _stream.Dispose();
    }

    /// <summary>
    /// Gets the validated source for node-level reads.
    /// </summary>
    /// <returns>The source.</returns>
    /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
    internal PstSource GetSource()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _source;
    }

    /// <summary>
    /// Projects a node entry to its public directory facts, resolving the data length through the block B-tree.
    /// </summary>
    /// <param name="entry">The node entry.</param>
    /// <returns>The node facts.</returns>
    internal PstNodeInfo ToInfo(PstNbtEntry entry) =>
        new(
            new PstNodeId(entry.NodeId),
            new PstNodeId(entry.ParentNodeId),
            ResolveDataLength(entry.DataBlockId),
            entry.SubnodeBlockId != 0);

    /// <summary>
    /// Resolves the total payload length behind a data-block identifier without materializing the data.
    /// </summary>
    /// <param name="blockId">The data-block identifier.</param>
    /// <returns>The payload length in bytes; <c>0</c> when there is no data or the block is unresolved.</returns>
    private long ResolveDataLength(ulong blockId)
    {
        if (blockId == 0 || !PstBTree.TryFindBlock(_source, blockId, out PstBbtEntry entry))
            return 0;

        if ((blockId & 0x2) == 0)
            return entry.Length;

        // An internal identifier means a data tree; its XBLOCK/XXBLOCK header records the logical total.
        byte[] tree = _source.ReadBlock(entry);
        return tree.Length >= 8 && tree[0] == 0x01
            ? System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(tree.AsSpan(4))
            : 0;
    }
}

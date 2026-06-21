// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundWriter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.IO.Compound.Nodes;

namespace Bodu.IO.Compound.Writer;

/// <summary>
/// Provides a low-level, imperative API for authoring a compound file by opening storages and writing streams, emitting
/// the complete container when flushed.
/// </summary>
/// <remarks>
/// <para>
/// Because the OLE2 / Compound File Binary layout is a whole-file property — the header, file-allocation table, and
/// directory all depend on the full set of streams — the writer accumulates the structure in memory and serializes it
/// once, on <see cref="Flush" /> or disposal. This mirrors the buffer-then-emit behavior of the sibling text writers.
/// </para>
/// <para>
/// For most scenarios the mutable object model (<see cref="CompoundStorageNode" />) is the more convenient surface;
/// this type suits callers that prefer a streaming, start/end authoring style.
/// </para>
/// </remarks>
public sealed class CompoundWriter
    : IDisposable
{
    /// <summary>The destination stream, when constructed over a stream.</summary>
    private readonly Stream? _stream;

    /// <summary>The destination buffer writer, when constructed over one.</summary>
    private readonly IBufferWriter<byte>? _buffer;

    /// <summary>The writer options.</summary>
    private readonly CompoundWriterOptions _options;

    /// <summary>The root storage accumulating the authored structure.</summary>
    private readonly CompoundStorageNode _root;

    /// <summary>The stack of open storages, with the root at the bottom.</summary>
    private readonly Stack<CompoundStorageNode> _open;

    /// <summary>The most recently opened storage or written stream, for metadata application.</summary>
    private CompoundNode _current;

    /// <summary>Whether the container has already been emitted.</summary>
    private bool _flushed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundWriter" /> class that writes to a stream.
    /// </summary>
    /// <param name="output">The destination stream.</param>
    /// <param name="options">The options controlling the output layout.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="output" /> is <see langword="null" />.
    /// </exception>
    public CompoundWriter(Stream output, CompoundWriterOptions options = default)
        : this(options)
    {
        ThrowHelper.ThrowIfNull(output);
        _stream = output;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundWriter" /> class that writes to a buffer writer.
    /// </summary>
    /// <param name="output">The destination buffer writer.</param>
    /// <param name="options">The options controlling the output layout.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="output" /> is <see langword="null" />.
    /// </exception>
    public CompoundWriter(IBufferWriter<byte> output, CompoundWriterOptions options = default)
        : this(options)
    {
        ThrowHelper.ThrowIfNull(output);
        _buffer = output;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundWriter" /> class with the shared writer state.
    /// </summary>
    /// <param name="options">The options controlling the output layout.</param>
    private CompoundWriter(CompoundWriterOptions options)
    {
        _options = options;
        _root = CompoundStorageNode.CreateRoot();
        _open = new Stack<CompoundStorageNode>();
        _open.Push(_root);
        _current = _root;
    }

    /// <summary>
    /// Opens a new child storage and makes it the current storage.
    /// </summary>
    /// <param name="name">The storage name.</param>
    /// <exception cref="CompoundFileSerializationException">
    /// Thrown when the name is invalid or already present.
    /// </exception>
    public void WriteStartStorage(string name)
    {
        CompoundStorageNode storage = _open.Peek().AddStorage(name);
        _open.Push(storage);
        _current = storage;
    }

    /// <summary>
    /// Closes the current storage, returning to its parent.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no storage other than the root is open.</exception>
    public void WriteEndStorage()
    {
        if (_open.Count <= 1)
            throw new InvalidOperationException(CompoundResourceStrings.Op_Invalid_CompoundNodeNotStorage);

        _ = _open.Pop();
        _current = _open.Peek();
    }

    /// <summary>
    /// Writes a stream into the current storage.
    /// </summary>
    /// <param name="name">The stream name.</param>
    /// <param name="content">The stream payload.</param>
    /// <exception cref="CompoundFileSerializationException">
    /// Thrown when the name is invalid or already present.
    /// </exception>
    public void WriteStream(string name, ReadOnlySpan<byte> content) =>
        _current = _open.Peek().AddStream(name, (ReadOnlyMemory<byte>)content.ToArray());

    /// <summary>
    /// Writes a deferred stream into the current storage, sourcing its payload on demand during serialization.
    /// </summary>
    /// <param name="name">The stream name.</param>
    /// <param name="openRead">A factory that opens a readable stream over the payload each time it is invoked.</param>
    /// <param name="length">The payload length, in bytes.</param>
    /// <exception cref="CompoundFileSerializationException">
    /// Thrown when the name is invalid or already present.
    /// </exception>
    public void WriteStream(string name, Func<Stream> openRead, long length) =>
        _current = _open.Peek().AddStream(name, openRead, length);

    /// <summary>
    /// Writes a deferred stream into the current storage, sourcing its payload on demand from a file.
    /// </summary>
    /// <param name="name">The stream name.</param>
    /// <param name="path">The path of the file providing the payload.</param>
    /// <exception cref="CompoundFileSerializationException">
    /// Thrown when the name is invalid or already present.
    /// </exception>
    public void WriteStreamFromFile(string name, string path) =>
        _current = _open.Peek().AddStreamFromFile(name, path);

    /// <summary>
    /// Sets the class identifier of the most recently opened storage or written stream.
    /// </summary>
    /// <param name="classId">The class identifier to assign.</param>
    public void SetClassId(Guid classId) =>
        _current.ClassId = classId;

    /// <summary>
    /// Sets the creation and modified times of the most recently opened storage or written stream.
    /// </summary>
    /// <param name="creationTime">The creation time, or <see langword="null" /> to clear it.</param>
    /// <param name="modifiedTime">The modified time, or <see langword="null" /> to clear it.</param>
    public void SetTimes(DateTimeOffset? creationTime, DateTimeOffset? modifiedTime)
    {
        _current.CreationTime = creationTime;
        _current.ModifiedTime = modifiedTime;
    }

    /// <summary>
    /// Sets the state bits of the most recently opened storage or written stream.
    /// </summary>
    /// <param name="stateBits">The state-bit flags to assign.</param>
    public void SetStateBits(int stateBits) =>
        _current.StateBits = stateBits;

    /// <summary>
    /// Serializes the authored structure to the destination. Subsequent calls have no effect.
    /// </summary>
    /// <exception cref="CompoundFileSerializationException">
    /// Thrown when the structure cannot be represented.
    /// </exception>
    public void Flush()
    {
        if (_flushed)
            return;

        _flushed = true;
        if (_stream is not null)
            CompoundContainerLayout.WriteTo(_stream, _root, _options);
        else
            _buffer!.Write(CompoundContainerLayout.Write(_root, _options));
    }

    /// <summary>
    /// Flushes any unwritten content and releases the writer.
    /// </summary>
    public void Dispose() =>
        Flush();
}

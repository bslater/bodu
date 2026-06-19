// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlWriter.Streaming.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Text.Toml.Serialization;

namespace Bodu.Text.Toml.Writer;

public ref partial struct Utf8TomlWriter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8TomlWriter" /> struct that writes the finished document to a
    /// stream.
    /// </summary>
    /// <param name="utf8Toml">The writable destination stream.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="utf8Toml" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="utf8Toml" /> does not support writing.
    /// </exception>
    public Utf8TomlWriter(Stream utf8Toml)
        : this(utf8Toml, default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8TomlWriter" /> struct that writes the finished document to a
    /// stream, using the supplied options.
    /// </summary>
    /// <param name="utf8Toml">The writable destination stream.</param>
    /// <param name="options">The writer options controlling the maximum nesting depth.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="utf8Toml" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="utf8Toml" /> does not support writing.
    /// </exception>
    /// <remarks>
    /// Rendered bytes are buffered when the root table is closed and reach the stream when <see cref="Flush" /> or
    /// <see cref="Dispose" /> is called; <see cref="BytesPending" /> reports the buffered count.
    /// </remarks>
    public Utf8TomlWriter(Stream utf8Toml, TomlWriterOptions options)
    {
        ThrowHelper.ThrowIfNull(utf8Toml);
        ThrowHelper.ThrowIfStreamNotWritable(utf8Toml);

        _stream = utf8Toml;
        _streamBuffer = new ArrayBufferWriter<byte>();
        _output = _streamBuffer;
        _frames = [];
        _root = new TomlWriterNode?[1];
        _maxDepth = options.MaxDepth <= 0 ? TomlLimits.AbsoluteMaxDepth : Math.Min(options.MaxDepth, TomlLimits.AbsoluteMaxDepth);
        _byteCounts = new long[2];
        _writeStack = new TomlWriteStack?[1];
    }

    /// <summary>
    /// Gets the number of bytes committed to the destination so far.
    /// </summary>
    /// <returns>
    /// For a buffer-writer destination, the bytes delivered when the root table was closed; for a stream destination,
    /// the bytes written to the stream by <see cref="Flush" />.
    /// </returns>
    public readonly long BytesCommitted => _byteCounts[0];

    /// <summary>
    /// Gets the number of rendered bytes buffered for a stream destination and not yet flushed.
    /// </summary>
    /// <returns>
    /// The pending byte count. Always zero for a buffer-writer destination, where bytes are delivered as the root table
    /// closes; for a stream destination the count becomes non-zero when the document completes and returns to zero on
    /// <see cref="Flush" />.
    /// </returns>
    public readonly long BytesPending => _byteCounts[1];

    /// <summary>
    /// Writes any buffered output to the destination stream and flushes it.
    /// </summary>
    /// <remarks>
    /// For a buffer-writer destination the method is a no-op: rendered bytes are delivered to the
    /// <see cref="IBufferWriter{T}" /> when the root table is closed.
    /// </remarks>
    public readonly void Flush()
    {
        if (_stream is null || _streamBuffer is null)
            return;

        if (_streamBuffer.WrittenCount > 0)
        {
            _stream.Write(_streamBuffer.WrittenSpan);
            _streamBuffer.Clear();
        }

        _stream.Flush();
        _byteCounts[0] += _byteCounts[1];
        _byteCounts[1] = 0;
    }

    /// <summary>
    /// Flushes any buffered output to the destination stream.
    /// </summary>
    /// <remarks>
    /// The writer holds no unmanaged resources and does not own the destination, so disposal only flushes; it may be
    /// called multiple times. The method enables the <see langword="using" /> pattern over the
    /// <see langword="ref struct" />.
    /// </remarks>
    public readonly void Dispose() => Flush();

    /// <summary>
    /// Resets the writer so a new document can be written to the same destination.
    /// </summary>
    /// <remarks>
    /// Open containers, the buffered value tree, any unflushed stream output, and the <see cref="BytesCommitted" /> /
    /// <see cref="BytesPending" /> counts are all cleared.
    /// </remarks>
    public readonly void Reset()
    {
        _frames.Clear();
        _root[0] = null;
        _writeStack[0] = null;
        _streamBuffer?.Clear();
        _byteCounts[0] = 0;
        _byteCounts[1] = 0;
    }
}

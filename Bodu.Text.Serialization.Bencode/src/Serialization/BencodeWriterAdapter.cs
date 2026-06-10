// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeWriterAdapter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Text;
using Bodu.Text.Serialization.Bencode.Syntax;

namespace Bodu.Text.Serialization.Bencode;

/// <summary>
/// Receives format-neutral tokens from the serialization engine and emits canonical Bencode bytes, buffering dictionary
/// entries so their keys can be sorted into ascending bytewise order as the grammar requires.
/// </summary>
/// <remarks>
/// Kinds the Bencode grammar cannot represent — Booleans, floating-point values, date-times, and nulls — are rejected
/// at the point they are written, unless a converter has already reduced the value to an integer or byte string.
/// </remarks>
internal sealed class BencodeWriterAdapter
    : ISerializationWriter
{
    /// <summary>
    /// The stack of open containers.
    /// </summary>
    private readonly Stack<Frame> _frames = new();

    /// <summary>
    /// The maximum permitted container nesting depth.
    /// </summary>
    private readonly int _maxDepth;

    /// <summary>
    /// The encoded top-level value, set once the document is complete.
    /// </summary>
    private byte[]? _root;

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeWriterAdapter" /> class.
    /// </summary>
    /// <param name="maxDepth">The maximum permitted container nesting depth.</param>
    internal BencodeWriterAdapter(int maxDepth)
    {
        _maxDepth = maxDepth;
    }

    /// <summary>
    /// Returns the encoded Bencode document.
    /// </summary>
    /// <returns>The canonical Bencode bytes.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no complete value has been written.</exception>
    internal byte[] ToByteArray() =>
        _root ?? throw new InvalidOperationException();

    /// <inheritdoc />
    public void WriteStartObject() => Push(new DictionaryFrame());

    /// <inheritdoc />
    public void WriteEndObject()
    {
        var frame = (DictionaryFrame)_frames.Pop();
        frame.Entries.Sort(static (left, right) => left.Key.AsSpan().SequenceCompareTo(right.Key));

        ArrayBufferWriter<byte> buffer = new();
        buffer.Write("d"u8);
        foreach ((byte[] key, byte[] value) in frame.Entries)
        {
            BencodeEncoding.WriteByteString(buffer, key);
            buffer.Write(value);
        }

        buffer.Write("e"u8);
        Emit(buffer.WrittenSpan.ToArray());
    }

    /// <inheritdoc />
    public void WriteStartArray() => Push(new ListFrame());

    /// <inheritdoc />
    public void WriteEndArray()
    {
        var frame = (ListFrame)_frames.Pop();

        ArrayBufferWriter<byte> buffer = new();
        buffer.Write("l"u8);
        foreach (byte[] item in frame.Items)
            buffer.Write(item);
        buffer.Write("e"u8);
        Emit(buffer.WrittenSpan.ToArray());
    }

    /// <inheritdoc />
    public void WritePropertyName(string name)
    {
        ThrowHelper.ThrowIfNull(name);
        ((DictionaryFrame)_frames.Peek()).PendingKey = Encoding.UTF8.GetBytes(name);
    }

    /// <inheritdoc />
    public void WriteString(string value)
    {
        ThrowHelper.ThrowIfNull(value);
        Emit(EncodeByteString(Encoding.UTF8.GetBytes(value)));
    }

    /// <inheritdoc />
    public void WriteBytes(ReadOnlySpan<byte> value) => Emit(EncodeByteString(value));

    /// <inheritdoc />
    public void WriteInt64(long value)
    {
        ArrayBufferWriter<byte> buffer = new();
        BencodeEncoding.WriteInteger(buffer, value);
        Emit(buffer.WrittenSpan.ToArray());
    }

    /// <inheritdoc />
    public void WriteUInt64(ulong value)
    {
        ArrayBufferWriter<byte> buffer = new();
        BencodeEncoding.WriteInteger(buffer, value);
        Emit(buffer.WrittenSpan.ToArray());
    }

    /// <inheritdoc />
    public void WriteDouble(double value) => throw Unsupported(SerializationValueKind.Double);

    /// <inheritdoc />
    public void WriteBoolean(bool value) => throw Unsupported(SerializationValueKind.Boolean);

    /// <inheritdoc />
    public void WriteNull() => throw Unsupported(SerializationValueKind.Null);

    /// <inheritdoc />
    public void WriteDateTimeOffset(DateTimeOffset value) => throw Unsupported(SerializationValueKind.DateTimeOffset);

    /// <inheritdoc />
    public void WriteDateTime(DateTime value) => throw Unsupported(SerializationValueKind.DateTime);

    /// <inheritdoc />
    public void WriteDateOnly(DateOnly value) => throw Unsupported(SerializationValueKind.DateOnly);

    /// <inheritdoc />
    public void WriteTimeOnly(TimeOnly value) => throw Unsupported(SerializationValueKind.TimeOnly);

    /// <summary>
    /// Encodes a byte string in canonical Bencode form.
    /// </summary>
    /// <param name="content">The raw bytes.</param>
    /// <returns>The encoded byte string.</returns>
    private static byte[] EncodeByteString(ReadOnlySpan<byte> content)
    {
        ArrayBufferWriter<byte> buffer = new();
        BencodeEncoding.WriteByteString(buffer, content);
        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Pushes a container frame, enforcing the maximum nesting depth.
    /// </summary>
    /// <param name="frame">The frame to push.</param>
    /// <exception cref="FormatSerializationException">Thrown when the depth exceeds the configured maximum.</exception>
    private void Push(Frame frame)
    {
        if (_frames.Count + 1 > _maxDepth)
            throw new FormatSerializationException(string.Format(CultureInfo.CurrentCulture, SerializationResourceStrings.Arg_OutOfRange_MaxDepthExceeded, _maxDepth));

        _frames.Push(frame);
    }

    /// <summary>
    /// Emits a fully encoded value to the enclosing container, or records it as the root when at the top level.
    /// </summary>
    /// <param name="encoded">The encoded value bytes.</param>
    private void Emit(byte[] encoded)
    {
        if (_frames.Count == 0)
        {
            _root = encoded;
            return;
        }

        _frames.Peek().AddValue(encoded);
    }

    /// <summary>
    /// Creates the exception thrown when a value kind cannot be represented in Bencode.
    /// </summary>
    /// <param name="kind">The unsupported value kind.</param>
    /// <returns>The exception to throw.</returns>
    private static NotSupportedException Unsupported(SerializationValueKind kind) =>
        new(string.Format(CultureInfo.CurrentCulture, BencodeSerializationResourceStrings.Op_NotSupported_BencodeValueKind, kind));

    /// <summary>
    /// The base for an open container frame.
    /// </summary>
    private abstract class Frame
    {
        /// <summary>
        /// Adds an encoded value to the frame.
        /// </summary>
        /// <param name="encoded">The encoded value bytes.</param>
        internal abstract void AddValue(byte[] encoded);
    }

    /// <summary>
    /// An open list frame collecting its encoded elements.
    /// </summary>
    private sealed class ListFrame
        : Frame
    {
        /// <summary>
        /// Gets the encoded elements, in order.
        /// </summary>
        /// <returns>The encoded elements.</returns>
        internal List<byte[]> Items { get; } = [];

        /// <inheritdoc />
        internal override void AddValue(byte[] encoded) => Items.Add(encoded);
    }

    /// <summary>
    /// An open dictionary frame collecting its entries and the pending key.
    /// </summary>
    private sealed class DictionaryFrame
        : Frame
    {
        /// <summary>
        /// Gets the entries, each pairing a raw key with its encoded value.
        /// </summary>
        /// <returns>The entries.</returns>
        internal List<(byte[] Key, byte[] Value)> Entries { get; } = [];

        /// <summary>
        /// Gets or sets the raw bytes of the key awaiting its value.
        /// </summary>
        /// <returns>The pending key, or <see langword="null" /> when none is pending.</returns>
        internal byte[]? PendingKey { get; set; }

        /// <inheritdoc />
        internal override void AddValue(byte[] encoded)
        {
            Entries.Add((PendingKey!, encoded));
            PendingKey = null;
        }
    }
}

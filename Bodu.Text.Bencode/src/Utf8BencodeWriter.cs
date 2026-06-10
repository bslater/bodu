// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeWriter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Buffers.Text;
using System.Text;

namespace Bodu.Text.Bencode;

/// <summary>
/// Provides a forward-only writer that emits canonical Bencode (BEP 3) bytes to an <see cref="IBufferWriter{T}" />,
/// mirroring the role of <see cref="System.Text.Json.Utf8JsonWriter" />. Because the grammar requires dictionary keys
/// in ascending bytewise order, the writer buffers each dictionary's entries and sorts them when the dictionary is
/// closed.
/// </summary>
/// <remarks>
/// The writer is a <see langword="ref struct" /> whose mutable state lives in shared managed buffers, so a copy taken
/// by value continues to write to the same output. Booleans, floating-point values, and date-times have no Bencode
/// representation and must be reduced to an integer or byte string by a converter before they are written.
/// </remarks>
public ref struct Utf8BencodeWriter
{
    /// <summary>
    /// The destination buffer writer that receives the completed document.
    /// </summary>
    private readonly IBufferWriter<byte> _output;

    /// <summary>
    /// The stack of open containers, innermost last.
    /// </summary>
    private readonly List<Frame> _frames;

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8BencodeWriter" /> struct.
    /// </summary>
    /// <param name="output">The destination buffer writer.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="output" /> is <see langword="null" />.
    /// </exception>
    public Utf8BencodeWriter(IBufferWriter<byte> output)
    {
        ThrowHelper.ThrowIfNull(output);

        _output = output;
        _frames = [];
    }

    /// <summary>
    /// Writes the start of a list.
    /// </summary>
    public readonly void WriteStartList() => _frames.Add(new ListFrame());

    /// <summary>
    /// Writes the end of the current list.
    /// </summary>
    public readonly void WriteEndList()
    {
        var frame = (ListFrame)Pop();

        var buffer = new ArrayBufferWriter<byte>();
        buffer.Write("l"u8);
        foreach (var item in frame.Items)
            buffer.Write(item);
        buffer.Write("e"u8);
        Emit(buffer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Writes the start of a dictionary.
    /// </summary>
    public readonly void WriteStartDictionary() => _frames.Add(new DictionaryFrame());

    /// <summary>
    /// Writes the end of the current dictionary, emitting its entries in ascending bytewise key order.
    /// </summary>
    public readonly void WriteEndDictionary()
    {
        var frame = (DictionaryFrame)Pop();
        frame.Entries.Sort(static (left, right) => left.Key.AsSpan().SequenceCompareTo(right.Key));

        var buffer = new ArrayBufferWriter<byte>();
        buffer.Write("d"u8);
        foreach (var (key, value) in frame.Entries)
        {
            WriteByteStringTo(buffer, key);
            buffer.Write(value);
        }

        buffer.Write("e"u8);
        Emit(buffer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Writes the name of the dictionary key whose value follows.
    /// </summary>
    /// <param name="name">The key bytes.</param>
    public readonly void WritePropertyName(ReadOnlySpan<byte> name) =>
        ((DictionaryFrame)_frames[^1]).PendingKey = name.ToArray();

    /// <summary>
    /// Writes the name of the dictionary key whose value follows, encoding the name as UTF-8.
    /// </summary>
    /// <param name="name">The key text.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    public readonly void WritePropertyName(string name)
    {
        ThrowHelper.ThrowIfNull(name);
        WritePropertyName(Encoding.UTF8.GetBytes(name));
    }

    /// <summary>
    /// Writes an integer value.
    /// </summary>
    /// <param name="value">The integer value.</param>
    public readonly void WriteInteger(long value)
    {
        var buffer = new ArrayBufferWriter<byte>();
        buffer.Write("i"u8);
        WriteAsciiInt64(buffer, value);
        buffer.Write("e"u8);
        Emit(buffer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Writes a byte-string value.
    /// </summary>
    /// <param name="value">The byte-string content.</param>
    public readonly void WriteByteString(ReadOnlySpan<byte> value)
    {
        var buffer = new ArrayBufferWriter<byte>();
        WriteByteStringTo(buffer, value);
        Emit(buffer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Writes a string value, encoding it as a UTF-8 byte string.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public readonly void WriteString(string value)
    {
        ThrowHelper.ThrowIfNull(value);
        WriteByteString(Encoding.UTF8.GetBytes(value));
    }

    /// <summary>
    /// Writes a byte string in canonical Bencode form to the supplied buffer.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="content">The byte-string content.</param>
    private static void WriteByteStringTo(IBufferWriter<byte> buffer, ReadOnlySpan<byte> content)
    {
        WriteAsciiInt64(buffer, content.Length);
        buffer.Write(":"u8);
        buffer.Write(content);
    }

    /// <summary>
    /// Writes the base-ten ASCII representation of an integer.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="value">The value to write.</param>
    private static void WriteAsciiInt64(IBufferWriter<byte> buffer, long value)
    {
        Span<byte> digits = stackalloc byte[20];
        _ = Utf8Formatter.TryFormat(value, digits, out var written);
        buffer.Write(digits[..written]);
    }

    /// <summary>
    /// Pops the innermost open container.
    /// </summary>
    /// <returns>The popped frame.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no container is open.</exception>
    private readonly Frame Pop()
    {
        if (_frames.Count == 0)
            throw new InvalidOperationException();

        var frame = _frames[^1];
        _frames.RemoveAt(_frames.Count - 1);
        return frame;
    }

    /// <summary>
    /// Emits a completed value to the enclosing container, or writes it to the output at the top level.
    /// </summary>
    /// <param name="encoded">The encoded value bytes.</param>
    private readonly void Emit(byte[] encoded)
    {
        if (_frames.Count == 0)
            _output.Write(encoded);
        else
            _frames[^1].AddValue(encoded);
    }

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

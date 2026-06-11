// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeWriter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Buffers.Text;
using System.Globalization;
using System.Text;

namespace Bodu.Text.Bencode.Writer;

/// <summary>
/// Provides a forward-only writer that emits canonical Bencode (BEP 3) bytes to an <see cref="IBufferWriter{T}" />,
/// mirroring the role of <see cref="System.Text.Json.Utf8JsonWriter" />. Because the grammar requires dictionary keys
/// in ascending bytewise order, the writer buffers each dictionary's entries and sorts them when the dictionary is
/// closed.
/// </summary>
/// <remarks>
/// <para>
/// The writer is a <see langword="ref struct" /> whose mutable state lives in shared managed buffers, so a copy taken
/// by value continues to write to the same output. Booleans, floating-point values, and date-times have no Bencode
/// representation and must be reduced to an integer or byte string by a converter before they are written.
/// </para>
/// <para>
/// The writer validates the call sequence against the canonical grammar: a property name may only be written inside
/// an open dictionary, every dictionary value must follow a property name, container ends must match the open
/// container kind, and a dictionary containing duplicate keys is rejected when it is closed.
/// </para>
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
    /// The maximum permitted container nesting depth.
    /// </summary>
    /// <remarks>
    /// The field is <see langword="readonly" /> and assigned once at construction. The writer is a
    /// <see langword="ref struct" /> passed by value with its mutable state held in the shared managed
    /// <see cref="_frames" /> and <see cref="_output" />; a <see langword="readonly" /> field set at construction is
    /// therefore identical across every by-value copy, whereas a mutable value field would not propagate through
    /// copies.
    /// </remarks>
    private readonly int _maxDepth;

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
        _maxDepth = 256;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8BencodeWriter" /> struct using the supplied options.
    /// </summary>
    /// <param name="output">The destination buffer writer.</param>
    /// <param name="options">The writer options controlling the maximum nesting depth.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="output" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// A <see cref="BencodeWriterOptions.MaxDepth" /> of zero or less selects the default maximum depth of 256.
    /// </remarks>
    public Utf8BencodeWriter(IBufferWriter<byte> output, BencodeWriterOptions options)
    {
        ThrowHelper.ThrowIfNull(output);

        _output = output;
        _frames = [];
        _maxDepth = options.MaxDepth <= 0 ? 256 : options.MaxDepth;
    }

    /// <summary>
    /// Writes the start of a list.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the list is opened directly inside a dictionary before a property name has been written.
    /// </exception>
    /// <exception cref="BencodeSerializationException">
    /// Thrown when opening the list would exceed the configured maximum nesting depth.
    /// </exception>
    public readonly void WriteStartList()
    {
        EnsureValueAllowed();
        if (_frames.Count >= _maxDepth)
            throw new BencodeSerializationException(string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_WriterMaxDepthExceeded, _maxDepth));

        _frames.Add(new ListFrame());
    }

    /// <summary>
    /// Writes the end of the current list.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no container is open, or when the currently open container is not a list.
    /// </exception>
    public readonly void WriteEndList()
    {
        if (_frames.Count == 0)
            throw new InvalidOperationException(BencodeResourceStrings.Op_Invalid_WriterNoOpenContainer);
        if (_frames[^1] is not ListFrame frame)
            throw new InvalidOperationException(BencodeResourceStrings.Op_Invalid_WriterEndContainerMismatch);

        _frames.RemoveAt(_frames.Count - 1);

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
    /// <exception cref="InvalidOperationException">
    /// Thrown when the dictionary is opened directly inside a dictionary before a property name has been written.
    /// </exception>
    /// <exception cref="BencodeSerializationException">
    /// Thrown when opening the dictionary would exceed the configured maximum nesting depth.
    /// </exception>
    public readonly void WriteStartDictionary()
    {
        EnsureValueAllowed();
        if (_frames.Count >= _maxDepth)
            throw new BencodeSerializationException(string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_WriterMaxDepthExceeded, _maxDepth));

        _frames.Add(new DictionaryFrame());
    }

    /// <summary>
    /// Writes the end of the current dictionary, emitting its entries in ascending bytewise key order.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no container is open, when the currently open container is not a dictionary, or when a property
    /// name is still awaiting its value.
    /// </exception>
    /// <exception cref="BencodeSerializationException">
    /// Thrown when the dictionary contains more than one entry for the same key, which canonical Bencode forbids.
    /// </exception>
    public readonly void WriteEndDictionary()
    {
        if (_frames.Count == 0)
            throw new InvalidOperationException(BencodeResourceStrings.Op_Invalid_WriterNoOpenContainer);
        if (_frames[^1] is not DictionaryFrame frame)
            throw new InvalidOperationException(BencodeResourceStrings.Op_Invalid_WriterEndContainerMismatch);
        if (frame.PendingKey is not null)
            throw new InvalidOperationException(BencodeResourceStrings.Op_Invalid_WriterPropertyNameWithoutValue);

        _frames.RemoveAt(_frames.Count - 1);
        frame.Entries.Sort(static (left, right) => left.Key.AsSpan().SequenceCompareTo(right.Key));

        var buffer = new ArrayBufferWriter<byte>();
        buffer.Write("d"u8);
        byte[]? previousKey = null;
        foreach (var (key, value) in frame.Entries)
        {
            // Equal keys are adjacent after the canonical sort, so a single neighbour comparison detects duplicates.
            if (previousKey is not null && previousKey.AsSpan().SequenceEqual(key))
                throw new BencodeSerializationException(string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_WriterDuplicateDictionaryKey, Encoding.UTF8.GetString(key)));

            previousKey = key;
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
    /// <exception cref="InvalidOperationException">
    /// Thrown when the currently open container is not a dictionary, or when a previously written property name is
    /// still awaiting its value.
    /// </exception>
    public readonly void WritePropertyName(ReadOnlySpan<byte> name)
    {
        if (_frames.Count == 0 || _frames[^1] is not DictionaryFrame frame)
            throw new InvalidOperationException(BencodeResourceStrings.Op_Invalid_WriterPropertyNameNotAllowed);
        if (frame.PendingKey is not null)
            throw new InvalidOperationException(BencodeResourceStrings.Op_Invalid_WriterPropertyNamePending);

        frame.PendingKey = name.ToArray();
    }

    /// <summary>
    /// Writes the name of the dictionary key whose value follows, encoding the name as UTF-8.
    /// </summary>
    /// <param name="name">The key text.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the currently open container is not a dictionary, or when a previously written property name is
    /// still awaiting its value.
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
    /// <exception cref="InvalidOperationException">
    /// Thrown when the value is written directly inside a dictionary before a property name has been written.
    /// </exception>
    public readonly void WriteInteger(long value)
    {
        EnsureValueAllowed();

        var buffer = new ArrayBufferWriter<byte>();
        buffer.Write("i"u8);
        WriteAsciiInt64(buffer, value);
        buffer.Write("e"u8);
        Emit(buffer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Writes an unsigned integer value, permitting the full <see cref="ulong" /> range.
    /// </summary>
    /// <param name="value">The unsigned integer value.</param>
    /// <remarks>
    /// Bencode integers are arbitrary-precision in BEP 3, so values between <see cref="long.MaxValue" /> and
    /// <see cref="ulong.MaxValue" /> are valid documents even though they exceed the writer's signed 64-bit overload. A
    /// reader consuming such a value must use <see cref="Reader.Utf8BencodeReader.GetUInt64" /> rather than
    /// <see cref="Reader.Utf8BencodeReader.GetInt64" />.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the value is written directly inside a dictionary before a property name has been written.
    /// </exception>
    public readonly void WriteInteger(ulong value)
    {
        EnsureValueAllowed();

        var buffer = new ArrayBufferWriter<byte>();
        buffer.Write("i"u8);
        WriteAsciiUInt64(buffer, value);
        buffer.Write("e"u8);
        Emit(buffer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Writes a byte-string value.
    /// </summary>
    /// <param name="value">The byte-string content.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the value is written directly inside a dictionary before a property name has been written.
    /// </exception>
    public readonly void WriteByteString(ReadOnlySpan<byte> value)
    {
        EnsureValueAllowed();

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
    /// <exception cref="InvalidOperationException">
    /// Thrown when the value is written directly inside a dictionary before a property name has been written.
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
    /// Writes the base-ten ASCII representation of an unsigned integer.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="value">The value to write.</param>
    private static void WriteAsciiUInt64(IBufferWriter<byte> buffer, ulong value)
    {
        Span<byte> digits = stackalloc byte[20];
        _ = Utf8Formatter.TryFormat(value, digits, out var written);
        buffer.Write(digits[..written]);
    }

    /// <summary>
    /// Enforces that a value may begin at the current position, rejecting a value written directly inside a
    /// dictionary before its property name.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the currently open container is a dictionary with no property name awaiting a value.
    /// </exception>
    private readonly void EnsureValueAllowed()
    {
        if (_frames.Count > 0 && _frames[^1] is DictionaryFrame { PendingKey: null })
            throw new InvalidOperationException(BencodeResourceStrings.Op_Invalid_WriterValueWithoutPropertyName);
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

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeWriter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Buffers.Text;
using System.Globalization;
using System.Text;
using Bodu.Text.Bencode.Serialization;

namespace Bodu.Text.Bencode.Writer;

/// <summary>
/// Provides a forward-only writer that emits canonical Bencode (BEP 3) bytes to an <see cref="IBufferWriter{T}" />.
/// Because the grammar requires dictionary keys in ascending bytewise order, the writer buffers each dictionary's
/// entries and sorts them when the dictionary is closed; values at the root and inside lists stream directly to the
/// destination.
/// </summary>
/// <remarks>
/// <para>
/// The writer is a <see langword="ref struct" /> whose mutable state lives in shared managed buffers, so a copy taken
/// by value continues to write to the same output. Booleans, floating-point values, and date-times have no Bencode
/// representation and must be reduced to an integer or byte string by a converter before they are written.
/// </para>
/// <para>
/// The writer validates the call sequence against the canonical grammar: a property name may only be written inside an
/// open dictionary, every dictionary value must follow a property name, container ends must match the open container
/// kind, a dictionary containing duplicate keys is rejected when it is closed, and a second root value is rejected
/// unless <see cref="BencodeWriterOptions.AllowMultipleRootValues" /> is set.
/// </para>
/// <para>
/// Scalars and lists written outside any dictionary are emitted to the destination as they are written; bytes that
/// belong to an open dictionary are held in that dictionary's buffer until it closes, because the canonical key order
/// is only known at that point. A consequence of streaming is that a write failing partway through a document leaves
/// the bytes already emitted in the destination — discard the destination when any write throws, and assert
/// <see cref="CurrentDepth" /> is zero before consuming it.
/// </para>
/// </remarks>
public ref struct Utf8BencodeWriter
{
    /// <summary>The destination buffer writer that receives the completed document.</summary>
    private readonly IBufferWriter<byte> _output;

    /// <summary>The shared mutable state — the container stack — that survives by-value copies of the writer.</summary>
    private readonly List<Frame> _frames;

    /// <summary>The maximum permitted container nesting depth.</summary>
    /// <remarks>
    /// The field is <see langword="readonly" /> and assigned once at construction. The writer is a
    /// <see langword="ref struct" /> passed by value with its mutable state held in the shared managed
    /// <see cref="_frames" /> and <see cref="_output" />; a <see langword="readonly" /> field set at construction is
    /// therefore identical across every by-value copy, whereas a mutable value field would not propagate through
    /// copies.
    /// </remarks>
    private readonly int _maxDepth;

    /// <summary>Whether the writer accepts more than one value at the top level.</summary>
    private readonly bool _allowMultipleRootValues;

    /// <summary>The shared root-completion flag, held on the heap so by-value copies of the writer observe the same document state.</summary>
    private readonly RootState _root;

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
        _maxDepth = BencodeLimits.AbsoluteMaxDepth;
        _allowMultipleRootValues = false;
        _root = new RootState();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8BencodeWriter" /> struct using the supplied options.
    /// </summary>
    /// <param name="output">The destination buffer writer.</param>
    /// <param name="options">The writer options controlling the maximum nesting depth and root-value policy.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="output" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// A <see cref="BencodeWriterOptions.MaxDepth" /> of zero or less selects the default maximum depth of 64, and a
    /// larger value is clamped to <see cref="BencodeLimits.AbsoluteMaxDepth" /> so that an unbounded configured value
    /// cannot drive the writer into a <see cref="StackOverflowException" />. Opening a list or dictionary past that
    /// depth throws <see cref="BencodeSerializationException" />.
    /// </remarks>
    public Utf8BencodeWriter(IBufferWriter<byte> output, BencodeWriterOptions options)
    {
        ThrowHelper.ThrowIfNull(output);

        _output = output;
        _frames = [];
        _maxDepth = options.MaxDepth <= 0 ? BencodeLimits.AbsoluteMaxDepth : Math.Min(options.MaxDepth, BencodeLimits.AbsoluteMaxDepth);
        _allowMultipleRootValues = options.AllowMultipleRootValues;
        _root = new RootState();
    }

    /// <summary>
    /// Gets the current container nesting depth.
    /// </summary>
    /// <returns>The number of open containers, where zero means the writer is at the document root.</returns>
    /// <remarks>
    /// A document is complete only when the depth has returned to zero: an unclosed dictionary's bytes never reach the
    /// destination, and an unclosed list leaves the output without its terminating <c>e</c>. Callers driving the writer
    /// manually can assert this property is zero before consuming the destination.
    /// </remarks>
    public readonly int CurrentDepth =>
        _frames.Count;

    /// <summary>
    /// Gets the customizations the writer was created with.
    /// </summary>
    /// <returns>
    /// The effective options, with <see cref="BencodeWriterOptions.MaxDepth" /> carrying the resolved value rather than
    /// a zero placeholder.
    /// </returns>
    public readonly BencodeWriterOptions Options =>
        new()
        {
            MaxDepth = _maxDepth,
            AllowMultipleRootValues = _allowMultipleRootValues,
        };

    /// <summary>
    /// Gets the effective maximum container nesting depth this writer enforces, after clamping to
    /// <see cref="BencodeLimits.AbsoluteMaxDepth" />.
    /// </summary>
    /// <returns>The effective maximum depth.</returns>
    internal readonly int EffectiveMaxDepth =>
        _maxDepth;

    /// <summary>
    /// Gets the serializer write state attached to this writer, or <see langword="null" /> when the writer is used
    /// directly rather than by the serializer.
    /// </summary>
    /// <returns>The attached <see cref="BencodeWriteStack" />, or <see langword="null" />.</returns>
    internal readonly BencodeWriteStack? WriteStack =>
        _root.WriteStack;

    /// <summary>
    /// Attaches the serializer write state to this writer so the converters can record an over-deep graph cooperatively
    /// instead of throwing from deep in the recursion.
    /// </summary>
    /// <param name="state">The write state to attach.</param>
    internal readonly void AttachWriteStack(BencodeWriteStack state) =>
        _root.WriteStack = state;

    /// <summary>
    /// Writes the start of a list.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the list is opened directly inside a dictionary before a property name has been written, or when a
    /// complete root value has already been written and <see cref="BencodeWriterOptions.AllowMultipleRootValues" /> is
    /// not set.
    /// </exception>
    /// <exception cref="BencodeSerializationException">
    /// Thrown when opening the list would exceed the configured maximum nesting depth.
    /// </exception>
    public readonly void WriteStartList()
    {
        EnsureValueAllowed();
        if (_frames.Count >= _maxDepth)
            throw new BencodeSerializationException(string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_WriterMaxDepthExceeded, _maxDepth));

        IBufferWriter<byte> sink = CurrentSink();
        sink.Write("l"u8);
        _frames.Add(new ListFrame(sink));
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
        frame.ContentSink.Write("e"u8);
        CompleteValue();
    }

    /// <summary>
    /// Writes the start of a dictionary.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the dictionary is opened directly inside a dictionary before a property name has been written, or
    /// when a complete root value has already been written and
    /// <see cref="BencodeWriterOptions.AllowMultipleRootValues" /> is not set.
    /// </exception>
    /// <exception cref="BencodeSerializationException">
    /// Thrown when opening the dictionary would exceed the configured maximum nesting depth.
    /// </exception>
    /// <remarks>
    /// No bytes are emitted until the dictionary closes: entries must be reordered into ascending bytewise key order,
    /// so the dictionary's content accumulates in a private buffer that <see cref="WriteEndDictionary" /> flushes.
    /// </remarks>
    public readonly void WriteStartDictionary()
    {
        EnsureValueAllowed();
        if (_frames.Count >= _maxDepth)
            throw new BencodeSerializationException(string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_WriterMaxDepthExceeded, _maxDepth));

        _frames.Add(new DictionaryFrame(CurrentSink()));
    }

    /// <summary>
    /// Writes the end of the current dictionary, emitting its entries in ascending bytewise key order.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no container is open, when the currently open container is not a dictionary, or when a property name
    /// is still awaiting its value.
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

        // Validate before emitting: no byte may reach the sink until the dictionary is known to be canonical, so a
        // duplicate-key failure cannot leak a partial document into the caller's destination. Equal keys are
        // adjacent after the canonical sort, so a single neighbour comparison detects duplicates.
        byte[]? previousKey = null;
        foreach (DictionaryEntry entry in frame.Entries)
        {
            if (previousKey is not null && previousKey.AsSpan().SequenceEqual(entry.Key))
                throw new BencodeSerializationException(string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_WriterDuplicateDictionaryKey, Encoding.UTF8.GetString(entry.Key)));

            previousKey = entry.Key;
        }

        IBufferWriter<byte> sink = frame.Sink;
        ReadOnlySpan<byte> values = frame.Buffer.WrittenSpan;
        sink.Write("d"u8);
        foreach (DictionaryEntry entry in frame.Entries)
        {
            WriteByteStringTo(sink, entry.Key);
            sink.Write(values.Slice(entry.ValueStart, entry.ValueLength));
        }

        sink.Write("e"u8);
        CompleteValue();
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
        frame.PendingValueStart = frame.Buffer.WrittenCount;
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
    /// Writes the name of the dictionary key whose value follows, encoding the characters as UTF-8.
    /// </summary>
    /// <param name="name">The key text.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the currently open container is not a dictionary, or when a previously written property name is
    /// still awaiting its value.
    /// </exception>
    public readonly void WritePropertyName(ReadOnlySpan<char> name)
    {
        byte[] utf8 = new byte[Encoding.UTF8.GetByteCount(name)];
        _ = Encoding.UTF8.GetBytes(name, utf8);
        WritePropertyName((ReadOnlySpan<byte>)utf8);
    }

    /// <summary>
    /// Writes an integer value.
    /// </summary>
    /// <param name="value">The integer value.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the value is written directly inside a dictionary before a property name has been written, or when a
    /// complete root value has already been written and <see cref="BencodeWriterOptions.AllowMultipleRootValues" /> is
    /// not set.
    /// </exception>
    public readonly void WriteInteger(long value)
    {
        EnsureValueAllowed();

        IBufferWriter<byte> sink = CurrentSink();
        sink.Write("i"u8);
        WriteAsciiInt64(sink, value);
        sink.Write("e"u8);
        CompleteValue();
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
    /// Thrown when the value is written directly inside a dictionary before a property name has been written, or when a
    /// complete root value has already been written and <see cref="BencodeWriterOptions.AllowMultipleRootValues" /> is
    /// not set.
    /// </exception>
    public readonly void WriteInteger(ulong value)
    {
        EnsureValueAllowed();

        IBufferWriter<byte> sink = CurrentSink();
        sink.Write("i"u8);
        WriteAsciiUInt64(sink, value);
        sink.Write("e"u8);
        CompleteValue();
    }

    /// <summary>
    /// Writes a byte-string value.
    /// </summary>
    /// <param name="value">The byte-string content.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the value is written directly inside a dictionary before a property name has been written, or when a
    /// complete root value has already been written and <see cref="BencodeWriterOptions.AllowMultipleRootValues" /> is
    /// not set.
    /// </exception>
    public readonly void WriteByteString(ReadOnlySpan<byte> value)
    {
        EnsureValueAllowed();

        WriteByteStringTo(CurrentSink(), value);
        CompleteValue();
    }

    /// <summary>
    /// Writes a string value, encoding it as a UTF-8 byte string.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the value is written directly inside a dictionary before a property name has been written, or when a
    /// complete root value has already been written and <see cref="BencodeWriterOptions.AllowMultipleRootValues" /> is
    /// not set.
    /// </exception>
    public readonly void WriteString(string value)
    {
        ThrowHelper.ThrowIfNull(value);
        WriteByteString(Encoding.UTF8.GetBytes(value));
    }

    /// <summary>
    /// Writes a property name and the start of a list as its value, combining <see cref="WritePropertyName(string)" />
    /// and <see cref="WriteStartList()" />.
    /// </summary>
    /// <param name="name">The key text.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the currently open container is not a dictionary, or when a previously written property name is
    /// still awaiting its value.
    /// </exception>
    /// <exception cref="BencodeSerializationException">
    /// Thrown when opening the list would exceed the configured maximum nesting depth.
    /// </exception>
    public readonly void WriteStartList(string name)
    {
        WritePropertyName(name);
        WriteStartList();
    }

    /// <summary>
    /// Writes a property name and the start of a list as its value.
    /// </summary>
    /// <param name="name">The key bytes.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the currently open container is not a dictionary, or when a previously written property name is
    /// still awaiting its value.
    /// </exception>
    /// <exception cref="BencodeSerializationException">
    /// Thrown when opening the list would exceed the configured maximum nesting depth.
    /// </exception>
    public readonly void WriteStartList(ReadOnlySpan<byte> name)
    {
        WritePropertyName(name);
        WriteStartList();
    }

    /// <summary>
    /// Writes a property name and the start of a dictionary as its value, combining
    /// <see cref="WritePropertyName(string)" /> and <see cref="WriteStartDictionary()" />.
    /// </summary>
    /// <param name="name">The key text.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the currently open container is not a dictionary, or when a previously written property name is
    /// still awaiting its value.
    /// </exception>
    /// <exception cref="BencodeSerializationException">
    /// Thrown when opening the dictionary would exceed the configured maximum nesting depth.
    /// </exception>
    public readonly void WriteStartDictionary(string name)
    {
        WritePropertyName(name);
        WriteStartDictionary();
    }

    /// <summary>
    /// Writes a property name and the start of a dictionary as its value.
    /// </summary>
    /// <param name="name">The key bytes.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the currently open container is not a dictionary, or when a previously written property name is
    /// still awaiting its value.
    /// </exception>
    /// <exception cref="BencodeSerializationException">
    /// Thrown when opening the dictionary would exceed the configured maximum nesting depth.
    /// </exception>
    public readonly void WriteStartDictionary(ReadOnlySpan<byte> name)
    {
        WritePropertyName(name);
        WriteStartDictionary();
    }

    /// <summary>
    /// Writes a property name and an integer value in one call.
    /// </summary>
    /// <param name="name">The key text.</param>
    /// <param name="value">The integer value.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the currently open container is not a dictionary, or when a previously written property name is
    /// still awaiting its value.
    /// </exception>
    public readonly void WriteInteger(string name, long value)
    {
        WritePropertyName(name);
        WriteInteger(value);
    }

    /// <summary>
    /// Writes a property name and an integer value in one call.
    /// </summary>
    /// <param name="name">The key bytes.</param>
    /// <param name="value">The integer value.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the currently open container is not a dictionary, or when a previously written property name is
    /// still awaiting its value.
    /// </exception>
    public readonly void WriteInteger(ReadOnlySpan<byte> name, long value)
    {
        WritePropertyName(name);
        WriteInteger(value);
    }

    /// <summary>
    /// Writes a property name and an unsigned integer value in one call, permitting the full <see cref="ulong" />
    /// range.
    /// </summary>
    /// <param name="name">The key text.</param>
    /// <param name="value">The unsigned integer value.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the currently open container is not a dictionary, or when a previously written property name is
    /// still awaiting its value.
    /// </exception>
    public readonly void WriteInteger(string name, ulong value)
    {
        WritePropertyName(name);
        WriteInteger(value);
    }

    /// <summary>
    /// Writes a property name and an unsigned integer value in one call, permitting the full <see cref="ulong" />
    /// range.
    /// </summary>
    /// <param name="name">The key bytes.</param>
    /// <param name="value">The unsigned integer value.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the currently open container is not a dictionary, or when a previously written property name is
    /// still awaiting its value.
    /// </exception>
    public readonly void WriteInteger(ReadOnlySpan<byte> name, ulong value)
    {
        WritePropertyName(name);
        WriteInteger(value);
    }

    /// <summary>
    /// Writes a property name and a byte-string value in one call.
    /// </summary>
    /// <param name="name">The key text.</param>
    /// <param name="value">The byte-string content.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the currently open container is not a dictionary, or when a previously written property name is
    /// still awaiting its value.
    /// </exception>
    public readonly void WriteByteString(string name, ReadOnlySpan<byte> value)
    {
        WritePropertyName(name);
        WriteByteString(value);
    }

    /// <summary>
    /// Writes a property name and a byte-string value in one call.
    /// </summary>
    /// <param name="name">The key bytes.</param>
    /// <param name="value">The byte-string content.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the currently open container is not a dictionary, or when a previously written property name is
    /// still awaiting its value.
    /// </exception>
    public readonly void WriteByteString(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        WritePropertyName(name);
        WriteByteString(value);
    }

    /// <summary>
    /// Writes a property name and a string value in one call, encoding both as UTF-8.
    /// </summary>
    /// <param name="name">The key text.</param>
    /// <param name="value">The string value.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> or <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the currently open container is not a dictionary, or when a previously written property name is
    /// still awaiting its value.
    /// </exception>
    public readonly void WriteString(string name, string value)
    {
        ThrowHelper.ThrowIfNull(value);

        WritePropertyName(name);
        WriteString(value);
    }

    /// <summary>
    /// Writes a property name and a string value in one call, encoding the value as UTF-8.
    /// </summary>
    /// <param name="name">The key bytes.</param>
    /// <param name="value">The string value.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the currently open container is not a dictionary, or when a previously written property name is
    /// still awaiting its value.
    /// </exception>
    public readonly void WriteString(ReadOnlySpan<byte> name, string value)
    {
        ThrowHelper.ThrowIfNull(value);

        WritePropertyName(name);
        WriteString(value);
    }

    /// <summary>
    /// Writes a pre-encoded Bencode value verbatim. Supports round-tripping verified slices such as a torrent's
    /// <c>info</c> dictionary without re-encoding.
    /// </summary>
    /// <param name="value">The complete, already-encoded Bencode value bytes.</param>
    /// <param name="skipInputValidation">
    /// <see langword="true" /> to write the bytes without verifying they form a single complete Bencode value.
    /// </param>
    /// <exception cref="BencodeFormatException">
    /// Thrown when validation is enabled and <paramref name="value" /> is not exactly one complete, canonical Bencode
    /// value.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the value is written directly inside a dictionary before a property name has been written, or when a
    /// complete root value has already been written and <see cref="BencodeWriterOptions.AllowMultipleRootValues" /> is
    /// not set.
    /// </exception>
    /// <remarks>
    /// Validation parses <paramref name="value" /> with a standalone <see cref="Reader.Utf8BencodeReader" /> using the
    /// default maximum depth; the payload's nesting is not counted against this writer's configured
    /// <see cref="BencodeWriterOptions.MaxDepth" />. When validation is skipped the caller is responsible for the bytes
    /// being canonical — non-canonical bytes produce a document this library's reader rejects.
    /// </remarks>
    public readonly void WriteRawValue(ReadOnlySpan<byte> value, bool skipInputValidation = false)
    {
        if (!skipInputValidation)
            ValidateRawValue(value);

        EnsureValueAllowed();
        CurrentSink().Write(value);
        CompleteValue();
    }

    /// <summary>
    /// Verifies that the supplied bytes form exactly one complete Bencode value.
    /// </summary>
    /// <param name="value">The bytes to validate.</param>
    /// <exception cref="BencodeFormatException">
    /// Thrown when the bytes are empty, malformed, or carry trailing data after the first value.
    /// </exception>
    private static void ValidateRawValue(ReadOnlySpan<byte> value)
    {
        var reader = new Reader.Utf8BencodeReader(value);
        if (!reader.Read())
            throw new BencodeFormatException(BencodeResourceStrings.Format_Invalid_BencodeUnexpectedEndOfData, 0);

        // Skip consumes the whole subtree when the first token opens a container; the trailing Read then enforces
        // that nothing follows the single root value.
        reader.Skip();
        _ = reader.Read();
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
        _ = Utf8Formatter.TryFormat(value, digits, out int written);
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
        _ = Utf8Formatter.TryFormat(value, digits, out int written);
        buffer.Write(digits[..written]);
    }

    /// <summary>
    /// Resolves the buffer the next value's bytes are written to: the innermost open dictionary's entry buffer, a
    /// list's captured sink, or the destination output at the root.
    /// </summary>
    /// <returns>The buffer writer that receives the next value's bytes.</returns>
    private readonly IBufferWriter<byte> CurrentSink() =>
        _frames.Count == 0
            ? _output
            : _frames[^1] is DictionaryFrame dictionary
                ? dictionary.Buffer
                : ((ListFrame)_frames[^1]).ContentSink;

    /// <summary>
    /// Enforces that a value may begin at the current position, rejecting a value written directly inside a dictionary
    /// before its property name and a second root value when multiple roots are not permitted; a value beginning at the
    /// root marks the document's single root as written.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the currently open container is a dictionary with no property name awaiting a value, or when a
    /// complete root value has already been written and <see cref="BencodeWriterOptions.AllowMultipleRootValues" /> is
    /// not set.
    /// </exception>
    private readonly void EnsureValueAllowed()
    {
        if (_frames.Count == 0)
        {
            if (_root.HasRootValue && !_allowMultipleRootValues)
                throw new InvalidOperationException(BencodeResourceStrings.Op_Invalid_WriterMultipleRootValues);

            _root.HasRootValue = true;
            return;
        }

        if (_frames[^1] is DictionaryFrame { PendingKey: null })
            throw new InvalidOperationException(BencodeResourceStrings.Op_Invalid_WriterValueWithoutPropertyName);
    }

    /// <summary>
    /// Records the completion of a value: when the enclosing container is a dictionary, the pending key and the byte
    /// range the value occupies in the dictionary's buffer become a finished entry.
    /// </summary>
    private readonly void CompleteValue()
    {
        if (_frames.Count > 0 && _frames[^1] is DictionaryFrame { PendingKey: not null } frame)
        {
            frame.Entries.Add(new DictionaryEntry(frame.PendingKey, frame.PendingValueStart, frame.Buffer.WrittenCount - frame.PendingValueStart));
            frame.PendingKey = null;
        }
    }

    /// <summary>
    /// Holds the document-level state shared by every by-value copy of the writer.
    /// </summary>
    private sealed class RootState
    {
        /// <summary>
        /// Gets or sets a value indicating whether a root value has been started or completed.
        /// </summary>
        /// <returns><see langword="true" /> once the document's root value has begun.</returns>
        internal bool HasRootValue { get; set; }

        /// <summary>
        /// Gets or sets the serializer write state, when the writer is driven by the serializer; otherwise
        /// <see langword="null" />.
        /// </summary>
        /// <returns>The attached write state, or <see langword="null" />.</returns>
        internal BencodeWriteStack? WriteStack { get; set; }
    }

    /// <summary>
    /// The base for an open container frame.
    /// </summary>
    private abstract class Frame
    {
    }

    /// <summary>
    /// An open list frame. Lists need no reordering, so their content streams straight through to the sink captured
    /// when the list was opened.
    /// </summary>
    private sealed class ListFrame
        : Frame
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListFrame" /> class.
        /// </summary>
        /// <param name="contentSink">The buffer that receives the list's bytes.</param>
        internal ListFrame(IBufferWriter<byte> contentSink)
        {
            ContentSink = contentSink;
        }

        /// <summary>
        /// Gets the buffer that receives the list's bytes — the innermost enclosing dictionary's buffer, or the
        /// destination output when no dictionary is open above the list.
        /// </summary>
        /// <returns>The list's content sink.</returns>
        internal IBufferWriter<byte> ContentSink { get; }
    }

    /// <summary>
    /// An open dictionary frame. Entry values accumulate in a single buffer with an offset table so the entries can be
    /// emitted in canonical key order when the dictionary closes, without re-copying each value separately.
    /// </summary>
    private sealed class DictionaryFrame
        : Frame
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DictionaryFrame" /> class.
        /// </summary>
        /// <param name="sink">The buffer that receives the dictionary's emission when it closes.</param>
        internal DictionaryFrame(IBufferWriter<byte> sink)
        {
            Sink = sink;
        }

        /// <summary>
        /// Gets the buffer that receives the dictionary's emission when it closes.
        /// </summary>
        /// <returns>The dictionary's destination sink.</returns>
        internal IBufferWriter<byte> Sink { get; }

        /// <summary>
        /// Gets the buffer holding the encoded bytes of every entry value, in write order.
        /// </summary>
        /// <returns>The dictionary's value buffer.</returns>
        internal ArrayBufferWriter<byte> Buffer { get; } = new();

        /// <summary>
        /// Gets the completed entries, each pairing a raw key with the byte range its value occupies in
        /// <see cref="Buffer" />.
        /// </summary>
        /// <returns>The completed entries.</returns>
        internal List<DictionaryEntry> Entries { get; } = [];

        /// <summary>
        /// Gets or sets the raw bytes of the key awaiting its value.
        /// </summary>
        /// <returns>The pending key, or <see langword="null" /> when none is pending.</returns>
        internal byte[]? PendingKey { get; set; }

        /// <summary>
        /// Gets or sets the offset within <see cref="Buffer" /> where the pending key's value begins.
        /// </summary>
        /// <returns>The pending value's start offset.</returns>
        internal int PendingValueStart { get; set; }
    }

    /// <summary>
    /// A completed dictionary entry: a raw key and the byte range its encoded value occupies in the owning dictionary
    /// frame's buffer.
    /// </summary>
    private readonly struct DictionaryEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DictionaryEntry" /> struct.
        /// </summary>
        /// <param name="key">The raw key bytes.</param>
        /// <param name="valueStart">The value's start offset within the dictionary frame's buffer.</param>
        /// <param name="valueLength">The value's byte length.</param>
        internal DictionaryEntry(byte[] key, int valueStart, int valueLength)
        {
            Key = key;
            ValueStart = valueStart;
            ValueLength = valueLength;
        }

        /// <summary>
        /// Gets the raw key bytes.
        /// </summary>
        /// <returns>The key bytes.</returns>
        internal byte[] Key { get; }

        /// <summary>
        /// Gets the value's start offset within the dictionary frame's buffer.
        /// </summary>
        /// <returns>The value's start offset.</returns>
        internal int ValueStart { get; }

        /// <summary>
        /// Gets the value's byte length.
        /// </summary>
        /// <returns>The value's byte length.</returns>
        internal int ValueLength { get; }
    }
}

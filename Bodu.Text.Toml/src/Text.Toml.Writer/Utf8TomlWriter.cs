// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlWriter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;

namespace Bodu.Text.Toml.Writer;

/// <summary>
/// Provides a forward-only writer that emits canonical TOML bytes to an <see cref="IBufferWriter{T}" />. Because TOML's
/// surface layout is a whole-document property, the writer buffers every value into an in-memory tree and serializes it
/// when the root table is closed.
/// </summary>
/// <remarks>
/// <para>
/// The writer is a <see langword="ref struct" /> whose mutable state lives in shared managed objects — a stack of open
/// containers and the buffered value tree — so a copy taken by value continues to write to the same output. Whether a
/// table becomes a <c>[header]</c> block or an inline <c>{ … }</c> depends on where it sits in the finished document,
/// and arrays are inline, so the layout cannot be decided incrementally: the forward <c>Write*</c> calls only build the
/// tree.
/// </para>
/// <para>
/// Canonical emission happens once, when the outermost table is closed by the root <see cref="WriteEndTable" />. A
/// table's scalar and array members are written first as <c>key = value</c> lines, then its sub-tables as
/// <c>[dotted.path]</c> block headers (depth-first, in document order); an array whose every element is a table is
/// emitted as a run of <c>[[path]]</c> blocks. Arrays of non-table values are inline (<c>[1, 2, 3]</c>), and a table
/// that appears as an array element is an inline table (<c>{ a = 1, b = 2 }</c>). Keys are bare when they match the
/// bare-key grammar and basic-quoted otherwise; strings are basic-quoted with escaping; floats render <c>inf</c>,
/// <c>-inf</c>, and <c>nan</c> and otherwise use their shortest round-trippable spelling; date-times use the RFC 3339
/// form matching their kind.
/// </para>
/// </remarks>
public ref partial struct Utf8TomlWriter
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
    /// The single-element holder that receives the completed root node when the outermost container is closed.
    /// </summary>
    /// <remarks>
    /// The holder is a shared managed object so that the result of the final <see cref="Emit" /> survives a by-value
    /// copy of the writer, just as the <see cref="_frames" /> stack does.
    /// </remarks>
    private readonly TomlWriterNode?[] _root;

    /// <summary>
    /// The maximum permitted container nesting depth.
    /// </summary>
    /// <remarks>
    /// The field is <see langword="readonly" /> and assigned once at construction. The writer is a
    /// <see langword="ref struct" /> passed by value with its mutable state held in the shared managed
    /// <see cref="_frames" />, <see cref="_root" />, and <see cref="_output" />; a <see langword="readonly" /> field
    /// set at construction is therefore identical across every by-value copy, whereas a mutable value field would not
    /// propagate through copies.
    /// </remarks>
    private readonly int _maxDepth;

    /// <summary>
    /// The destination stream when the writer was constructed over a <see cref="Stream" />, with the buffered output
    /// awaiting <see cref="Flush" />; otherwise <see langword="null" />.
    /// </summary>
    private readonly Stream? _stream;

    /// <summary>
    /// The intermediate buffer that receives rendered bytes in stream mode until they are flushed.
    /// </summary>
    private readonly ArrayBufferWriter<byte>? _streamBuffer;

    /// <summary>
    /// The shared committed/pending byte counters (committed at index 0, pending at index 1), held in an array so the
    /// counts survive a by-value copy of the writer.
    /// </summary>
    private readonly long[] _byteCounts;

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8TomlWriter" /> struct.
    /// </summary>
    /// <param name="output">The destination buffer writer.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="output" /> is <see langword="null" />.
    /// </exception>
    public Utf8TomlWriter(IBufferWriter<byte> output)
    {
        ThrowHelper.ThrowIfNull(output);

        _output = output;
        _frames = [];
        _root = new TomlWriterNode?[1];
        _maxDepth = 256;
        _byteCounts = new long[2];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8TomlWriter" /> struct using the supplied options.
    /// </summary>
    /// <param name="output">The destination buffer writer.</param>
    /// <param name="options">
    /// The writer options controlling the specification version and maximum nesting depth.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="output" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// A <see cref="TomlWriterOptions.MaxDepth" /> of zero or less selects the default maximum depth of 256.
    /// </remarks>
    public Utf8TomlWriter(IBufferWriter<byte> output, TomlWriterOptions options)
    {
        ThrowHelper.ThrowIfNull(output);

        _output = output;
        _frames = [];
        _root = new TomlWriterNode?[1];
        _maxDepth = options.MaxDepth <= 0 ? 256 : options.MaxDepth;
        _byteCounts = new long[2];
    }

    /// <summary>
    /// Writes the start of a table.
    /// </summary>
    /// <exception cref="TomlSerializationException">
    /// Thrown when opening the table would exceed the configured maximum nesting depth.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the document is already complete, or when the enclosing container is a table with no pending
    /// property name.
    /// </exception>
    public readonly void WriteStartTable()
    {
        ThrowIfComplete();
        ThrowIfValueNeedsPropertyName();
        if (_frames.Count >= _maxDepth)
            throw new TomlSerializationException(string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_WriterMaxDepthExceeded, _maxDepth));

        _frames.Add(new TableFrame());
    }

    /// <summary>
    /// Writes the end of the current table.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no container is open, the innermost open container is not a table, or the table has a pending
    /// property name without a value.
    /// </exception>
    /// <remarks>
    /// Closing the outermost table serializes the buffered value tree to canonical TOML and writes the resulting UTF-8
    /// bytes to the destination buffer writer.
    /// </remarks>
    public readonly void WriteEndTable()
    {
        if (_frames.Count == 0)
            throw new InvalidOperationException(TomlResourceStrings.Op_Invalid_WriterNoOpenContainer);
        if (_frames[^1] is not TableFrame frame)
            throw new InvalidOperationException(TomlResourceStrings.Op_Invalid_WriterEndMismatch);
        if (frame.PendingKey is not null)
            throw new InvalidOperationException(TomlResourceStrings.Op_Invalid_WriterPropertyNameWithoutValue);

        _frames.RemoveAt(_frames.Count - 1);
        Emit(frame.Table);
    }

    /// <summary>
    /// Writes the start of an array.
    /// </summary>
    /// <exception cref="TomlSerializationException">
    /// Thrown when opening the array would exceed the configured maximum nesting depth.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the document is already complete, when the array would become the document root (the root of a TOML
    /// document must be a table), or when the enclosing container is a table with no pending property name.
    /// </exception>
    public readonly void WriteStartArray()
    {
        ThrowIfComplete();
        if (_frames.Count == 0)
            throw new InvalidOperationException(TomlResourceStrings.Op_Invalid_WriterRootMustBeTable);
        ThrowIfValueNeedsPropertyName();
        if (_frames.Count >= _maxDepth)
            throw new TomlSerializationException(string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_WriterMaxDepthExceeded, _maxDepth));

        _frames.Add(new ArrayFrame());
    }

    /// <summary>
    /// Writes the end of the current array.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no container is open or the innermost open container is not an array.
    /// </exception>
    public readonly void WriteEndArray()
    {
        if (_frames.Count == 0)
            throw new InvalidOperationException(TomlResourceStrings.Op_Invalid_WriterNoOpenContainer);
        if (_frames[^1] is not ArrayFrame frame)
            throw new InvalidOperationException(TomlResourceStrings.Op_Invalid_WriterEndMismatch);

        _frames.RemoveAt(_frames.Count - 1);
        Emit(frame.Array);
    }

    /// <summary>
    /// Writes the name of the table key whose value follows.
    /// </summary>
    /// <param name="name">The key text.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name" /> contains an unpaired surrogate.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the document is already complete, the innermost open container is not a table, another property
    /// name is already pending, or <paramref name="name" /> has already been written to the current table.
    /// </exception>
    public readonly void WritePropertyName(string name)
    {
        ThrowHelper.ThrowIfNull(name);
        ThrowIfUnpairedSurrogate(name);
        ThrowIfComplete();
        if (_frames.Count == 0 || _frames[^1] is not TableFrame frame)
            throw new InvalidOperationException(TomlResourceStrings.Op_Invalid_WriterPropertyNameOutsideTable);
        if (frame.PendingKey is not null)
            throw new InvalidOperationException(TomlResourceStrings.Op_Invalid_WriterPropertyNamePending);
        if (frame.Table.ContainsKey(name))
            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_WriterDuplicateKey, name));

        frame.PendingKey = name;
    }

    /// <summary>
    /// Writes a string value.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value" /> contains an unpaired surrogate.
    /// </exception>
    public readonly void WriteString(string value)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowIfUnpairedSurrogate(value);

        Emit(new TomlScalarWriterNode(TomlTokenType.String, value));
    }

    /// <summary>
    /// Writes a 64-bit signed integer value.
    /// </summary>
    /// <param name="value">The integer value.</param>
    public readonly void WriteInteger(long value) =>
        Emit(new TomlScalarWriterNode(TomlTokenType.Integer, value));

    /// <summary>
    /// Writes an IEEE 754 binary64 floating-point value.
    /// </summary>
    /// <param name="value">The floating-point value.</param>
    public readonly void WriteFloat(double value) =>
        Emit(new TomlScalarWriterNode(TomlTokenType.Float, value));

    /// <summary>
    /// Writes a Boolean value.
    /// </summary>
    /// <param name="value">The Boolean value.</param>
    public readonly void WriteBoolean(bool value) =>
        Emit(new TomlScalarWriterNode(TomlTokenType.Boolean, value));

    /// <summary>
    /// Writes an offset date-time value in RFC 3339 form.
    /// </summary>
    /// <param name="value">The offset date-time value.</param>
    public readonly void WriteOffsetDateTime(DateTimeOffset value) =>
        Emit(new TomlScalarWriterNode(TomlTokenType.OffsetDateTime, value));

    /// <summary>
    /// Writes a local date-time value in RFC 3339 form, without any offset.
    /// </summary>
    /// <param name="value">The local date-time value.</param>
    public readonly void WriteLocalDateTime(DateTime value) =>
        Emit(new TomlScalarWriterNode(TomlTokenType.LocalDateTime, value));

    /// <summary>
    /// Writes a local date value in RFC 3339 form.
    /// </summary>
    /// <param name="value">The local date value.</param>
    public readonly void WriteLocalDate(DateOnly value) =>
        Emit(new TomlScalarWriterNode(TomlTokenType.LocalDate, value));

    /// <summary>
    /// Writes a local time value in RFC 3339 form.
    /// </summary>
    /// <param name="value">The local time value.</param>
    public readonly void WriteLocalTime(TimeOnly value) =>
        Emit(new TomlScalarWriterNode(TomlTokenType.LocalTime, value));

    /// <summary>
    /// Throws when the document root has already been completed.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the document is complete.</exception>
    private readonly void ThrowIfComplete()
    {
        if (_root[0] is not null)
            throw new InvalidOperationException(TomlResourceStrings.Op_Invalid_WriterDocumentComplete);
    }

    /// <summary>
    /// Throws when the innermost open container is a table with no pending property name, so a value cannot legally be
    /// added to it.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the enclosing table has no pending property name.
    /// </exception>
    private readonly void ThrowIfValueNeedsPropertyName()
    {
        if (_frames.Count > 0 && _frames[^1] is TableFrame { PendingKey: null })
            throw new InvalidOperationException(TomlResourceStrings.Op_Invalid_WriterValueWithoutKey);
    }

    /// <summary>
    /// Throws when <paramref name="value" /> contains an unpaired surrogate, which cannot be encoded as UTF-8.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value" /> contains an unpaired surrogate.
    /// </exception>
    private static void ThrowIfUnpairedSurrogate(string value, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (char.IsHighSurrogate(c))
            {
                if (i + 1 >= value.Length || !char.IsLowSurrogate(value[i + 1]))
                    throw new ArgumentException(TomlResourceStrings.Arg_Invalid_TomlUnpairedSurrogate, paramName);

                i++;
                continue;
            }

            if (char.IsLowSurrogate(c))
                throw new ArgumentException(TomlResourceStrings.Arg_Invalid_TomlUnpairedSurrogate, paramName);
        }
    }

    /// <summary>
    /// Emits a completed value to the enclosing container, or records it as the root and serializes the document when
    /// the outermost container has been closed.
    /// </summary>
    /// <param name="node">The completed value node.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the document is already complete, or when the completed value would become a non-table document
    /// root.
    /// </exception>
    private readonly void Emit(TomlWriterNode node)
    {
        if (_frames.Count == 0)
        {
            ThrowIfComplete();
            if (node is not TomlTableWriterNode table)
                throw new InvalidOperationException(TomlResourceStrings.Op_Invalid_WriterRootMustBeTable);

            _root[0] = node;
            Serialize(table);
            return;
        }

        _frames[^1].AddValue(node);
    }

    /// <summary>
    /// Serializes the completed root table to canonical TOML, emitting the UTF-8 bytes directly to the destination
    /// buffer writer without an intermediate document-sized buffer.
    /// </summary>
    /// <param name="root">The completed root table.</param>
    private readonly void Serialize(TomlTableWriterNode root)
    {
        var emitter = new TomlUtf8Emitter(_output);
        TomlCanonicalWriter.WriteTableBody(ref emitter, root, []);
        emitter.Complete();

        // In buffer-writer mode the bytes were delivered to the caller's writer; in stream mode they await Flush.
        if (_stream is null)
            _byteCounts[0] += emitter.BytesWritten;
        else
            _byteCounts[1] += emitter.BytesWritten;
    }

    /// <summary>
    /// The base for an open container frame.
    /// </summary>
    private abstract class Frame
    {
        /// <summary>
        /// Adds a completed value to the frame.
        /// </summary>
        /// <param name="node">The value node.</param>
        internal abstract void AddValue(TomlWriterNode node);
    }

    /// <summary>
    /// An open table frame building its key/value pairs and tracking the pending key.
    /// </summary>
    private sealed class TableFrame
        : Frame
    {
        /// <summary>
        /// Gets the table being built.
        /// </summary>
        /// <returns>The table.</returns>
        internal TomlTableWriterNode Table { get; } = new();

        /// <summary>
        /// Gets or sets the key awaiting its value.
        /// </summary>
        /// <returns>The pending key, or <see langword="null" /> when none is pending.</returns>
        internal string? PendingKey { get; set; }

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">
        /// Thrown when no property name is pending for the value.
        /// </exception>
        internal override void AddValue(TomlWriterNode node)
        {
            if (PendingKey is not { } key)
                throw new InvalidOperationException(TomlResourceStrings.Op_Invalid_WriterValueWithoutKey);

            Table.Add(key, node);
            PendingKey = null;
        }
    }

    /// <summary>
    /// An open array frame building its elements.
    /// </summary>
    private sealed class ArrayFrame
        : Frame
    {
        /// <summary>
        /// Gets the array being built.
        /// </summary>
        /// <returns>The array.</returns>
        internal TomlArrayWriterNode Array { get; } = new();

        /// <inheritdoc />
        internal override void AddValue(TomlWriterNode node) => Array.Add(node);
    }
}

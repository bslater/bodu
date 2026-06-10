// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlWriter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Text;

namespace Bodu.Text.Toml.Writer;

/// <summary>
/// Provides a forward-only writer that emits canonical TOML bytes to an <see cref="IBufferWriter{T}" />, mirroring the
/// role of <see cref="System.Text.Json.Utf8JsonWriter" />. Because TOML's surface layout is a whole-document property,
/// the writer buffers every value into an in-memory tree and serializes it when the root table is closed.
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
public ref struct Utf8TomlWriter
{
    /// <summary>
    /// The UTF-8 encoding used to emit the finished document; it omits a byte-order mark.
    /// </summary>
    private static readonly UTF8Encoding s_utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

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
    }

    /// <summary>
    /// Writes the start of a table.
    /// </summary>
    /// <exception cref="TomlSerializationException">
    /// Thrown when opening the table would exceed the configured maximum nesting depth.
    /// </exception>
    public readonly void WriteStartTable()
    {
        if (_frames.Count >= _maxDepth)
            throw new TomlSerializationException(string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_WriterMaxDepthExceeded, _maxDepth));

        _frames.Add(new TableFrame());
    }

    /// <summary>
    /// Writes the end of the current table.
    /// </summary>
    /// <remarks>
    /// Closing the outermost table serializes the buffered value tree to canonical TOML and writes the resulting UTF-8
    /// bytes to the destination buffer writer.
    /// </remarks>
    public readonly void WriteEndTable()
    {
        TomlTableWriterNode table = ((TableFrame)Pop()).Table;
        Emit(table);
    }

    /// <summary>
    /// Writes the start of an array.
    /// </summary>
    /// <exception cref="TomlSerializationException">
    /// Thrown when opening the array would exceed the configured maximum nesting depth.
    /// </exception>
    public readonly void WriteStartArray()
    {
        if (_frames.Count >= _maxDepth)
            throw new TomlSerializationException(string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_WriterMaxDepthExceeded, _maxDepth));

        _frames.Add(new ArrayFrame());
    }

    /// <summary>
    /// Writes the end of the current array.
    /// </summary>
    public readonly void WriteEndArray() => Emit(((ArrayFrame)Pop()).Array);

    /// <summary>
    /// Writes the name of the table key whose value follows.
    /// </summary>
    /// <param name="name">The key text.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    public readonly void WritePropertyName(string name)
    {
        ThrowHelper.ThrowIfNull(name);
        ((TableFrame)_frames[^1]).PendingKey = name;
    }

    /// <summary>
    /// Writes a string value.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public readonly void WriteString(string value)
    {
        ThrowHelper.ThrowIfNull(value);
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
    /// Pops the innermost open container.
    /// </summary>
    /// <returns>The popped frame.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no container is open.</exception>
    private readonly Frame Pop()
    {
        if (_frames.Count == 0)
            throw new InvalidOperationException();

        Frame frame = _frames[^1];
        _frames.RemoveAt(_frames.Count - 1);
        return frame;
    }

    /// <summary>
    /// Emits a completed value to the enclosing container, or records it as the root and serializes the document when
    /// the outermost container has been closed.
    /// </summary>
    /// <param name="node">The completed value node.</param>
    private readonly void Emit(TomlWriterNode node)
    {
        if (_frames.Count == 0)
        {
            _root[0] = node;
            if (node is TomlTableWriterNode table)
                Serialize(table);

            return;
        }

        _frames[^1].AddValue(node);
    }

    /// <summary>
    /// Serializes the completed root table to canonical TOML and writes the UTF-8 bytes to the destination buffer
    /// writer.
    /// </summary>
    /// <param name="root">The completed root table.</param>
    private readonly void Serialize(TomlTableWriterNode root)
    {
        StringBuilder builder = new();
        TomlCanonicalWriter.WriteTableBody(builder, root, []);

        _output.Write(s_utf8.GetBytes(builder.ToString()));
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
        internal override void AddValue(TomlWriterNode node)
        {
            Table.Add(PendingKey!, node);
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

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlWriterAdapter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Serialization.Toml.Syntax;

namespace Bodu.Text.Serialization.Toml;

/// <summary>
/// Receives format-neutral tokens from the serialization engine and builds a <see cref="TomlTableSyntax" /> document
/// model, which the writer then renders to canonical TOML text.
/// </summary>
/// <remarks>
/// The root value must be a table, because a TOML document's root is always a table. A byte array is rendered as a
/// base64 string, and the kinds TOML cannot represent — a null at the document or array level — are rejected.
/// </remarks>
internal sealed class TomlWriterAdapter
    : ISerializationWriter
{
    /// <summary>
    /// The stack of open containers.
    /// </summary>
    private readonly Stack<Frame> _frames = new();

    /// <summary>
    /// The completed root value, set once the document is complete.
    /// </summary>
    private TomlSyntaxNode? _root;

    /// <summary>
    /// Returns the completed root table, requiring the root value to be a table.
    /// </summary>
    /// <returns>The root table.</returns>
    /// <exception cref="FormatSerializationException">Thrown when the root value is not a table.</exception>
    internal TomlTableSyntax GetRootTable() =>
        _root as TomlTableSyntax
            ?? throw new FormatSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlSerializationResourceStrings.Op_Invalid_TomlRootNotTable, _root?.Kind.ToString() ?? "null"));

    /// <inheritdoc />
    public void WriteStartObject() => _frames.Push(new TableFrame());

    /// <inheritdoc />
    public void WriteEndObject() => Emit(((TableFrame)_frames.Pop()).Table);

    /// <inheritdoc />
    public void WriteStartArray() => _frames.Push(new ArrayFrame());

    /// <inheritdoc />
    public void WriteEndArray() => Emit(((ArrayFrame)_frames.Pop()).Array);

    /// <inheritdoc />
    public void WritePropertyName(string name)
    {
        ThrowHelper.ThrowIfNull(name);
        ((TableFrame)_frames.Peek()).PendingKey = name;
    }

    /// <inheritdoc />
    public void WriteString(string value)
    {
        ThrowHelper.ThrowIfNull(value);
        Emit(new TomlScalarSyntax(TomlSyntaxKind.String, value, default));
    }

    /// <inheritdoc />
    public void WriteBytes(ReadOnlySpan<byte> value) =>
        Emit(new TomlScalarSyntax(TomlSyntaxKind.String, Convert.ToBase64String(value), default));

    /// <inheritdoc />
    public void WriteInt64(long value) => Emit(new TomlScalarSyntax(TomlSyntaxKind.Integer, value, default));

    /// <inheritdoc />
    public void WriteUInt64(ulong value)
    {
        if (value > long.MaxValue)
            throw new FormatSerializationException(string.Format(CultureInfo.CurrentCulture, SerializationResourceStrings.Op_Invalid_IntegerOverflow, value, typeof(long)));
        Emit(new TomlScalarSyntax(TomlSyntaxKind.Integer, (long)value, default));
    }

    /// <inheritdoc />
    public void WriteDouble(double value) => Emit(new TomlScalarSyntax(TomlSyntaxKind.Float, value, default));

    /// <inheritdoc />
    public void WriteBoolean(bool value) => Emit(new TomlScalarSyntax(TomlSyntaxKind.Boolean, value, default));

    /// <inheritdoc />
    public void WriteNull()
    {
        if (_frames.Count > 0 && _frames.Peek() is ArrayFrame)
            throw new FormatSerializationException(TomlSerializationResourceStrings.Op_Invalid_TomlNullArrayElement);

        throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, TomlSerializationResourceStrings.Op_NotSupported_TomlValueKind, SerializationValueKind.Null));
    }

    /// <inheritdoc />
    public void WriteDateTimeOffset(DateTimeOffset value) => Emit(new TomlScalarSyntax(TomlSyntaxKind.OffsetDateTime, value, default));

    /// <inheritdoc />
    public void WriteDateTime(DateTime value) => Emit(new TomlScalarSyntax(TomlSyntaxKind.LocalDateTime, value, default));

    /// <inheritdoc />
    public void WriteDateOnly(DateOnly value) => Emit(new TomlScalarSyntax(TomlSyntaxKind.LocalDate, value, default));

    /// <inheritdoc />
    public void WriteTimeOnly(TimeOnly value) => Emit(new TomlScalarSyntax(TomlSyntaxKind.LocalTime, value, default));

    /// <summary>
    /// Emits a completed value to the enclosing container, or records it as the root at the top level.
    /// </summary>
    /// <param name="node">The completed value node.</param>
    private void Emit(TomlSyntaxNode node)
    {
        if (_frames.Count == 0)
        {
            _root = node;
            return;
        }

        _frames.Peek().Add(node);
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
        internal abstract void Add(TomlSyntaxNode node);
    }

    /// <summary>
    /// An open table frame building its key/value pairs.
    /// </summary>
    private sealed class TableFrame
        : Frame
    {
        /// <summary>
        /// Gets the table being built.
        /// </summary>
        /// <returns>The table.</returns>
        internal TomlTableSyntax Table { get; } = new();

        /// <summary>
        /// Gets or sets the key awaiting its value.
        /// </summary>
        /// <returns>The pending key, or <see langword="null" /> when none is pending.</returns>
        internal string? PendingKey { get; set; }

        /// <inheritdoc />
        internal override void Add(TomlSyntaxNode node)
        {
            Table[PendingKey!] = node;
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
        internal TomlArraySyntax Array { get; } = new();

        /// <inheritdoc />
        internal override void Add(TomlSyntaxNode node) => Array.Add(node);
    }
}

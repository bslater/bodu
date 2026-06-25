// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlReaderRow.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml.Reader;

/// <summary>
/// A single node in the flat, index-linked document store produced by the parser. Rows are appended in document order
/// and linked into a tree through child and sibling indices, so the store materializes the node graph without per-node
/// heap objects.
/// </summary>
internal struct YamlReaderRow
{
    /// <summary>The structural kind of the node.</summary>
    public YamlReaderNodeKind Kind;

    /// <summary>The resolved scalar value kind, meaningful only when <see cref="Kind" /> is a scalar.</summary>
    public YamlValueKind ValueKind;

    /// <summary>The presentation style of a scalar node.</summary>
    public YamlScalarStyle ScalarStyle;

    /// <summary>Structural markers describing how the row was produced.</summary>
    public YamlReaderRowFlags Flags;

    /// <summary>The packed scalar payload. Integers and booleans are stored directly, floating-point values as their raw bits, and strings as a packed source span (see <see cref="PackStringSpan" />).</summary>
    public long ScalarBits;

    /// <summary>The decoded key text when this row is a mapping value; <see langword="null" /> for sequence elements and the root.</summary>
    public string? Key;

    /// <summary>The anchor name attached to this node, or <see langword="null" /> when it is not anchored.</summary>
    public string? Anchor;

    /// <summary>The verbatim or resolved tag attached to this node, or <see langword="null" /> when none was given.</summary>
    public string? Tag;

    /// <summary>The zero-based byte offset in the source at which the node begins.</summary>
    public int Offset;

    /// <summary>The row index of the first child, or <c>-1</c> when the node has no children.</summary>
    public int FirstChild;

    /// <summary>The row index of the last child, kept for O(1) append during the build, or <c>-1</c> when empty.</summary>
    public int LastChild;

    /// <summary>The row index of the next sibling, or <c>-1</c> when this is the last child of its parent.</summary>
    public int NextSibling;

    /// <summary>For an alias row, the resolved target row index; otherwise <c>-1</c>.</summary>
    public int AliasTarget;

    /// <summary>The number of children: key/value pairs for a mapping, elements for a sequence.</summary>
    public int ChildCount;

    /// <summary>The nesting depth of the node, with the document root at zero.</summary>
    public int Depth;

    /// <summary>
    /// Gets the high 32 bits of <see cref="ScalarBits" /> as the start offset of a string scalar's source span.
    /// </summary>
    /// <value>The zero-based byte offset of the scalar's decoded-content span.</value>
    public readonly int StringContentStart => (int)(ScalarBits >> 32);

    /// <summary>
    /// Gets the low 32 bits of <see cref="ScalarBits" /> as the length of a string scalar's source span.
    /// </summary>
    /// <value>The byte length of the scalar's decoded-content span.</value>
    public readonly int StringContentLength => (int)(ScalarBits & 0xFFFFFFFF);

    /// <summary>
    /// Reads the scalar payload as a 64-bit integer.
    /// </summary>
    /// <returns>The integer value.</returns>
    public readonly long AsInt64() => ScalarBits;

    /// <summary>
    /// Reads the scalar payload as a double-precision floating-point value.
    /// </summary>
    /// <returns>The floating-point value.</returns>
    public readonly double AsDouble() => BitConverter.Int64BitsToDouble(ScalarBits);

    /// <summary>
    /// Reads the scalar payload as a boolean.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when the stored bits are non-zero; otherwise <see langword="false" />.
    /// </returns>
    public readonly bool AsBoolean() => ScalarBits != 0;

    /// <summary>
    /// Packs the start and length of a string scalar's source span into a single payload word.
    /// </summary>
    /// <param name="start">The zero-based byte offset of the span.</param>
    /// <param name="length">The byte length of the span.</param>
    /// <returns>The packed payload word.</returns>
    public static long PackStringSpan(int start, int length) =>
        ((long)(uint)start << 32) | (uint)length;
}

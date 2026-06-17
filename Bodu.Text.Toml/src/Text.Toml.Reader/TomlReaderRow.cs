// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlReaderRow.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Reader;

/// <summary>
/// A single node in the flat row store produced by <see cref="TomlDocumentBuilder" />: a table, an array, or a decoded
/// scalar. Children are held as an index-linked list (<see cref="FirstChild" /> … <see cref="NextSibling" />) rather
/// than a contiguous span, so the builder can append out-of-line <c>[table]</c> content to an already-recorded table
/// without shifting rows. The same store backs random-access navigation in <c>TomlDocument</c> and the depth-first
/// cursor in <see cref="TomlDocumentReader" />, so no separate tree or token list is materialized.
/// </summary>
/// <remarks>
/// No scalar holds a reference. A numeric, Boolean, or date/time scalar packs its value into <see cref="ScalarBits" />
/// (with <see cref="ScalarOffsetMinutes" /> for an offset date-time), and a string scalar packs its content's source
/// span into <see cref="ScalarBits" /> instead — decoded on demand from the retained source rather than at parse. The
/// <c>As…</c> value accessors and the <c>StringContent…</c> span accessors are the single counterpart to the packing
/// the builder performs, so the two cannot drift.
/// </remarks>
internal struct TomlReaderRow
{
    /// <summary>The kind of node: <see cref="TomlReaderNodeKind.Table" />, <see cref="TomlReaderNodeKind.Array" />, or <see cref="TomlReaderNodeKind.Scalar" />.</summary>
    public TomlReaderNodeKind Kind;

    /// <summary>For a scalar, the token type that classifies the value; unused for a table or array.</summary>
    public TomlTokenType TokenType;

    /// <summary>The scalar's packed payload, interpreted by <see cref="TokenType" />. A value-type scalar stores its value: the integer itself; the float reinterpreted with <see cref="BitConverter.DoubleToInt64Bits(double)" />; zero or one for a Boolean; the day number for a local date; or the tick count for a local time, a local date-time, or the local clock time of an offset date-time. A string scalar stores its content's source span, packed by <see cref="PackStringSpan(int, int, bool)" />. Unused for a table or array.</summary>
    public long ScalarBits;

    /// <summary>The key under which this row sits in its parent table, or <see langword="null" /> for an array element or the document root.</summary>
    public string? Key;

    /// <summary>The zero-based source byte offset at which the node begins.</summary>
    public int Offset;

    /// <summary>The row index of this container's first child, or <c>-1</c> when it has none.</summary>
    public int FirstChild;

    /// <summary>The row index of this container's last child, used to append in constant time during the build, or <c>-1</c>.</summary>
    public int LastChild;

    /// <summary>The row index of the sibling that follows this row within its parent, or <c>-1</c> when it is the last child.</summary>
    public int NextSibling;

    /// <summary>The number of children: key/value pairs for a table, elements for an array.</summary>
    public int ChildCount;

    /// <summary>The nesting depth of a table within its tree, where the document root is zero, used to bound nesting created by dotted keys and header paths.</summary>
    public int Depth;

    /// <summary>For an offset date-time scalar, the UTC offset in whole minutes; zero for every other kind.</summary>
    public short ScalarOffsetMinutes;

    /// <summary>The structural classifications recorded while building, used to enforce TOML's table rules.</summary>
    public TomlReaderRowFlags Flags;

    /// <summary>
    /// Decodes this integer scalar's value.
    /// </summary>
    /// <returns>The 64-bit signed integer.</returns>
    public readonly long AsInt64() =>
        ScalarBits;

    /// <summary>
    /// Decodes this float scalar's value.
    /// </summary>
    /// <returns>The IEEE 754 binary64 value.</returns>
    public readonly double AsDouble() =>
        BitConverter.Int64BitsToDouble(ScalarBits);

    /// <summary>
    /// Decodes this Boolean scalar's value.
    /// </summary>
    /// <returns>The Boolean value.</returns>
    public readonly bool AsBoolean() =>
        ScalarBits != 0;

    /// <summary>
    /// Decodes this local-date scalar's value.
    /// </summary>
    /// <returns>The local date.</returns>
    public readonly DateOnly AsDateOnly() =>
        DateOnly.FromDayNumber((int)ScalarBits);

    /// <summary>
    /// Decodes this local-time scalar's value.
    /// </summary>
    /// <returns>The local time.</returns>
    public readonly TimeOnly AsTimeOnly() =>
        new(ScalarBits);

    /// <summary>
    /// Decodes this local-date-time scalar's value.
    /// </summary>
    /// <returns>
    /// The local date-time, whose <see cref="DateTime.Kind" /> is <see cref="DateTimeKind.Unspecified" />.
    /// </returns>
    public readonly DateTime AsDateTime() =>
        new(ScalarBits, DateTimeKind.Unspecified);

    /// <summary>
    /// Decodes this offset-date-time scalar's value.
    /// </summary>
    /// <returns>The offset date-time.</returns>
    public readonly DateTimeOffset AsDateTimeOffset() =>
        new(ScalarBits, new TimeSpan(0, ScalarOffsetMinutes, 0));

    /// <summary>
    /// Gets the source byte offset at which this string scalar's content begins, inside its delimiters.
    /// </summary>
    /// <returns>
    /// The content start offset, paired with <see cref="StringContentLength" /> to slice the retained source.
    /// </returns>
    public readonly int StringContentStart =>
        (int)(ScalarBits >> 32);

    /// <summary>
    /// Gets the byte length of this string scalar's content.
    /// </summary>
    /// <returns>
    /// The content length, paired with <see cref="StringContentStart" /> to slice the retained source.
    /// </returns>
    public readonly int StringContentLength =>
        (int)((uint)ScalarBits & LengthMask);

    /// <summary>
    /// Gets a value indicating whether this string scalar's content carries escapes that decoding must resolve.
    /// </summary>
    /// <returns><see langword="true" /> when the content must be unescaped rather than transcoded directly.</returns>
    public readonly bool StringHasEscapes =>
        ((uint)ScalarBits & EscapeFlag) != 0;

    /// <summary>
    /// Packs a string scalar's content source span and escape state into the 64-bit <see cref="ScalarBits" /> payload.
    /// </summary>
    /// <param name="start">The source byte offset at which the content begins.</param>
    /// <param name="length">The content length in bytes, which must be less than 2^31.</param>
    /// <param name="hasEscapes">Whether the content carries escapes that decoding must resolve.</param>
    /// <returns>The packed payload: the start in the high 32 bits, the length and escape flag in the low 32.</returns>
    public static long PackStringSpan(int start, int length, bool hasEscapes) =>
        ((long)(uint)start << 32) | (uint)length | (hasEscapes ? EscapeFlag : 0U);

    /// <summary>The bit within the low 32 bits of <see cref="ScalarBits" /> that flags a string scalar's content as escaped.</summary>
    private const uint EscapeFlag = 0x8000_0000U;

    /// <summary>The mask selecting a string scalar's content length from the low 32 bits of <see cref="ScalarBits" />.</summary>
    private const uint LengthMask = 0x7FFF_FFFFU;
}

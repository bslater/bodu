// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeDocument.Row.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Document;

public sealed partial class BencodeDocument
{
    /// <summary>
    /// Represents a single entry in the flat metadata index that <see cref="BencodeDocument" /> builds when parsing.
    /// Each row describes one Bencode value — or one dictionary key — and carries enough information to navigate the
    /// document without re-reading the source, mirroring the database-row layout used internally by
    /// <see cref="System.Text.Json.JsonDocument" />.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Rows are stored in document order. A value's subtree occupies a contiguous run of rows beginning with the
    /// value's own row: an <see cref="BencodeValueKind.Array" /> is laid out as
    /// <c>[array row][element 0 subtree][element 1 subtree]…</c>, and an <see cref="BencodeValueKind.Object" /> as
    /// <c>[object row][key 0 row][value 0 subtree][key 1 row][value 1 subtree]…</c>, where each key is itself stored as
    /// a single <see cref="BencodeValueKind.ByteString" /> row.
    /// </para>
    /// <para>
    /// <see cref="NumberOfRows" /> records the length of that contiguous run, which is what allows a sibling to be
    /// located by skipping the whole of the preceding subtree in constant time per hop.
    /// </para>
    /// </remarks>
    private readonly struct Row
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Row" /> struct.
        /// </summary>
        /// <param name="kind">The kind of value the row represents.</param>
        /// <param name="location">The absolute byte offset of a byte string's content; otherwise zero.</param>
        /// <param name="length">The byte length of a byte string's content; otherwise zero.</param>
        /// <param name="integer">The decoded value of an integer; otherwise zero.</param>
        /// <param name="numberOfRows">The number of rows occupied by this value's subtree, including this row.</param>
        /// <param name="childCount">
        /// The element count of an array or the pair count of an object; otherwise zero.
        /// </param>
        /// <param name="rawLocation">The absolute byte offset of the value's complete encoded form.</param>
        /// <param name="rawLength">The byte length of the value's complete encoded form.</param>
        private Row(BencodeValueKind kind, int location, int length, long integer, int numberOfRows, int childCount, int rawLocation, int rawLength)
        {
            Kind = kind;
            Location = location;
            Length = length;
            Integer = integer;
            NumberOfRows = numberOfRows;
            ChildCount = childCount;
            RawLocation = rawLocation;
            RawLength = rawLength;
        }

        /// <summary>
        /// Gets the kind of value this row represents.
        /// </summary>
        /// <returns>The value kind.</returns>
        public BencodeValueKind Kind { get; }

        /// <summary>
        /// Gets the absolute byte offset of a byte string's content within the owning document's buffer.
        /// </summary>
        /// <returns>The content offset for a <see cref="BencodeValueKind.ByteString" /> row; otherwise zero.</returns>
        public int Location { get; }

        /// <summary>
        /// Gets the byte length of a byte string's content.
        /// </summary>
        /// <returns>The content length for a <see cref="BencodeValueKind.ByteString" /> row; otherwise zero.</returns>
        public int Length { get; }

        /// <summary>
        /// Gets the decoded value of an integer.
        /// </summary>
        /// <returns>The value for a <see cref="BencodeValueKind.Integer" /> row; otherwise zero.</returns>
        public long Integer { get; }

        /// <summary>
        /// Gets the number of rows occupied by this value's subtree, including this row.
        /// </summary>
        /// <returns>The subtree row count, which is always at least one.</returns>
        public int NumberOfRows { get; }

        /// <summary>
        /// Gets the number of direct children this value contains.
        /// </summary>
        /// <returns>
        /// The element count for a <see cref="BencodeValueKind.Array" /> row, the key/value pair count for a
        /// <see cref="BencodeValueKind.Object" /> row; otherwise zero.
        /// </returns>
        public int ChildCount { get; }

        /// <summary>
        /// Gets the absolute byte offset of the value's complete encoded form within the owning document's buffer,
        /// including the framing tokens (the <c>i…e</c>, length prefix, or container delimiters).
        /// </summary>
        /// <returns>The raw start offset.</returns>
        public int RawLocation { get; }

        /// <summary>
        /// Gets the byte length of the value's complete encoded form.
        /// </summary>
        /// <returns>The raw byte length.</returns>
        public int RawLength { get; }

        /// <summary>
        /// Creates a row describing an integer value.
        /// </summary>
        /// <param name="value">The decoded integer value.</param>
        /// <param name="rawLocation">The absolute byte offset of the encoded <c>i…e</c> token.</param>
        /// <param name="rawLength">The byte length of the encoded token.</param>
        /// <returns>A scalar row whose subtree is a single row.</returns>
        public static Row Int(long value, int rawLocation, int rawLength) =>
            new(BencodeValueKind.Integer, location: 0, length: 0, integer: value, numberOfRows: 1, childCount: 0, rawLocation: rawLocation, rawLength: rawLength);

        /// <summary>
        /// Creates a row describing a byte string, used both for byte-string values and for dictionary keys.
        /// </summary>
        /// <param name="location">The absolute byte offset of the content within the owning document's buffer.</param>
        /// <param name="length">The byte length of the content.</param>
        /// <param name="rawLocation">The absolute byte offset of the encoded form, including the length prefix.</param>
        /// <param name="rawLength">The byte length of the encoded form.</param>
        /// <returns>A scalar row whose subtree is a single row.</returns>
        public static Row Bytes(int location, int length, int rawLocation, int rawLength) =>
            new(BencodeValueKind.ByteString, location: location, length: length, integer: 0, numberOfRows: 1, childCount: 0, rawLocation: rawLocation, rawLength: rawLength);

        /// <summary>
        /// Creates a row describing a container, either an array or an object.
        /// </summary>
        /// <param name="kind">
        /// The container kind, either <see cref="BencodeValueKind.Array" /> or <see cref="BencodeValueKind.Object" />.
        /// </param>
        /// <param name="childCount">The element count for an array, or the pair count for an object.</param>
        /// <param name="numberOfRows">
        /// The number of rows occupied by the container's subtree, including the container row.
        /// </param>
        /// <param name="rawLocation">The absolute byte offset of the container's opening delimiter.</param>
        /// <param name="rawLength">The byte length of the encoded container, including both delimiters.</param>
        /// <returns>A container row spanning the supplied number of subtree rows.</returns>
        public static Row Container(BencodeValueKind kind, int childCount, int numberOfRows, int rawLocation, int rawLength) =>
            new(kind, location: 0, length: 0, integer: 0, numberOfRows: numberOfRows, childCount: childCount, rawLocation: rawLocation, rawLength: rawLength);
    }
}

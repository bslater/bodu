// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlDocument.Row.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Document;

public sealed partial class TomlDocument
{
    /// <summary>
    /// Represents a single entry in the flat metadata index that <see cref="TomlDocument" /> builds when parsing. Each
    /// row describes one TOML value — or one table key — and carries enough information to navigate the document
    /// without re-reading the source, mirroring the database-row layout used internally by
    /// <see cref="System.Text.Json.JsonDocument" />.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Rows are stored in document order. A value's subtree occupies a contiguous run of rows beginning with the
    /// value's own row: a <see cref="TomlValueKind.Array" /> is laid out as
    /// <c>[array row][element 0 subtree][element 1 subtree]…</c>, and a <see cref="TomlValueKind.Table" /> as
    /// <c>[table row][key 0 row][value 0 subtree][key 1 row][value 1 subtree]…</c>, where each key is itself stored as
    /// a single key row carrying the property name.
    /// </para>
    /// <para>
    /// <see cref="NumberOfRows" /> records the length of that contiguous run, which is what allows a sibling to be
    /// located by skipping the whole of the preceding subtree in constant time per hop.
    /// </para>
    /// <para>
    /// Because the underlying <see cref="Reader.TomlDocumentReader" /> decodes every scalar to its concrete CLR type during
    /// parsing, a scalar row stores that decoded value directly in <see cref="Value" /> — for example a
    /// <see cref="string" />, <see cref="long" />, <see cref="double" />, <see cref="bool" />,
    /// <see cref="DateTimeOffset" />, <see cref="DateTime" />, <see cref="DateOnly" />, or <see cref="TimeOnly" /> — so
    /// no copy of the source bytes is retained.
    /// </para>
    /// </remarks>
    private readonly struct Row
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Row" /> struct.
        /// </summary>
        /// <param name="kind">The kind of value the row represents.</param>
        /// <param name="value">
        /// The decoded scalar value for a scalar row, the key name for a key row, or <see langword="null" /> for a
        /// container row.
        /// </param>
        /// <param name="numberOfRows">The number of rows occupied by this value's subtree, including this row.</param>
        /// <param name="childCount">The element count of an array or the pair count of a table; otherwise zero.</param>
        private Row(TomlValueKind kind, object? value, int numberOfRows, int childCount)
        {
            Kind = kind;
            Value = value;
            NumberOfRows = numberOfRows;
            ChildCount = childCount;
        }

        /// <summary>
        /// Gets the kind of value this row represents.
        /// </summary>
        /// <returns>The value kind.</returns>
        public TomlValueKind Kind { get; }

        /// <summary>
        /// Gets the decoded scalar value, or the key name for a key row.
        /// </summary>
        /// <returns>
        /// The decoded CLR value for a scalar row, the key name as a <see cref="string" /> for a key row, or
        /// <see langword="null" /> for a <see cref="TomlValueKind.Array" /> or <see cref="TomlValueKind.Table" /> row.
        /// </returns>
        public object? Value { get; }

        /// <summary>
        /// Gets the number of rows occupied by this value's subtree, including this row.
        /// </summary>
        /// <returns>The subtree row count, which is always at least one.</returns>
        public int NumberOfRows { get; }

        /// <summary>
        /// Gets the number of direct children this value contains.
        /// </summary>
        /// <returns>
        /// The element count for a <see cref="TomlValueKind.Array" /> row, the key/value pair count for a
        /// <see cref="TomlValueKind.Table" /> row; otherwise zero.
        /// </returns>
        public int ChildCount { get; }

        /// <summary>
        /// Creates a row describing a decoded scalar value.
        /// </summary>
        /// <param name="kind">The scalar value kind.</param>
        /// <param name="value">The decoded CLR value of the scalar.</param>
        /// <returns>A scalar row whose subtree is a single row.</returns>
        public static Row Scalar(TomlValueKind kind, object? value) =>
            new(kind, value, numberOfRows: 1, childCount: 0);

        /// <summary>
        /// Creates a row describing a table key, used in key position immediately before the key's value subtree.
        /// </summary>
        /// <param name="name">The decoded key name.</param>
        /// <returns>A key row whose subtree is a single row.</returns>
        public static Row Key(string name) =>
            new(TomlValueKind.Table, name, numberOfRows: 1, childCount: 0);

        /// <summary>
        /// Creates a row describing a container, either an array or a table.
        /// </summary>
        /// <param name="kind">
        /// The container kind, either <see cref="TomlValueKind.Array" /> or <see cref="TomlValueKind.Table" />.
        /// </param>
        /// <param name="childCount">The element count for an array, or the pair count for a table.</param>
        /// <param name="numberOfRows">
        /// The number of rows occupied by the container's subtree, including the container row.
        /// </param>
        /// <returns>A container row spanning the supplied number of subtree rows.</returns>
        public static Row Container(TomlValueKind kind, int childCount, int numberOfRows) =>
            new(kind, value: null, numberOfRows: numberOfRows, childCount: childCount);
    }
}

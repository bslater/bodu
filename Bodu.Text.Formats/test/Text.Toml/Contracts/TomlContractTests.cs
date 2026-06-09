// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Formats.Contracts;

namespace Bodu.Text.Toml.Contracts;

/// <summary>
/// Drives <see cref="TextDocumentFormatContractTests{TDocument, TOptions}" /> against <see cref="Toml" /> with a
/// representative set of positive and negative vectors. The contract base verifies parse-equality, parse-failure
/// exception types, and the parse → format → parse round-trip — the shared text-format contract. TOML has no parser
/// options, so the options type parameter is a placeholder.
/// </summary>
[TestClass]
public sealed class TomlContractTests
    : TextDocumentFormatContractTests<TomlTable, object?>
{
    /// <inheritdoc />
    protected override TomlTable Parse(string text, object? options = default) =>
        Toml.Parse(text.AsSpan());

    /// <inheritdoc />
    protected override string Format(TomlTable document, object? options = default) =>
        Toml.Format(document);

    /// <inheritdoc />
    protected override IEqualityComparer<TomlTable> DocumentComparer { get; } = new StructuralComparer();

    /// <inheritdoc />
    protected override IReadOnlyList<TextDocumentKat<TomlTable, object?>> ValidCases { get; } =
    [
        new("empty document", string.Empty, new TomlTable()),
        new(
            "scalar key/values",
            "a = 1\nb = \"x\"\nc = true\nd = 3.5",
            new TomlTable { { "a", new TomlInteger(1) }, { "b", new TomlString("x") }, { "c", new TomlBoolean(true) }, { "d", new TomlFloat(3.5) } }),
        new(
            "nested table",
            "[t]\nx = 1",
            new TomlTable { { "t", new TomlTable { { "x", new TomlInteger(1) } } } }),
        new(
            "inline array",
            "a = [1, 2, 3]",
            new TomlTable { { "a", new TomlArray { new TomlInteger(1), new TomlInteger(2), new TomlInteger(3) } } }),
        new(
            "array of tables",
            "[[p]]\nn = 1\n[[p]]\nn = 2",
            new TomlTable { { "p", new TomlArray { new TomlTable { { "n", new TomlInteger(1) } }, new TomlTable { { "n", new TomlInteger(2) } } } } }),
    ];

    /// <inheritdoc />
    protected override IReadOnlyList<InvalidTextDocumentKat<object?>> InvalidCases { get; } =
    [
        new("duplicate key", "a = 1\na = 2", default, typeof(TomlFormatException)),
        new("duplicate table", "[a]\n[a]", default, typeof(TomlFormatException)),
        new("missing value", "key = ", default, typeof(TomlFormatException)),
        new("unterminated array", "a = [1, 2", default, typeof(TomlFormatException)),
    ];

    /// <summary>
    /// Compares two TOML documents by structural equality of their values, since <see cref="TomlTable" /> uses
    /// reference equality by default.
    /// </summary>
    private sealed class StructuralComparer
        : IEqualityComparer<TomlTable>
    {
        /// <inheritdoc />
        public bool Equals(TomlTable? x, TomlTable? y) =>
            ReferenceEquals(x, y) || (x is not null && y is not null && string.Equals(Normalize(x), Normalize(y), StringComparison.Ordinal));

        /// <inheritdoc />
        public int GetHashCode(TomlTable obj) =>
            Normalize(obj).GetHashCode(StringComparison.Ordinal);

        /// <summary>
        /// Reduces a value to a canonical, order-independent string.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The canonical representation.</returns>
        private static string Normalize(TomlValue value)
        {
            switch (value)
            {
                case TomlTable table:
                    var entries = new List<string>();
                    foreach (var pair in table)
                        entries.Add("<" + pair.Key + ">=" + Normalize(pair.Value));
                    entries.Sort(StringComparer.Ordinal);
                    return "{" + string.Join(",", entries) + "}";
                case TomlArray array:
                    var items = new List<string>();
                    foreach (var item in array)
                        items.Add(Normalize(item));
                    return "[" + string.Join(",", items) + "]";
                default:
                    return value.Kind + ":" + value;
            }
        }
    }
}

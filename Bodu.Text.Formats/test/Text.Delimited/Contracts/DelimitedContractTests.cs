// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Formats.Contracts;

namespace Bodu.Text.Delimited.Contracts;

/// <summary>
/// Drives <see cref="TextDocumentFormatContractTests{TDocument, TOptions}" /> against
/// <see cref="Delimited" /> with a minimal set of RFC 4180-shaped positive vectors and an unclosed-
/// quote negative vector. The contract base verifies parse-equality, parse-failure exception type,
/// and the parse → format → parse round-trip. Bespoke Delimited coverage (header handling, duplicate
/// headers, multi-byte options, reader/writer streams) lives in <c>DelimitedTests.*</c>.
/// </summary>
[TestClass]
public sealed class DelimitedContractTests
    : TextDocumentFormatContractTests<DelimitedDocument, DelimitedParseOptions>
{
    /// <inheritdoc />
    protected override DelimitedDocument Parse(string text, DelimitedParseOptions options = default!) =>
        Delimited.Parse(text.AsSpan(), options);

    /// <inheritdoc />
    protected override string Format(DelimitedDocument document, DelimitedParseOptions options = default!) =>
        Delimited.Format(document, options);

    /// <inheritdoc />
    protected override IEqualityComparer<DelimitedDocument> DocumentComparer { get; } = new RowFieldComparer();

    /// <inheritdoc />
    protected override IReadOnlyList<TextDocumentKat<DelimitedDocument, DelimitedParseOptions>> ValidCases { get; } =
        [
        new(
            Name: "single header row",
            Text: "name",
            ExpectedDocument: Delimited.Parse("name".AsSpan()),
            Options: DelimitedParseOptions.Default),
        new(
            Name: "header + single data row",
            Text: "name,age\nalice,30",
            ExpectedDocument: Delimited.Parse("name,age\nalice,30".AsSpan()),
            Options: DelimitedParseOptions.Default),
        new(
            Name: "quoted value with embedded comma",
            Text: "field\n\"a,b\"",
            ExpectedDocument: Delimited.Parse("field\n\"a,b\"".AsSpan()),
            Options: DelimitedParseOptions.Default),
    ];

    /// <inheritdoc />
    protected override IReadOnlyList<InvalidTextDocumentKat<DelimitedParseOptions>> InvalidCases { get; } =
        [
        new(
            Name: "unterminated quoted field",
            Text: "name\n\"unclosed",
            Options: DelimitedParseOptions.Default,
            ExceptionType: typeof(DelimitedFormatException)),
    ];

    /// <summary>
    /// Compares two <see cref="DelimitedDocument" /> instances by header sequence and row field
    /// sequence, ignoring object identity.
    /// </summary>
    private sealed class RowFieldComparer : IEqualityComparer<DelimitedDocument>
    {
        public bool Equals(DelimitedDocument? x, DelimitedDocument? y)
        {
            if (x is null || y is null)
                return ReferenceEquals(x, y);

            if (x.Headers.Count != y.Headers.Count) return false;
            if (x.Rows.Count != y.Rows.Count) return false;

            for (int i = 0; i < x.Headers.Count; i++)
            {
                if (x.Headers[i] != y.Headers[i]) return false;
            }

            for (int r = 0; r < x.Rows.Count; r++)
            {
                if (x.Rows[r].Fields.Count != y.Rows[r].Fields.Count) return false;
                for (int f = 0; f < x.Rows[r].Fields.Count; f++)
                {
                    if (x.Rows[r].Fields[f] != y.Rows[r].Fields[f]) return false;
                }
            }

            return true;
        }

        public int GetHashCode(DelimitedDocument obj) => obj.Rows.Count;
    }
}

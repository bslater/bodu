// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Formats.Contracts;

namespace Bodu.Text.DotEnv.Contracts;

/// <summary>
/// Drives <see cref="TextDocumentFormatContractTests{TDocument, TOptions}" /> against
/// <see cref="DotEnv" /> with a minimal set of positive and negative vectors. The contract base
/// verifies parse-equality, parse-failure exception-type assertions, and the parse → format → parse
/// round-trip. Bespoke DotEnv coverage (duplicate-key behaviour, comments, export-prefix toggle, value
/// quoting) lives in <c>DotEnvTests.*</c>.
/// </summary>
/// <remarks>
/// <para>
/// The expected document for each row is itself produced by parsing the canonical text. This loses
/// the absolute-correctness check that an independently constructed expected document would offer,
/// but the DotEnv document constructor is internal so building one in tests is not possible. The
/// contract base still verifies the round-trip: parse → format → parse must produce an equivalent
/// document, which catches asymmetric encode/decode bugs.
/// </para>
/// </remarks>
[TestClass]
public sealed class DotEnvContractTests
    : TextDocumentFormatContractTests<DotEnvDocument, DotEnvParseOptions>
{
    /// <inheritdoc />
    protected override DotEnvDocument Parse(string text, DotEnvParseOptions options = default!) =>
        DotEnv.Parse(text.AsSpan(), options);

    /// <inheritdoc />
    protected override string Format(DotEnvDocument document, DotEnvParseOptions options = default!) =>
        DotEnv.Format(document);

    /// <inheritdoc />
    protected override IEqualityComparer<DotEnvDocument> DocumentComparer { get; } = new EntryComparer();

    /// <inheritdoc />
    protected override IReadOnlyList<TextDocumentKat<DotEnvDocument, DotEnvParseOptions>> ValidCases { get; } =
        [
        new(
            Name: "empty document",
            Text: string.Empty,
            ExpectedDocument: DotEnv.Parse(string.Empty.AsSpan()),
            Options: DotEnvParseOptions.Default),
        new(
            Name: "single unquoted entry",
            Text: "KEY=value",
            ExpectedDocument: DotEnv.Parse("KEY=value".AsSpan()),
            Options: DotEnvParseOptions.Default),
        new(
            Name: "two entries with comment",
            Text: "# leading\nHOST=localhost\nPORT=8080",
            ExpectedDocument: DotEnv.Parse("# leading\nHOST=localhost\nPORT=8080".AsSpan()),
            Options: DotEnvParseOptions.Default),
    ];

    /// <inheritdoc />
    protected override IReadOnlyList<InvalidTextDocumentKat<DotEnvParseOptions>> InvalidCases { get; } =
        [
        new(
            Name: "key starts with digit",
            Text: "1INVALID=value",
            Options: DotEnvParseOptions.Default,
            ExceptionType: typeof(DotEnvFormatException)),
        new(
            Name: "key contains hyphen",
            Text: "MY-KEY=value",
            Options: DotEnvParseOptions.Default,
            ExceptionType: typeof(DotEnvFormatException)),
        new(
            Name: "unclosed double-quoted value",
            Text: "KEY=\"unclosed",
            Options: DotEnvParseOptions.Default,
            ExceptionType: typeof(DotEnvFormatException)),
    ];

    /// <summary>
    /// Compares two <see cref="DotEnvDocument" /> instances by their <see cref="DotEnvEntry.Key" /> /
    /// <see cref="DotEnvEntry.Value" /> pairs in order. Sufficient for the parse → format → parse round
    /// trip contract because the canonical formatter emits each entry once.
    /// </summary>
    private sealed class EntryComparer : IEqualityComparer<DotEnvDocument>
    {
        public bool Equals(DotEnvDocument? x, DotEnvDocument? y)
        {
            if (x is null || y is null)
                return ReferenceEquals(x, y);

            if (x.Entries.Count != y.Entries.Count)
                return false;

            for (var i = 0; i < x.Entries.Count; i++)
            {
                if (x.Entries[i].Key != y.Entries[i].Key) return false;
                if (x.Entries[i].Value != y.Entries[i].Value) return false;
            }

            return true;
        }

        public int GetHashCode(DotEnvDocument obj) => obj.Entries.Count;
    }
}

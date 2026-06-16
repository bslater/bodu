// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Formats.Contracts;

namespace Bodu.Text.Ini.Contracts;

/// <summary>
/// Drives <see cref="TextDocumentFormatContractTests{TDocument, TOptions}" /> against
/// <see cref="Ini" /> with a small representative set of positive and negative vectors. The contract
/// base verifies parse-equality, parse-failure exception-type assertions, and the
/// parse → format → parse round-trip — the shared text-format contract the plan calls for.
/// </summary>
[TestClass]
public sealed class IniContractTests
    : TextDocumentFormatContractTests<IniDocument, IniParseOptions>
{
    /// <inheritdoc />
    protected override IniDocument Parse(string text, IniParseOptions options = default!) =>
        Ini.Parse(text.AsSpan(), options);

    /// <inheritdoc />
    protected override string Format(IniDocument document, IniParseOptions options = default!) =>
        Ini.Format(document);

    /// <inheritdoc />
    protected override IEqualityComparer<IniDocument> DocumentComparer { get; } = new SectionNameComparer();

    /// <inheritdoc />
    protected override IReadOnlyList<TextDocumentKat<IniDocument, IniParseOptions>> ValidCases { get; } =
    [
        new(
            Name: "empty document",
            Text: string.Empty,
            ExpectedDocument: new IniDocument()),
        new(
            Name: "single section with one key",
            Text: "[section]\nkey=value",
            ExpectedDocument: BuildSingleSection("section", "key", "value")),
    ];

    /// <inheritdoc />
    protected override IReadOnlyList<InvalidTextDocumentKat<IniParseOptions>> InvalidCases { get; } =
    [
        new(
            Name: "section header missing closing bracket",
            Text: "[unclosed",
            Options: default,
            ExceptionType: typeof(IniFormatException)),
    ];

    private static IniDocument BuildSingleSection(string sectionName, string key, string value)
    {
        IniDocument document = new();
        IniSection section = document.GetOrAddSection(sectionName);
        section.AddEntry(new IniEntry(key, value));
        return document;
    }

    private sealed class SectionNameComparer
        : IEqualityComparer<IniDocument>
    {
        public bool Equals(IniDocument? x, IniDocument? y)
        {
            if (x is null || y is null)
                return ReferenceEquals(x, y);

            if (x.Sections.Count != y.Sections.Count)
                return false;

            for (int i = 0; i < x.Sections.Count; i++)
            {
                if (x.Sections[i].Name != y.Sections[i].Name)
                    return false;
            }

            return true;
        }

        public int GetHashCode(IniDocument obj) => obj.Sections.Count;
    }
}

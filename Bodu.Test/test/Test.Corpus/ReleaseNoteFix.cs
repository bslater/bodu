// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReleaseNoteFix.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Test.Corpus;

/// <summary>
/// Represents one row of a release-note fix catalogue: a defect fix another library shipped, how it bears on Bodu,
/// and, for a row that runs, the scenario Bodu must handle.
/// </summary>
/// <param name="FileName">The catalogue the row came from, such as <c>tomli-fixes.csv</c>.</param>
/// <param name="LineNumber">The one-based line of the row in its catalogue.</param>
/// <param name="Library">The library that shipped the fix, as the catalogue's file name gives it.</param>
/// <param name="Version">The release that shipped the fix, or <c>unreleased</c>.</param>
/// <param name="Reference">The issue, pull request, commit or CVE behind the fix, or the empty string.</param>
/// <param name="Summary">A short paraphrase of the release-note entry.</param>
/// <param name="Class">How the fix bears on Bodu: <c>applies</c>, <c>dialect</c>, <c>n/a</c> or <c>unknown</c>.</param>
/// <param name="Case">The case number when the fix needs several rows, or the empty string.</param>
/// <param name="Kind">What the row checks: <c>parse</c>, <c>reject</c>, <c>write</c>, <c>write-reject</c>, <c>roundtrip</c> or <c>unit</c>.</param>
/// <param name="Options">The options the row runs with, written <c>Name=Value;Name=Value</c>.</param>
/// <param name="Input">The escaped input, or the test method that covers a <c>unit</c> row.</param>
/// <param name="Expected">The escaped expected result.</param>
/// <param name="Expectation">Where the expected result comes from: <c>upstream</c>, <c>spec</c>, <c>derived</c> or <c>oracle:&lt;tool&gt;</c>.</param>
/// <param name="Reason">Why the row has its class, citing its source.</param>
public sealed record ReleaseNoteFix(
    string FileName,
    int LineNumber,
    string Library,
    string Version,
    string Reference,
    string Summary,
    string Class,
    string Case,
    string Kind,
    string Options,
    string Input,
    string Expected,
    string Expectation,
    string Reason) : IKat
{
    /// <summary>
    /// Gets the label that identifies the row in test output.
    /// </summary>
    /// <value>
    /// <c>"{Library} fix {Version} {Reference}: {Summary}"</c>, without the reference when the row has none, and followed
    /// by <c>" (case {Case})"</c> when the fix needs several rows.
    /// </value>
    public string Name
    {
        get
        {
            string reference = Reference.Length == 0 ? string.Empty : " " + Reference;
            string caseNumber = Case.Length == 0 ? string.Empty : $" (case {Case})";
            return $"{Library} fix {Version}{reference}: {Summary}{caseNumber}";
        }
    }

    /// <summary>
    /// Gets a value indicating whether the row runs as data: an <c>applies</c> or <c>dialect</c> row of any kind except
    /// <c>unit</c>.
    /// </summary>
    /// <value>
    /// <see langword="true" /> for the rows an area's corpus tests execute; <see langword="false" /> for <c>n/a</c> and
    /// <c>unknown</c> rows and for <c>unit</c> rows, which name a test method instead.
    /// </value>
    public bool IsRunnable =>
        Class is "applies" or "dialect" && Kind is "parse" or "reject" or "write" or "write-reject" or "roundtrip";

    /// <summary>
    /// Returns the row's <see cref="Name" /> and its position in its catalogue.
    /// </summary>
    /// <returns>The label, followed by the file name and line number.</returns>
    public override string ToString() =>
        $"{Name} [{FileName}:{LineNumber}]";
}

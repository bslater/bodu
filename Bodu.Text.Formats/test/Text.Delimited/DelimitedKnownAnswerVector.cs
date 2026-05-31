// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedKnownAnswerVector.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Text.Delimited;

/// <summary>
/// Represents a positive Known Answer Test (KAT) vector for the RFC 4180 CSV/delimited-text format. A vector pairs a
/// raw source string with the expected parsed state — header names and per-row field values — so that data-driven tests
/// can verify <see cref="Delimited" /> against authoritative references and common-practice inputs.
/// </summary>
/// <param name="Description">A short human-readable label identifying the vector.</param>
/// <param name="Input">The raw CSV or TSV source text to parse.</param>
/// <param name="ExpectedHeaders">
/// The expected header names in source order. Pass an empty array when the vector uses
/// <see cref="DelimitedParseOptions.HasHeader" /> set to <see langword="false" />.
/// </param>
/// <param name="ExpectedRows">
/// The expected field values for each data row, in source order. Each inner array holds the fields for one row.
/// </param>
/// <param name="Options">
/// The parse options to apply. <see langword="null" /> indicates that <see cref="DelimitedParseOptions.Default" />
/// should be used.
/// </param>
/// <param name="Source">
/// The citation for the vector's origin (specification clause, reference implementation, etc.).
/// </param>
public sealed record DelimitedKnownAnswerVector(
    string Description,
    string Input,
    string[] ExpectedHeaders,
    string[][] ExpectedRows,
    DelimitedParseOptions? Options = null,
    string? Source = null) : IKat
{
    /// <inheritdoc />
    /// <remarks>
    /// Returns <see cref="Description" /> so the row participates in <see cref="KatDisplayName" />-driven display
    /// formatting.
    /// </remarks>
    string IKat.Name => Description;

    /// <summary>
    /// Returns the human-readable label, used by MSTest's <c>[DynamicData]</c> infrastructure when generating test
    /// names so each row is identifiable in failure output.
    /// </summary>
    /// <returns>The description, optionally suffixed with the citation in square brackets.</returns>
    public override string ToString() =>
        Source is null ? Description : $"{Description} [{Source}]";
}

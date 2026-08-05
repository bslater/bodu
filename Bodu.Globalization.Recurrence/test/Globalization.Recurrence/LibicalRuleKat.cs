// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LibicalRuleKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

/// <summary>
/// Represents one rule from libical's recurrence test corpus, pairing it with the occurrence count that
/// reference implementation asserts for it.
/// </summary>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Start">The series start (<c>DTSTART</c>) read as a wall-clock instant.</param>
/// <param name="Rule">The recurrence-rule text.</param>
/// <param name="ExpectedCount">The number of occurrences libical asserts through <c>X-EXPECT-NUMEVENTS</c>.</param>
/// <param name="ExceptionDate">The single exception date the block excludes, or <see langword="null" /> for none.</param>
/// <param name="Flags">The space-separated scope flags recorded in the corpus.</param>
/// <remarks>
/// A count is a weaker oracle than a date list, but it is the one that catches the two defects that matter most
/// in an occurrence engine: emitting an instant twice, and dropping one.
/// </remarks>
public sealed record LibicalRuleKat(
    string Name,
    DateTime Start,
    string Rule,
    int ExpectedCount,
    DateTime? ExceptionDate,
    string Flags) : IKat
{
    /// <summary>
    /// Gets a value indicating whether the rule falls inside the surface this library models.
    /// </summary>
    /// <value><see langword="true" /> when no excluding flag is present.</value>
    public bool IsInScope =>
        RecurrenceCorpusTests.InScope(Flags);
}

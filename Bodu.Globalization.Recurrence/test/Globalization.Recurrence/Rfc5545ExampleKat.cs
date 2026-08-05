// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Rfc5545ExampleKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

/// <summary>
/// Represents one worked recurrence example transcribed from RFC 5545 §3.8.5.3, pairing the rule with the occurrence
/// dates the standard lists for it.
/// </summary>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Start">The series start (<c>DTSTART</c>) read as a wall-clock instant.</param>
/// <param name="Rule">The recurrence-rule text, including its <c>RRULE:</c> prefix.</param>
/// <param name="Flags">The space-separated scope flags recorded in the corpus.</param>
/// <param name="Expected">The expected occurrence dates, formatted <c>yyyy-MM-dd</c>.</param>
public sealed record Rfc5545ExampleKat(
    string Name,
    DateTime Start,
    string Rule,
    string Flags,
    string[] Expected) : IKat
{
    /// <summary>
    /// Gets a value indicating whether the example falls inside the surface this library models.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when no excluding flag is present and the example lists at least one occurrence.
    /// </value>
    public bool IsInScope =>
        Expected.Length > 0 && RecurrenceCorpusTests.InScope(Flags);
}

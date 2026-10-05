// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateutilSubDailyKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

/// <summary>
/// Represents one sub-daily recurrence rule from the python-dateutil comparison corpus, pairing it with the leading
/// occurrences that implementation produces for it.
/// </summary>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Start">The series start (<c>DTSTART</c>) read as a wall-clock instant.</param>
/// <param name="Rule">The recurrence-rule text.</param>
/// <param name="Flags">The space-separated flags recorded in the corpus.</param>
/// <param name="Expected">The occurrences dateutil produces, up to the corpus's recorded limit.</param>
/// <remarks>
/// dateutil is a third-party comparison rather than an authority. A row flagged <c>truncated</c> continues past the
/// recorded occurrences; any other row records its whole stream, which is empty for a rule flagged <c>empty-set</c>,
/// one whose interval never reaches a time of day its own limits allow.
/// </remarks>
public sealed record DateutilSubDailyKat(
    string Name,
    DateTime Start,
    string Rule,
    string Flags,
    DateTime[] Expected) : IKat
{
    /// <summary>
    /// Gets a value indicating whether the rule continues past the recorded occurrences.
    /// </summary>
    /// <value><see langword="true" /> when the corpus flags the row <c>truncated</c>.</value>
    public bool IsTruncated =>
        Flags.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("truncated");
}

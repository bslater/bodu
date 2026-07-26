// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceRuleTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

/// <summary>
/// Contains unit tests for the <see cref="RecurrenceRule" /> type.
/// </summary>
[TestClass]
public partial class RecurrenceRuleTests
{
    /// <summary>
    /// Expands the supplied rule from the given start and returns the first <paramref name="take" /> occurrences.
    /// </summary>
    /// <param name="rule">The RRULE text.</param>
    /// <param name="start">The series start.</param>
    /// <param name="take">The maximum number of occurrences to materialize.</param>
    /// <returns>The materialized occurrences.</returns>
    private static DateTime[] Occurrences(string rule, DateTime start, int take) =>
        RecurrenceRule.Parse(rule).GetOccurrences(start).Take(take).ToArray();
}

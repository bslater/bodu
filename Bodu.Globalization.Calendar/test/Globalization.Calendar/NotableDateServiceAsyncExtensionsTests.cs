// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceAsyncExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Tests for the <see cref="NotableDateServiceAsyncExtensions" /> streaming projections.
/// </summary>
[TestClass]
public partial class NotableDateServiceAsyncExtensionsTests
{
    /// <summary>
    /// Drains an asynchronous occurrence stream into a list.
    /// </summary>
    /// <param name="stream">The stream to drain.</param>
    /// <returns>The streamed occurrences in order.</returns>
    private static async Task<List<NotableDate>> DrainAsync(IAsyncEnumerable<NotableDate> stream)
    {
        List<NotableDate> occurrences = new();

        await foreach (NotableDate occurrence in stream)
        {
            occurrences.Add(occurrence);
        }

        return occurrences;
    }

    /// <summary>
    /// Projects occurrences to comparable identity tuples, avoiding reference-sensitive record members.
    /// </summary>
    /// <param name="occurrences">The occurrences to project.</param>
    /// <returns>An ordered list of comparable tuples.</returns>
    private static List<(DateOnly Date, DateOnly? ActualDate, string Id, string RuleId, bool Observed)> AsComparable(
        IEnumerable<NotableDate> occurrences) =>
        occurrences
            .Select(o => (o.Date, o.ActualDate, o.NotableDateId, o.RuleId, o.IsObserved))
            .ToList();
}

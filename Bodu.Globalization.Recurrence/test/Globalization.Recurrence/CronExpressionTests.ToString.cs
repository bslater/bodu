// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CronExpressionTests.ToString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class CronExpressionTests
{
    /// <summary>
    /// Verifies that the canonical text of an expression re-parses to the same schedule, including the day-field
    /// expressions whose restricted-ness decides between the union and intersection branches.
    /// </summary>
    /// <param name="text">The expression to render and re-parse.</param>
    [TestMethod]
    [DataRow("0 0 */2 * MON", DisplayName = "unrestricted day-of-month step beside a restricted weekday")]
    [DataRow("0 0 1-31/2 * MON", DisplayName = "restricted day-of-month step beside a restricted weekday")]
    [DataRow("0 0 1-31 * MON", DisplayName = "restricted day-of-month spanning the whole range")]
    [DataRow("0 0 * * MON", DisplayName = "unrestricted day-of-month beside a restricted weekday")]
    [DataRow("0 0 13 * 0-6", DisplayName = "restricted weekday spanning the whole range")]
    [DataRow("0 0 13 * */2", DisplayName = "unrestricted weekday step beside a restricted day-of-month")]
    [DataRow("0 0 13 * FRI", DisplayName = "both day fields restricted")]
    public void ToString_WhenReparsed_ShouldSelectTheSameInstants(string text)
    {
        CronExpression original = CronExpression.Parse(text);

        CronExpression roundTripped = CronExpression.Parse(original.ToString());

        // A year of occurrences pins the union/intersection branch as well as the field masks.
        var from = new DateTime(2026, 1, 1, 0, 0, 0);
        var until = new DateTime(2027, 1, 1, 0, 0, 0);

        CollectionAssert.AreEqual(
            Occurrences(original, from, until),
            Occurrences(roundTripped, from, until));
    }

    /// <summary>
    /// Verifies that a day field that was written with a leading <c>*</c> is rendered in a form that also leads with
    /// <c>*</c>, so it re-parses as unrestricted rather than as the explicit list of the days it selects.
    /// </summary>
    /// <param name="text">The expression to render.</param>
    /// <param name="expected">The expected canonical text.</param>
    [TestMethod]
    [DataRow("0 0 */2 * MON", "0 0 */2 * 1", DisplayName = "day-of-month step keeps its star")]
    [DataRow("0 0 13 * */2", "0 0 13 * */2", DisplayName = "weekday step keeps its star")]
    [DataRow("0 0 1-31 * MON", "0 0 1-31 * 1", DisplayName = "a full but restricted day-of-month stays explicit")]
    [DataRow("0 0 13 * 0-6", "0 0 13 * 0-6", DisplayName = "a full but restricted weekday stays explicit")]
    public void ToString_WhenDayFieldRestrictednessIsNotImpliedByItsValues_ShouldPreserveIt(string text, string expected)
    {
        CronExpression cron = CronExpression.Parse(text);

        Assert.AreEqual(expected, cron.ToString());
    }

    /// <summary>
    /// Collects every occurrence of an expression within a half-open window, as a list suitable for collection
    /// assertions.
    /// </summary>
    /// <param name="cron">The expression to enumerate.</param>
    /// <param name="from">The inclusive start of the window.</param>
    /// <param name="until">The exclusive end of the window.</param>
    /// <returns>The occurrences in ascending order.</returns>
    private static List<DateTime> Occurrences(CronExpression cron, DateTime from, DateTime until)
    {
        var results = new List<DateTime>();
        DateTime? current = cron.GetNextOccurrence(from, inclusive: true);

        while (current is { } value && value < until)
        {
            results.Add(value);
            current = cron.GetNextOccurrence(value);
        }

        return results;
    }
}

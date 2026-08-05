// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CronExpressionTests.Equality.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class CronExpressionTests
{
    /// <summary>
    /// Verifies that expressions selecting the same instants compare equal and hash alike, however they are spelled.
    /// </summary>
    /// <param name="left">The first expression.</param>
    /// <param name="right">The second expression, denoting the same schedule.</param>
    [TestMethod]
    [DataRow("* * * * *", "0-59 * * * *", DisplayName = "star equals its full range")]
    [DataRow("* * * * *", "0-59/1 * * * *", DisplayName = "star equals a unit-stepped full range")]
    [DataRow("0 8 * * 1-5", "0 8 * * MON-FRI", DisplayName = "weekday numbers equal weekday names")]
    [DataRow("*/30 * * * *", "0,30 * * * *", DisplayName = "a step equals the list it expands to")]
    [DataRow("@hourly", "0 * * * *", DisplayName = "a macro equals its long form")]
    [DataRow("0 0 * * 0", "0 0 * * 7", DisplayName = "Sunday is both 0 and 7")]
    public void Equals_WhenSchedulesMatch_ShouldReturnTrueAndHashAlike(string left, string right)
    {
        CronExpression first = CronExpression.Parse(left);
        CronExpression second = CronExpression.Parse(right);

        Assert.IsTrue(first.Equals(second));
        Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
    }

    /// <summary>
    /// Verifies that expressions differing in exactly one field do not compare equal.
    /// </summary>
    /// <param name="left">The first expression.</param>
    /// <param name="right">The second expression, differing in one field.</param>
    [TestMethod]
    [DataRow("1 1 1 1 1", "2 1 1 1 1", DisplayName = "minute differs")]
    [DataRow("1 1 1 1 1", "1 2 1 1 1", DisplayName = "hour differs")]
    [DataRow("1 1 1 1 1", "1 1 2 1 1", DisplayName = "day-of-month differs")]
    [DataRow("1 1 1 1 1", "1 1 1 2 1", DisplayName = "month differs")]
    [DataRow("1 1 1 1 1", "1 1 1 1 2", DisplayName = "day-of-week differs")]
    public void Equals_WhenOneFieldDiffers_ShouldReturnFalse(string left, string right)
    {
        CronExpression first = CronExpression.Parse(left);
        CronExpression second = CronExpression.Parse(right);

        Assert.IsFalse(first.Equals(second));
    }

    /// <summary>
    /// Verifies that expressions differing only in which value a field selects hash differently, so a hash table keyed
    /// by schedule does not degenerate to a linear scan.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unequal hash codes are not required by the equality contract — only equal objects must hash equally — so this is
    /// a distribution assertion rather than a correctness one. It earns its place because the shape it guards is the
    /// overwhelmingly common one: almost every configured schedule (<c>0 2 * * *</c>, <c>30 9 * * *</c>, …) selects a
    /// single value in each of several fields, so if the hash ignores *which* value is selected, every such schedule
    /// collides with every other.
    /// </para>
    /// <para>
    /// Cronos asserts the same property over the same expression pairs in <c>Equals_ReturnsFalse_…</c>, which is where
    /// this was found.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void GetHashCode_WhenSchedulesDifferOnlyInSelectedValues_ShouldDiffer()
    {
        // Each of these selects exactly one value per field, so a cardinality-only hash maps them all to one bucket.
        string[] expressions =
        [
            "1 1 1 1 1",
            "2 1 1 1 1",
            "1 2 1 1 1",
            "1 1 2 1 1",
            "1 1 1 2 1",
            "1 1 1 1 2",
            "0 2 * * *",
            "30 9 * * *",
            "0 3 * * *",
        ];

        int[] hashes = [.. expressions.Select(e => CronExpression.Parse(e).GetHashCode())];

        // Distinct schedules, so distinct hashes: a collision here means the hash is ignoring field contents.
        CollectionAssert.AllItemsAreUnique(
            hashes,
            $"hash codes collided across distinct schedules: {string.Join(", ", expressions.Zip(hashes, (e, h) => $"'{e}'={h}"))}");
    }

    /// <summary>
    /// Verifies that the six-field and five-field readings of the same schedule are distinct values, so the declared
    /// format participates in equality.
    /// </summary>
    [TestMethod]
    public void Equals_WhenFormatsDiffer_ShouldReturnFalse()
    {
        CronExpression standard = CronExpression.Parse("0 12 * * 1", CronFormat.Standard);
        CronExpression withSeconds = CronExpression.Parse("0 0 12 * * 1", CronFormat.WithSeconds);

        Assert.IsFalse(standard.Equals(withSeconds));
    }
}

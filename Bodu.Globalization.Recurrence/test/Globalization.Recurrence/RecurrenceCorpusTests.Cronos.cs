// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceCorpusTests.Cronos.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Recurrence;

public sealed partial class RecurrenceCorpusTests
{
    /// <summary>
    /// Gets the Cronos vectors that assert the next occurrence of an expression.
    /// </summary>
    /// <value>The corpus rows.</value>
    public static IEnumerable<object[]> CronosNextOccurrences =>
        LoadCronos("next").Select(r => new object[] { r });

    /// <summary>
    /// Gets the Cronos vectors whose expressions can never fire.
    /// </summary>
    /// <value>The corpus rows.</value>
    public static IEnumerable<object[]> CronosUnreachable =>
        LoadCronos("unreachable").Select(r => new object[] { r });

    /// <summary>
    /// Gets the Cronos vectors that must be rejected as malformed.
    /// </summary>
    /// <value>The corpus rows.</value>
    public static IEnumerable<object[]> CronosInvalid =>
        LoadCronos("invalid").Select(r => new object[] { r });

    /// <summary>
    /// Gets the Cronos vectors that pair two spellings of the same schedule.
    /// </summary>
    /// <value>The corpus rows.</value>
    public static IEnumerable<object[]> CronosEquivalent =>
        LoadCronos("equal").Select(r => new object[] { r });

    /// <summary>
    /// Gets the Cronos vectors that pair two expressions denoting different schedules.
    /// </summary>
    /// <value>The corpus rows.</value>
    public static IEnumerable<object[]> CronosDistinct =>
        LoadCronos("notEqual").Select(r => new object[] { r });

    /// <summary>
    /// Gets the Cronos vectors that assert an expression's canonical text.
    /// </summary>
    /// <value>The corpus rows.</value>
    public static IEnumerable<object[]> CronosCanonicalText =>
        LoadCronos("toString").Select(r => new object[] { r });

    /// <summary>
    /// Verifies that each Cronos vector's next occurrence, taken inclusively, is the instant that implementation
    /// asserts.
    /// </summary>
    /// <param name="kat">The corpus row under test.</param>
    /// <remarks>
    /// Cronos states its vectors as a US Eastern wall clock on both sides of the assertion, so a zone-free engine
    /// reproduces them verbatim. The rows where that reasoning fails — those within a day of a DST transition — are
    /// flagged <c>dst</c> in the corpus and excluded here.
    /// </remarks>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(CronosNextOccurrences),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void GetNextOccurrence_WhenCronosVector_ShouldMatchTheAssertedInstant(CronosVectorKat kat)
    {
        DateTime expected = DateTime.ParseExact(kat.Expected, "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);

        DateTime? actual = CronExpression.Parse(kat.Expression, kat.Format).GetNextOccurrence(kat.From, inclusive: true);

        Assert.AreEqual(expected, actual, kat.Expression);
    }

    /// <summary>
    /// Verifies that an expression selecting a date that never occurs — February 30th and its kin — reports no next
    /// occurrence rather than searching without end.
    /// </summary>
    /// <param name="kat">The corpus row under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(CronosUnreachable),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void GetNextOccurrence_WhenCronosVectorIsUnreachable_ShouldReturnNull(CronosVectorKat kat)
    {
        DateTime? actual = CronExpression.Parse(kat.Expression, kat.Format).GetNextOccurrence(kat.From, inclusive: true);

        Assert.IsNull(actual, kat.Expression);
    }

    /// <summary>
    /// Verifies that each malformed Cronos vector is rejected.
    /// </summary>
    /// <param name="kat">The corpus row under test.</param>
    /// <remarks>
    /// Bodu rejects a superset of what Cronos rejects, having no Quartz extensions to accept, so these rows are
    /// reconciled whatever syntax they use. The one place the superset does not hold — a step wider than its range,
    /// which cronie warns about and accepts — is flagged <c>oversized-step</c> and excluded.
    /// </remarks>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(CronosInvalid),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void TryParse_WhenCronosVectorIsMalformed_ShouldReturnFalse(CronosVectorKat kat)
    {
        bool parsed = CronExpression.TryParse(kat.Expression, kat.Format, out CronExpression? result);

        Assert.IsFalse(parsed, kat.Expression);
        Assert.IsNull(result);
    }

    /// <summary>
    /// Verifies that two spellings of the same schedule compare equal and hash alike, so equality is decided by the
    /// selected instants rather than by the source text.
    /// </summary>
    /// <param name="kat">The corpus row under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(CronosEquivalent),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Equals_WhenCronosVectorsDenoteTheSameSchedule_ShouldReturnTrue(CronosVectorKat kat)
    {
        CronExpression left = CronExpression.Parse(kat.Expression, kat.Format);
        CronExpression right = CronExpression.Parse(kat.Expected, kat.Format);

        Assert.IsTrue(left.Equals(right), $"{kat.Expression} vs {kat.Expected}");
        Assert.AreEqual(left.GetHashCode(), right.GetHashCode(), $"{kat.Expression} vs {kat.Expected}");
    }

    /// <summary>
    /// Verifies that expressions denoting different schedules do not compare equal.
    /// </summary>
    /// <param name="kat">The corpus row under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(CronosDistinct),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Equals_WhenCronosVectorsDenoteDifferentSchedules_ShouldReturnFalse(CronosVectorKat kat)
    {
        CronExpression left = CronExpression.Parse(kat.Expression, kat.Format);
        CronExpression right = CronExpression.Parse(kat.Expected, kat.Format);

        Assert.IsFalse(left.Equals(right), $"{kat.Expression} vs {kat.Expected}");
    }

    /// <summary>
    /// Verifies that an expression renders to the canonical text Cronos normalizes it to: each field either
    /// <c>*</c> when it spans its whole range, or the sorted list of the values it selects.
    /// </summary>
    /// <param name="kat">The corpus row under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(CronosCanonicalText),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ToString_WhenCronosVector_ShouldMatchTheCanonicalText(CronosVectorKat kat)
    {
        string actual = CronExpression.Parse(kat.Expression, kat.Format).ToString();

        Assert.AreEqual(kat.Expected, actual, kat.Expression);
    }

    /// <summary>
    /// Verifies that a canonical rendering re-parses to an expression equal to the original, so the corpus's
    /// normalized text is a faithful round trip rather than a lossy display form.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void ToString_WhenReparsed_ShouldProduceAnEqualExpression()
    {
        var offenders = new List<string>();

        foreach (CronosVectorKat kat in LoadCronos().Where(r => r.IsInScope && r.Kind != "invalid"))
        {
            CronExpression original = CronExpression.Parse(kat.Expression, kat.Format);
            CronExpression reparsed = CronExpression.Parse(original.ToString(), kat.Format);

            if (!original.Equals(reparsed))
                offenders.Add($"{kat.Expression} -> {original}");
        }

        Assert.AreEqual(0, offenders.Count, string.Join("; ", offenders));
    }

    /// <summary>
    /// Reads the Cronos vector table from the embedded corpus.
    /// </summary>
    /// <param name="kind">The assertion kind to filter to, or <see langword="null" /> for every row.</param>
    /// <returns>The in-scope corpus rows in file order when a kind is given; otherwise every row.</returns>
    private static IEnumerable<CronosVectorKat> LoadCronos(string? kind = null)
    {
        foreach (string[] f in ReadCsv("cronos-cron-vectors.csv"))
        {
            var row = new CronosVectorKat(
                $"Cronos {f[0]} '{f[3]}'",
                f[1],
                f[2] == "withSeconds" ? CronFormat.WithSeconds : CronFormat.Standard,
                f[3],
                string.IsNullOrEmpty(f[4])
                    ? default
                    : DateTime.ParseExact(f[4], "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture),
                f[5],
                f[6]);

            if (kind is null || (row.Kind == kind && row.IsInScope))
                yield return row;
        }
    }
}

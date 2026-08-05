// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceRuleTests.TryParse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class RecurrenceRuleTests
{
    /// <summary>
    /// Gets the malformed rule rows, each carrying the defect fragment its failure message must name.
    /// </summary>
    /// <value>The malformed rule rows.</value>
    public static IEnumerable<object[]> TryParseDefectKats
    {
        get
        {
            var rows = new List<InvalidKat<string>>
            {
                new("empty text", string.Empty, typeof(FormatException), MessageContains: "empty"),
                new("missing FREQ", "INTERVAL=2", typeof(FormatException), MessageContains: "requires a frequency"),
                new("invalid frequency value", "FREQ=FORTNIGHTLY", typeof(FormatException), MessageContains: "FREQ=FORTNIGHTLY"),
                new("duplicate part", "FREQ=DAILY;COUNT=2;COUNT=3", typeof(FormatException), MessageContains: "more than once"),
                new("unknown part", "FREQ=DAILY;X-CUSTOM=1", typeof(FormatException), MessageContains: "not recognized"),
                new("value out of range", "FREQ=DAILY;BYHOUR=24", typeof(FormatException), MessageContains: "BYHOUR=24"),
                new("count and until together", "FREQ=DAILY;COUNT=2;UNTIL=20260101T000000", typeof(FormatException), MessageContains: "COUNT and UNTIL"),
            };

            foreach (InvalidKat<string> row in rows)
            {
                yield return [row];
            }
        }
    }

    /// <summary>
    /// Verifies that the defect-reporting <c>TryParse</c> overload names the specific defect on failure.
    /// </summary>
    /// <param name="kat">The malformed rule row under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(TryParseDefectKats),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void TryParse_WhenInvalid_ShouldReportDefectMessage(InvalidKat<string> kat)
    {
        bool parsed = RecurrenceRule.TryParse(kat.Input, out RecurrenceRule? result, out string? failureMessage);

        Assert.IsFalse(parsed);
        Assert.IsNull(result);
        Assert.IsNotNull(kat.MessageContains);
        Assert.IsNotNull(failureMessage);
        Assert.IsTrue(
            failureMessage.Contains(kat.MessageContains, StringComparison.Ordinal),
            $"Expected failure message to contain '{kat.MessageContains}' but was '{failureMessage}'.");
    }

    /// <summary>
    /// Verifies that the defect-reporting <c>TryParse</c> overload reports no message on success.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenValid_ShouldReportNullFailureMessage()
    {
        bool parsed = RecurrenceRule.TryParse("FREQ=WEEKLY;BYDAY=MO,FR", out RecurrenceRule? result, out string? failureMessage);

        Assert.IsTrue(parsed);
        Assert.IsNotNull(result);
        Assert.IsNull(failureMessage);
    }

    /// <summary>
    /// Verifies that the defect-reporting <c>TryParse</c> overload treats <see langword="null" /> input as an empty
    /// rule defect rather than throwing.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenNull_ShouldReturnFalseAndReportEmptyDefect()
    {
        bool parsed = RecurrenceRule.TryParse(null, out RecurrenceRule? result, out string? failureMessage);

        Assert.IsFalse(parsed);
        Assert.IsNull(result);
        Assert.IsNotNull(failureMessage);
        Assert.IsTrue(failureMessage.Contains("empty", StringComparison.Ordinal));
    }
}

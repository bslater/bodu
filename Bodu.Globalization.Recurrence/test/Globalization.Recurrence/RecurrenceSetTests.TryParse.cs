// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceSetTests.TryParse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class RecurrenceSetTests
{
    /// <summary>
    /// Gets the malformed property-block rows, each carrying the defect fragment its failure message must name.
    /// </summary>
    /// <value>The malformed property-block rows.</value>
    public static IEnumerable<object[]> TryParseDefectKats
    {
        get
        {
            var rows = new List<InvalidKat<string>>
            {
                new("missing DTSTART", "RRULE:FREQ=DAILY", typeof(FormatException), MessageContains: "DTSTART"),
                new("duplicate DTSTART", "DTSTART:20260101T090000\nDTSTART:20260102T090000\nRRULE:FREQ=DAILY", typeof(FormatException), MessageContains: "more than once"),
                new("invalid DTSTART value", "DTSTART:2026-01-01\nRRULE:FREQ=DAILY", typeof(FormatException), MessageContains: "2026-01-01"),
                new("no rules or dates", "DTSTART:20260101T090000", typeof(FormatException), MessageContains: "at least one rule"),
                new("invalid nested rule", "DTSTART:20260101T090000\nRRULE:FREQ=DAILY;BYHOUR=24", typeof(FormatException), MessageContains: "BYHOUR=24"),
                new("invalid RDATE value", "DTSTART:20260101T090000\nRRULE:FREQ=DAILY\nRDATE:notadate", typeof(FormatException), MessageContains: "notadate"),
                new("invalid EXDATE value", "DTSTART:20260101T090000\nRRULE:FREQ=DAILY\nEXDATE:20261303T000000", typeof(FormatException), MessageContains: "20261303T000000"),
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
    /// <param name="kat">The malformed property-block row under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(TryParseDefectKats),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void TryParse_WhenInvalid_ShouldReportDefectMessage(InvalidKat<string> kat)
    {
        bool parsed = RecurrenceSet.TryParse(kat.Input, out RecurrenceSet? result, out string? failureMessage);

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
        bool parsed = RecurrenceSet.TryParse(
            "DTSTART:20260101T090000\nRRULE:FREQ=DAILY",
            out RecurrenceSet? result,
            out string? failureMessage);

        Assert.IsTrue(parsed);
        Assert.IsNotNull(result);
        Assert.IsNull(failureMessage);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CronExpressionTests.TryParse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class CronExpressionTests
{
    /// <summary>
    /// Gets the malformed cron rows, each carrying the defect fragment its failure message must name.
    /// </summary>
    /// <value>The malformed cron rows.</value>
    public static IEnumerable<object[]> TryParseDefectKats
    {
        get
        {
            var rows = new List<InvalidKat<string>>
            {
                new("empty text", string.Empty, typeof(FormatException), MessageContains: "empty"),
                new("wrong field count", "0 9 * *", typeof(FormatException), MessageContains: "number of fields"),
                new("invalid field value", "0 25 * * *", typeof(FormatException), MessageContains: "'25'"),
                new("invalid name token", "0 9 * XXX *", typeof(FormatException), MessageContains: "'XXX'"),
                new("unknown macro", "@fortnightly", typeof(FormatException), MessageContains: "@fortnightly"),
                new("quartz extension token", "0 9 L * *", typeof(FormatException), MessageContains: "'L'"),
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
    /// <param name="kat">The malformed cron row under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(TryParseDefectKats),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void TryParse_WhenInvalid_ShouldReportDefectMessage(InvalidKat<string> kat)
    {
        bool parsed = CronExpression.TryParse(kat.Input, out CronExpression? result, out string? failureMessage);

        Assert.IsFalse(parsed);
        Assert.IsNull(result);
        Assert.IsNotNull(kat.MessageContains);
        Assert.IsNotNull(failureMessage);
        Assert.IsTrue(
            failureMessage.Contains(kat.MessageContains, StringComparison.Ordinal),
            $"Expected failure message to contain '{kat.MessageContains}' but was '{failureMessage}'.");
    }

    /// <summary>
    /// Verifies that the layout-specific defect-reporting overload names the field-count defect when the layout does
    /// not match.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenLayoutMismatch_ShouldReportFieldCountDefect()
    {
        bool parsed = CronExpression.TryParse("0 9 * * *", CronFormat.WithSeconds, out CronExpression? result, out string? failureMessage);

        Assert.IsFalse(parsed);
        Assert.IsNull(result);
        Assert.IsNotNull(failureMessage);
        Assert.IsTrue(failureMessage.Contains("number of fields", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that the defect-reporting <c>TryParse</c> overload reports no message on success.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenValid_ShouldReportNullFailureMessage()
    {
        bool parsed = CronExpression.TryParse("0 9 * * 1-5", out CronExpression? result, out string? failureMessage);

        Assert.IsTrue(parsed);
        Assert.IsNotNull(result);
        Assert.IsNull(failureMessage);
    }
}

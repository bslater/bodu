// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CronExpressionTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class CronExpressionTests
{
    /// <summary>
    /// Verifies that a named-field expression matches the same instants as its numeric form.
    /// </summary>
    [TestMethod]
    public void Parse_WhenNamedFields_ShouldEqualNumericForm()
    {
        CronExpression named = CronExpression.Parse("0 0 1 JAN MON");
        CronExpression numeric = CronExpression.Parse("0 0 1 1 1");

        Assert.AreEqual(numeric, named);
    }

    /// <summary>
    /// Verifies that a Quartz token outside the two day fields throws <see cref="FormatException" />, since every token
    /// stands for a day of the month or of the week.
    /// </summary>
    [TestMethod]
    public void Parse_WhenQuartzTokenOutsideTheDayFields_ShouldThrowFormatException()
    {
        _ = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = CronExpression.Parse("0 L * * *");
        });
    }

    /// <summary>
    /// Verifies that <see cref="CronExpression.Parse(string)" /> throws <see cref="FormatException" /> whose message is
    /// the defect message the defect-reporting <c>TryParse</c> overload reports for the same text.
    /// </summary>
    /// <param name="kat">The malformed cron row under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(TryParseDefectKats),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Parse_WhenInvalid_ShouldThrowFormatExceptionNamingTheDefect(InvalidKat<string> kat)
    {
        _ = CronExpression.TryParse(kat.Input, out _, out string? failureMessage);

        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = CronExpression.Parse(kat.Input);
        });

        Assert.AreEqual(failureMessage, ex.Message);
        Assert.IsTrue(
            ex.Message.Contains(kat.MessageContains!, StringComparison.Ordinal),
            $"Expected message to contain '{kat.MessageContains}' but was '{ex.Message}'.");
    }

    /// <summary>
    /// Verifies that <see cref="CronExpression.Parse(string, CronFormat)" /> throws <see cref="FormatException" />
    /// whose message names the field-count defect when the text does not match the requested layout.
    /// </summary>
    [TestMethod]
    public void Parse_WhenLayoutMismatch_ShouldThrowFormatExceptionNamingTheFieldCountDefect()
    {
        _ = CronExpression.TryParse("0 9 * * *", CronFormat.WithSeconds, out _, out string? failureMessage);

        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = CronExpression.Parse("0 9 * * *", CronFormat.WithSeconds);
        });

        Assert.AreEqual(failureMessage, ex.Message);
        Assert.IsTrue(ex.Message.Contains("number of fields", StringComparison.Ordinal));
    }
}

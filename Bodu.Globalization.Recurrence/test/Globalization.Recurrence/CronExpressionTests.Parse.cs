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
}

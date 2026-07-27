// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceRuleTests.Formatting.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class RecurrenceRuleTests
{
    /// <summary>
    /// Verifies that a rule formats to its canonical text, omitting the default interval and week start.
    /// </summary>
    [TestMethod]
    public void ToString_WhenDefaultsOmitted_ShouldProduceCanonicalText()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=WEEKLY;INTERVAL=1;BYDAY=MO,WE;WKST=MO");

        Assert.AreEqual("FREQ=WEEKLY;BYDAY=MO,WE", rule.ToString());
    }

    /// <summary>
    /// Verifies that an ordinal <c>BYDAY</c> entry and an <c>UNTIL</c> bound round-trip through the canonical text.
    /// </summary>
    [TestMethod]
    public void ToString_WhenOrdinalAndUntil_ShouldRoundTrip()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=MONTHLY;BYDAY=-1FR;UNTIL=20261231T000000Z");

        Assert.AreEqual("FREQ=MONTHLY;UNTIL=20261231T000000Z;BYDAY=-1FR", rule.ToString());
        Assert.AreEqual(rule, RecurrenceRule.Parse(rule.ToString()));
    }

    /// <summary>
    /// Verifies that formatting with an unsupported specifier throws <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void ToString_WhenUnsupportedFormat_ShouldThrowFormatException()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=DAILY");

        _ = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = rule.ToString("X", null);
        });
    }
}

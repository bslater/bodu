// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WorkingDayOffsetFromRuleStrategyTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class WorkingDayOffsetFromRuleStrategyTests
{
    /// <summary>
    /// Verifies that a <see langword="null" /> concept reference throws <see cref="ArgumentNullException" /> naming the
    /// <c>notableDateRef</c> parameter.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenNotableDateRefIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new WorkingDayOffsetFromRuleStrategy(null!, null, 1);
        });

        Assert.AreEqual("notableDateRef", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a reference that does not match the identifier pattern, which the loader rejects with
    /// <c>BODU-CAL-SCHEMA</c>, throws <see cref="ArgumentException" /> naming the parameter.
    /// </summary>
    /// <param name="parameter">The name of the parameter given the reference.</param>
    /// <param name="value">The reference.</param>
    [TestMethod]
    [DataRow("notableDateRef", "")]
    [DataRow("notableDateRef", "New-Year")]
    [DataRow("notableDateRef", "new-year ")]
    [DataRow("notableDateRef", "1st")]
    [DataRow("ruleRef", "")]
    [DataRow("ruleRef", "R")]
    [DataRow("ruleRef", "r_1")]
    [DataRow("ruleRef", "rule-")]
    public void Ctor_WhenReferenceBreaksIdentifierPattern_ShouldThrowArgumentException(string parameter, string value)
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = parameter switch
            {
                "notableDateRef" => new WorkingDayOffsetFromRuleStrategy(value, "default", 1),
                "ruleRef" => new WorkingDayOffsetFromRuleStrategy("anchor", value, 1),
                _ => throw new ArgumentOutOfRangeException(nameof(parameter)),
            };
        });

        Assert.AreEqual(parameter, ex.ParamName);
    }

    /// <summary>
    /// Verifies that references matching the identifier pattern, and a <see langword="null" /> rule reference, are kept
    /// as given.
    /// </summary>
    /// <param name="notableDateRef">The concept reference.</param>
    /// <param name="ruleRef">The rule reference, or <see langword="null" />.</param>
    [TestMethod]
    [DataRow("a", null)]
    [DataRow("easter-sunday", "default")]
    [DataRow("x1-2b", "r2")]
    public void Ctor_WhenReferencesMatchIdentifierPattern_ShouldKeepThem(string notableDateRef, string? ruleRef)
    {
        var strategy = new WorkingDayOffsetFromRuleStrategy(notableDateRef, ruleRef, 1);

        Assert.AreEqual(notableDateRef, strategy.NotableDateRef);
        Assert.AreEqual(ruleRef, strategy.RuleRef);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NthWeekdayFromRuleStrategyTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NthWeekdayFromRuleStrategyTests
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
            _ = new NthWeekdayFromRuleStrategy(null!, null, DayOfWeek.Monday, 1);
        });

        Assert.AreEqual("notableDateRef", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an enumeration value its type does not define, which the loader rejects with <c>BODU-CAL-ENUM</c>,
    /// throws <see cref="ArgumentOutOfRangeException" /> naming the parameter, as the binary pack reader rejects it.
    /// </summary>
    /// <param name="parameter">The name of the parameter given the undefined value.</param>
    [TestMethod]
    [DataRow("dayOfWeek")]
    public void Ctor_WhenEnumValueIsUndefined_ShouldThrowArgumentOutOfRangeException(string parameter)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = parameter switch
            {
                "dayOfWeek" => new NthWeekdayFromRuleStrategy("anchor", null, (DayOfWeek)7, 1),
                _ => throw new ArgumentOutOfRangeException(nameof(parameter)),
            };
        });

        Assert.AreEqual(parameter, ex.ParamName);
    }

    /// <summary>
    /// Verifies that an ordinal of zero, which the loader rejects, throws <see cref="ArgumentOutOfRangeException" /> naming
    /// the <c>ordinal</c> parameter.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenOrdinalIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new NthWeekdayFromRuleStrategy("anchor", null, DayOfWeek.Monday, 0);
        });

        Assert.AreEqual("ordinal", ex.ParamName);
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
                "notableDateRef" => new NthWeekdayFromRuleStrategy(value, "default", DayOfWeek.Monday, 1),
                "ruleRef" => new NthWeekdayFromRuleStrategy("anchor", value, DayOfWeek.Monday, 1),
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
        var strategy = new NthWeekdayFromRuleStrategy(notableDateRef, ruleRef, DayOfWeek.Monday, 1);

        Assert.AreEqual(notableDateRef, strategy.NotableDateRef);
        Assert.AreEqual(ruleRef, strategy.RuleRef);
    }
}

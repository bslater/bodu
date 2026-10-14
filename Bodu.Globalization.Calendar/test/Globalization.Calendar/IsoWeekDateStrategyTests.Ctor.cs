// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IsoWeekDateStrategyTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class IsoWeekDateStrategyTests
{
    /// <summary>
    /// Verifies that a week outside 1 to 53, which the loader rejects, throws <see cref="ArgumentOutOfRangeException" /> naming
    /// the <c>week</c> parameter.
    /// </summary>
    /// <param name="week">The week.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(54)]
    public void Ctor_WhenWeekIsOutsideOneToFiftyThree_ShouldThrowArgumentOutOfRangeException(int week)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new IsoWeekDateStrategy(week, DayOfWeek.Monday);
        });

        Assert.AreEqual("week", ex.ParamName);
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
                "dayOfWeek" => new IsoWeekDateStrategy(10, (DayOfWeek)7),
                _ => throw new ArgumentOutOfRangeException(nameof(parameter)),
            };
        });

        Assert.AreEqual(parameter, ex.ParamName);
    }
}

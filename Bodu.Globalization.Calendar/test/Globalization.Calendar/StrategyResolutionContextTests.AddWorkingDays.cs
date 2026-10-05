// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StrategyResolutionContextTests.AddWorkingDays.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public partial class StrategyResolutionContextTests
{
    /// <summary>
    /// Verifies that a move of <see cref="int.MinValue" /> working days returns <see langword="null" /> once it runs past
    /// the first representable date, as any move too large to satisfy does, rather than overflowing before it starts.
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_WhenCountIsMinimumInteger_ShouldReturnNull()
    {
        var context = new StrategyResolutionContext(NotableDateResourceLoader.Load(Resource), null, "XX");

        Assert.IsNull(context.AddWorkingDays(new DateOnly(1, 1, 10), int.MinValue, "XX"));
    }

    /// <summary>
    /// Verifies that a move one working day short of <see cref="int.MinValue" /> also returns <see langword="null" />
    /// once it runs past the first representable date.
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_WhenCountRunsPastMinValue_ShouldReturnNull()
    {
        var context = new StrategyResolutionContext(NotableDateResourceLoader.Load(Resource), null, "XX");

        Assert.IsNull(context.AddWorkingDays(new DateOnly(1, 1, 10), int.MinValue + 1, "XX"));
    }
}

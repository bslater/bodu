// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarAlgorithmEdgeTests.AlgorithmGuards.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Algorithms;

namespace Bodu.Globalization.Calendar;

public partial class CalendarAlgorithmEdgeTests
{
    /// <summary>
    /// Verifies that the algorithm strategy returns no occurrence for an out-of-range year, the Tibetan Losar
    /// calculator returns <see langword="null" /> for an out-of-range year, and the Hindu festival resolver returns
    /// <see langword="null" /> for an unknown festival key.
    /// </summary>
    [TestMethod]
    public void AlgorithmGuards_WhenInputOutOfRangeOrUnknown_ShouldReturnNull()
    {
        NotableDateResource resource = NotableDateResourceLoader.Load(MinimalResource);
        var context = new StrategyResolutionContext(resource);

        Assert.IsNull(new AlgorithmDateStrategy("easter-western").Calculate(0, context));
        Assert.IsNull(TibetanLosarCalculator.Losar(0));
        Assert.IsNull(HinduLunarCalculator.Resolve("not-a-festival", 2025));
    }
}

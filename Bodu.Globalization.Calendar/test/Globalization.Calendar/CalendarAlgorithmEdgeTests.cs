// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarAlgorithmEdgeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Algorithms;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the year-range guards on the Easter calculators and the fixed-date strategy return no result rather than
/// throwing for an out-of-range year.
/// </summary>
[TestClass]
public class CalendarAlgorithmEdgeTests
{
    private const string MinimalResource = """
        <?xml version="1.0" encoding="utf-8"?>
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.min">
          <NotableDates>
            <NotableDate id="x" displayName="X" category="PublicHoliday">
              <Rules><Rule id="r"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    /// <summary>
    /// Verifies that the Western and Orthodox Easter calculators return <see langword="null" /> for a year outside the
    /// representable range.
    /// </summary>
    [TestMethod]
    public void Easter_WhenYearOutOfRange_ShouldReturnNull()
    {
        Assert.IsNull(EasterCalculator.Western(0));
        Assert.IsNull(EasterCalculator.Western(10000));
        Assert.IsNull(EasterCalculator.Orthodox(0));
        Assert.IsNull(EasterCalculator.Orthodox(10000));
    }

    /// <summary>
    /// Verifies that a fixed-date strategy yields no occurrence for an out-of-range year through both the single and
    /// all-occurrence calculation paths.
    /// </summary>
    [TestMethod]
    public void FixedDateStrategy_WhenYearOutOfRange_ShouldReturnNullAndEmpty()
    {
        NotableDateResource resource = NotableDateResourceLoader.Load(MinimalResource);
        var context = new StrategyResolutionContext(resource);
        var strategy = new FixedDateStrategy(1, 1);

        Assert.IsNull(strategy.Calculate(0, context));
        Assert.AreEqual(0, strategy.CalculateAll(0, context).Count);
    }
}

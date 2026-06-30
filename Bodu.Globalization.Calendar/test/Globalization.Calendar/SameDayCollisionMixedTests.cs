// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SameDayCollisionMixedTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that same-day collision resolution keeps a lone occurrence untouched while reducing a colliding group,
/// exercising the single-occurrence branch of the collision loop.
/// </summary>
[TestClass]
public class SameDayCollisionMixedTests
{
    private const string Xml = """
        <?xml version="1.0" encoding="utf-8"?>
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.collide-mixed">
          <ResolutionPolicy duplicatePolicy="Error" sameDayCollisionPolicy="HighestPriorityOnly" priorityDirection="HigherWins" />
          <NotableDates>
            <NotableDate id="a" displayName="A" category="PublicHoliday">
              <Rules><Rule id="r" priority="100"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
            </NotableDate>
            <NotableDate id="b" displayName="B" category="PublicHoliday">
              <Rules><Rule id="r" priority="50"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
            </NotableDate>
            <NotableDate id="c" displayName="C" category="PublicHoliday">
              <Rules><Rule id="r"><Strategy><Fixed month="January" day="5" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    /// <summary>
    /// Verifies that, under a highest-priority-only policy, a colliding pair on 1 January resolves to the higher
    /// priority while the lone occurrence on 5 January is preserved.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenMixedSingleAndCollidingDates_ShouldKeepLoneAndReduceCollision()
    {
        var service = new NotableDateService(NotableDateResourceLoader.Load(Xml));

        IReadOnlyList<NotableDate> results = service.Resolve(
            new DateRange(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 7)), "XX");

        Assert.Contains(r => r.NotableDateId == "c", results, "Lone occurrence should be kept.");
        Assert.Contains(r => r.NotableDateId == "a", results, "Higher-priority collision winner should be kept.");
        Assert.DoesNotContain(r => r.NotableDateId == "b", results, "Lower-priority collision loser should be dropped.");
    }
}

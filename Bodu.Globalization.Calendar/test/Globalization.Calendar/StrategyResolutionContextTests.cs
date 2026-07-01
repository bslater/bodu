// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StrategyResolutionContextTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies <see cref="StrategyResolutionContext.ResolveReference" /> resolves a sole-rule reference and returns
/// <see langword="null" /> for missing, unknown-rule, and ambiguous references.
/// </summary>
[TestClass]
public class StrategyResolutionContextTests
{
    private const string Resource = """
        <?xml version="1.0" encoding="utf-8"?>
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.refs">
          <NotableDates>
            <NotableDate id="single" displayName="Single" category="PublicHoliday">
              <Rules><Rule id="r"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
            </NotableDate>
            <NotableDate id="multi" displayName="Multi" category="PublicHoliday">
              <Rules>
                <Rule id="r1"><Strategy><Fixed month="March" day="1" /></Strategy></Rule>
                <Rule id="r2"><Strategy><Fixed month="March" day="2" /></Strategy></Rule>
              </Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    /// <summary>
    /// Verifies that the single-argument constructor resolves a sole-rule reference to its occurrence.
    /// </summary>
    [TestMethod]
    public void ResolveReference_WhenSoleRule_ShouldResolveOccurrence()
    {
        NotableDateResource resource = NotableDateResourceLoader.Load(Resource);
        var context = new StrategyResolutionContext(resource);

        Assert.AreEqual(new DateOnly(2025, 1, 1), context.ResolveReference("single", null, 2025));
        Assert.AreEqual(new DateOnly(2025, 1, 1), context.ResolveReference("single", "r", 2025));
    }

    /// <summary>
    /// Verifies that a missing concept, an unknown rule, and an ambiguous multi-rule reference all resolve to
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ResolveReference_WhenUnresolvable_ShouldReturnNull()
    {
        NotableDateResource resource = NotableDateResourceLoader.Load(Resource);
        var context = new StrategyResolutionContext(resource);

        Assert.IsNull(context.ResolveReference("does-not-exist", null, 2025));
        Assert.IsNull(context.ResolveReference("single", "no-such-rule", 2025));
        Assert.IsNull(context.ResolveReference("multi", null, 2025));
    }
}

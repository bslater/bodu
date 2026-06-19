// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TerritoryCodeTests.Resolve.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class TerritoryCodeTests
{
    /// <summary>
    /// Verifies that a <see cref="TerritoryCode" /> flows into the string-based resolution surface via its implicit
    /// conversion, resolving a subdivision-scoped occurrence for the matching subdivision and nothing for a sibling.
    /// </summary>
    /// <param name="territory">The subdivision code passed to the resolver.</param>
    /// <param name="expectedCount">The expected number of resolved occurrences.</param>
    [TestMethod]
    [DataRow("AU-NSW", 1)]  // the scoped subdivision resolves the occurrence
    [DataRow("AU-VIC", 0)]  // a sibling subdivision resolves nothing
    public void Resolve_WhenPassedTerritoryCode_ShouldResolveThroughStringSurface(string territory, int expectedCount)
    {
        const string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.territory-interop">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <NotableDates>
            <NotableDate id="state-holiday" displayName="State Holiday" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="x"><Applicability><Territory code="AU-NSW" /></Applicability><Strategy><Fixed month="January" day="26" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        NotableDateService service = new(NotableDateResourceLoader.Load(xml));

        Assert.HasCount(expectedCount, service.Resolve(new DateOnly(2025, 1, 26), TerritoryCode.Parse(territory)));
    }
}

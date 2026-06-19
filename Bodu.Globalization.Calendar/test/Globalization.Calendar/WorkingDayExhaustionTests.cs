// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WorkingDayExhaustionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that the working-day and non-working-day traversal helpers throw when no qualifying day can be found within
/// the traversal guard.
/// </summary>
[TestClass]
public partial class WorkingDayExhaustionTests
{
    // A resource whose only holiday applies to territory "ZZ", so territory "XX" has no non-working holidays.
    private const string Xml = """
        <?xml version="1.0" encoding="utf-8"?>
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.exhaust">
          <NotableDates>
            <NotableDate id="zz" displayName="ZZ Day" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules>
                <Rule id="r">
                  <Applicability calendar="Gregorian"><Territory code="ZZ" /></Applicability>
                  <Strategy><Fixed month="January" day="1" /></Strategy>
                </Rule>
              </Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    private static INotableDateService Service =>
        new NotableDateService(NotableDateResourceLoader.Load(Xml));

}

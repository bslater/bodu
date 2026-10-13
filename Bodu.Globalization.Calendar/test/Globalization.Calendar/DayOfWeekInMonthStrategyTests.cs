// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfWeekInMonthStrategyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Contains unit tests for <see cref="DayOfWeekInMonthStrategy" /> called directly, outside the resolution pipeline.
/// </summary>
[TestClass]
public sealed partial class DayOfWeekInMonthStrategyTests
{
    /// <summary>A resource with one fixed rule, enough to build a resolution context.</summary>
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
    /// Creates a resolution context over <see cref="MinimalResource" />.
    /// </summary>
    /// <returns>The context.</returns>
    private static StrategyResolutionContext CreateContext() =>
        new(NotableDateResourceLoader.Load(MinimalResource));
}

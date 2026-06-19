// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateNameLocalizerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that <see cref="NotableDateNameLocalizer" /> and the <c>Localize</c> extensions apply culture-specific
/// display names with parent-culture and invariant fallback.
/// </summary>
[TestClass]
public sealed partial class NotableDateNameLocalizerTests
{
    /// <summary>
    /// A single-concept resource used by the localization tests.
    /// </summary>
    private const string Xml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.l10n">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <NotableDates>
        <NotableDate id="new-years-day" displayName="New Year's Day" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// Resolves the single New Year's Day occurrence for 2026.
    /// </summary>
    /// <returns>The resolved occurrence with its invariant display name.</returns>
    private static NotableDate NewYearsDay()
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(Xml));
        return service.Resolve(new DateOnly(2026, 1, 1), "XX").Single();
    }

}

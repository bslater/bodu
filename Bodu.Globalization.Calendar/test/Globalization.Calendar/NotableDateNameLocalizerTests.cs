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
public sealed class NotableDateNameLocalizerTests
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

    /// <summary>
    /// Verifies that a registered culture-specific name is returned, including when the requested culture is a child of
    /// the registered one.
    /// </summary>
    [TestMethod]
    public void Localize_WhenCultureRegistered_ShouldReturnLocalizedName()
    {
        NotableDateNameLocalizer localizer = new NotableDateNameLocalizer()
            .Register("new-years-day", CultureInfo.GetCultureInfo("fr"), "Jour de l'An");

        NotableDate localized = NewYearsDay().Localize(localizer, CultureInfo.GetCultureInfo("fr-FR"));

        Assert.AreEqual("Jour de l'An", localized.DisplayName);
    }

    /// <summary>
    /// Verifies that the lookup falls back to a parent culture when no exact culture is registered.
    /// </summary>
    [TestMethod]
    public void GetDisplayName_WhenOnlyParentRegistered_ShouldFallBackToParent()
    {
        NotableDateNameLocalizer localizer = new NotableDateNameLocalizer()
            .Register("new-years-day", CultureInfo.GetCultureInfo("fr"), "Jour de l'An");

        string? name = localizer.GetDisplayName(NewYearsDay(), CultureInfo.GetCultureInfo("fr-CA"));

        Assert.AreEqual("Jour de l'An", name);
    }

    /// <summary>
    /// Verifies that an invariant-culture registration acts as the default when no more specific culture matches.
    /// </summary>
    [TestMethod]
    public void GetDisplayName_WhenInvariantRegistered_ShouldUseAsDefault()
    {
        NotableDateNameLocalizer localizer = new NotableDateNameLocalizer()
            .Register("new-years-day", CultureInfo.InvariantCulture, "Default Name");

        string? name = localizer.GetDisplayName(NewYearsDay(), CultureInfo.GetCultureInfo("de"));

        Assert.AreEqual("Default Name", name);
    }

    /// <summary>
    /// Verifies that an unmatched culture leaves the occurrence's display name unchanged.
    /// </summary>
    [TestMethod]
    public void Localize_WhenNoRegistration_ShouldKeepOriginalName()
    {
        NotableDateNameLocalizer localizer = new();
        NotableDate original = NewYearsDay();

        NotableDate localized = original.Localize(localizer, CultureInfo.GetCultureInfo("de"));

        Assert.AreEqual("New Year's Day", localized.DisplayName);
        Assert.AreSame(original, localized);
    }

    /// <summary>
    /// Verifies that the list overload localizes every occurrence it is given.
    /// </summary>
    [TestMethod]
    public void Localize_List_ShouldLocalizeEachOccurrence()
    {
        NotableDateNameLocalizer localizer = new NotableDateNameLocalizer()
            .Register("new-years-day", CultureInfo.GetCultureInfo("fr"), "Jour de l'An");

        NotableDateService service = new(NotableDateResourceLoader.Load(Xml));
        IReadOnlyList<NotableDate> resolved = service.Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), "XX");

        IReadOnlyList<NotableDate> localized = resolved.Localize(localizer, CultureInfo.GetCultureInfo("fr"));

        Assert.AreEqual("Jour de l'An", localized.Single().DisplayName);
    }

    /// <summary>
    /// Verifies that localizing with a null localizer throws.
    /// </summary>
    [TestMethod]
    public void Localize_WhenLocalizerIsNull_ShouldThrowArgumentNullException()
    {
        NotableDate occurrence = NewYearsDay();

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = occurrence.Localize(null!, CultureInfo.InvariantCulture);
        });
    }
}

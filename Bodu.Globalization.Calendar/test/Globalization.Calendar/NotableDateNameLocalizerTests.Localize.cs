// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateNameLocalizerTests.Localize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateNameLocalizerTests
{
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

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateNameLocalizerTests.GetDisplayName.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateNameLocalizerTests
{
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
}

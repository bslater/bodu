// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResolutionServiceProductionEasterTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Data;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Tests the new resolution service against production XML rules that depend on the real Gregorian Easter algorithm.
/// </summary>
[TestClass]
public sealed class NotableDateResolutionServiceProductionEasterTests
{
    /// <summary>
    /// Verifies that the production United Kingdom XML resolves Easter-relative bank holidays through the real Easter
    /// algorithm.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenUsingProductionGbXml_ShouldResolveGregorianEasterRelativeDates_ForYear2024()
    {
        NotableDateResolutionService service = BuildUnitedKingdomResolutionService();

        IReadOnlyList<NotableDate> actual = service.GetNotableDates(2024, territoryCode: "GB");

        AssertContains(actual, "Good Friday", new DateTime(2024, 3, 29));
        AssertContains(actual, "Easter Monday", new DateTime(2024, 4, 1));
    }

    /// <summary>
    /// Verifies another year so the test proves the date is calculated, not hard-coded in the XML.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenUsingProductionGbXml_ShouldResolveGregorianEasterRelativeDates_ForYear2025()
    {
        NotableDateResolutionService service = BuildUnitedKingdomResolutionService();

        IReadOnlyList<NotableDate> actual = service.GetNotableDates(2025, territoryCode: "GB");

        AssertContains(actual, "Good Friday", new DateTime(2025, 4, 18));
        AssertContains(actual, "Easter Monday", new DateTime(2025, 4, 21));
    }

    /// <summary>
    /// Verifies that an observed-date query can resolve a production XML rule whose date is derived from Easter Sunday.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenQueryingProductionGbEasterMondayObservedDate_ShouldReturnEasterMonday()
    {
        NotableDateResolutionService service = BuildUnitedKingdomResolutionService();

        IReadOnlyList<NotableDate> actual = service.GetNotableDates(
            new DateTime(2024, 4, 1),
            territoryCode: "GB");

        Assert.IsTrue(
            actual.Any(date =>
                date.Name == "Easter Monday" &&
                date.Date == new DateTime(2024, 4, 1) &&
                date.TerritoryCode == "GB"),
            "The production GB XML should resolve Easter Monday for 2024-04-01 through the registered Easter algorithm.");
    }

    /// <summary>
    /// Verifies that a narrow window before Easter can still resolve a production XML rule that depends on the later Easter
    /// anchor.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenQueryingProductionGbGoodFridayObservedDate_ShouldResolveFromLaterEasterAnchor()
    {
        NotableDateResolutionService service = BuildUnitedKingdomResolutionService();

        IReadOnlyList<NotableDate> actual = service.GetNotableDates(
            new DateTime(2024, 3, 29),
            territoryCode: "GB");

        Assert.IsTrue(
            actual.Any(date =>
                date.Name == "Good Friday" &&
                date.Date == new DateTime(2024, 3, 29) &&
                date.TerritoryCode == "GB"),
            "The production GB XML should resolve Good Friday for 2024-03-29 from the later Easter Sunday anchor.");
    }

    /// <summary>
    /// Verifies known-answer dates for the production Gregorian Easter algorithm itself.
    /// </summary>
    [TestMethod]
    public void GregorianEasterSundayNotableDateProvider_WhenKnownYearsRequested_ShouldReturnKnownGregorianDates()
    {
        GregorianEasterSundayAlgorithmAdapter algorithm = new();

        Assert.AreEqual(new DateTime(2024, 3, 31), algorithm.GetDate(2024));
        Assert.AreEqual(new DateTime(2025, 4, 20), algorithm.GetDate(2025));
        Assert.AreEqual(new DateTime(2026, 4, 5), algorithm.GetDate(2026));
    }

    private static NotableDateResolutionService BuildUnitedKingdomResolutionService()
    {
        NotableDateAlgorithmRegistry registry = new NotableDateAlgorithmRegistry()
            .Register("easter-sunday", new GregorianEasterSundayAlgorithmAdapter());

        return new NotableDateResolutionService(
            ruleProviders: [EuropeCalendarData.CreateUnitedKingdomProvider()],
            algorithmRegistry: registry,
            workingWeek: WorkingDaysOfWeek.MondayToFriday);
    }

    private static void AssertContains(
        IReadOnlyList<NotableDate> actual,
        string name,
        DateTime expectedDate)
    {
        Assert.IsTrue(
            actual.Any(date =>
                date.Name == name &&
                date.Date == expectedDate &&
                date.TerritoryCode == "GB"),
            $"{name} should resolve to {expectedDate:yyyy-MM-dd} from the production GB XML.");
    }

    private sealed class GregorianEasterSundayAlgorithmAdapter
        : INotableDateAlgorithm
    {
        private readonly Bodu.Globalization.Calendar.Providers.GregorianEasterSundayNotableDateProvider provider = new();

        public DateTime? GetDate(int year, System.Globalization.Calendar? calendar = null)
        {
            if (!provider.SupportsYear(year))
                return null;

            return provider
                .GetDates(year)
                .SingleOrDefault(date => date.Name == "Easter Sunday")
                ?.Date;
        }
    }
}

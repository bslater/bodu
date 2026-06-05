// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateProviderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that a code-first <see cref="INotableDateProvider" /> contributes finished occurrences to resolution
/// alongside resource-authored rules, that its occurrences are clamped to the requested range and subject to the query
/// filter, and that they are absent when no provider is registered.
/// </summary>
[TestClass]
public sealed class NotableDateProviderTests
{
    private const string Territory = "XX";

    /// <summary>
    /// A resource with a single authored holiday on 1 January.
    /// </summary>
    private const string Xml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.a5">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <NotableDates>
        <NotableDate id="new-year" displayName="New Year's Day" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// A provider that contributes a single "Founders Day" observance on a fixed date.
    /// </summary>
    private sealed class FoundersDayProvider : INotableDateProvider
    {
        private readonly DateOnly _date;

        public FoundersDayProvider(DateOnly date) =>
            this._date = date;

        /// <inheritdoc />
        public IEnumerable<NotableDate> GetNotableDates(DateRange range, string territory)
        {
            yield return new NotableDate(
                this._date,
                this._date,
                false,
                new NotableDateRuleIdentity("provider", "founders-day", "x"),
                "Founders Day",
                territory,
                NotableDateCategory.Observance,
                0,
                1,
                false,
                Array.Empty<string>(),
                null,
                null);
        }
    }

    /// <summary>
    /// Builds a service over the fixture, optionally registering a Founders Day provider.
    /// </summary>
    /// <param name="provider">The provider to register, or <see langword="null" /> for none.</param>
    /// <returns>A service over the fixture.</returns>
    private static INotableDateService Build(INotableDateProvider? provider) =>
        new NotableDateService(
            NotableDateResourceLoader.Load(Xml),
            null,
            null,
            null,
            null,
            provider is null ? null : new[] { provider });

    /// <summary>
    /// Verifies that a provider's occurrence is resolved alongside the resource's authored holiday.
    /// </summary>
    [TestMethod]
    public void Provider_ContributesOccurrenceAlongsideResource()
    {
        INotableDateService service = Build(new FoundersDayProvider(new DateOnly(2025, 3, 15)));

        IReadOnlyList<NotableDate> results = service.Resolve(new DateRange(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31)), Territory);

        Assert.IsTrue(results.Any(r => r.NotableDateId == "new-year"));
        NotableDate founders = results.Single(r => r.NotableDateId == "founders-day");
        Assert.AreEqual(new DateOnly(2025, 3, 15), founders.Date);
    }

    /// <summary>
    /// Verifies that a provider occurrence outside the requested range is excluded.
    /// </summary>
    [TestMethod]
    public void Provider_OccurrenceOutsideRange_IsExcluded()
    {
        INotableDateService service = Build(new FoundersDayProvider(new DateOnly(2025, 3, 15)));

        IReadOnlyList<NotableDate> january = service.Resolve(new DateRange(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31)), Territory);

        Assert.IsFalse(january.Any(r => r.NotableDateId == "founders-day"));
        Assert.IsTrue(january.Any(r => r.NotableDateId == "new-year"));
    }

    /// <summary>
    /// Verifies that the query filter applies to provider occurrences just as it does to resource occurrences.
    /// </summary>
    [TestMethod]
    public void Provider_OccurrenceRespectsQueryFilter()
    {
        INotableDateService service = Build(new FoundersDayProvider(new DateOnly(2025, 3, 15)));
        DateRange year = new(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));

        IReadOnlyList<NotableDate> observances = service.Resolve(year, Territory, NotableDateFilter.ForCategory(NotableDateCategory.Observance));
        Assert.IsTrue(observances.Any(r => r.NotableDateId == "founders-day"));
        Assert.IsFalse(observances.Any(r => r.NotableDateId == "new-year"));

        IReadOnlyList<NotableDate> holidays = service.Resolve(year, Territory, NotableDateFilter.ForCategory(NotableDateCategory.PublicHoliday));
        Assert.IsFalse(holidays.Any(r => r.NotableDateId == "founders-day"));
    }

    /// <summary>
    /// Verifies that without a registered provider only resource occurrences resolve.
    /// </summary>
    [TestMethod]
    public void WithoutProvider_OnlyResourceOccurrencesResolve()
    {
        INotableDateService service = Build(null);

        IReadOnlyList<NotableDate> results = service.Resolve(new DateRange(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31)), Territory);

        Assert.IsFalse(results.Any(r => r.NotableDateId == "founders-day"));
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WorkingDaysOfWeekTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;

namespace Bodu;

[TestClass]
public class WorkingDaysOfWeekTests
{

    /// <summary>
    /// Provides the canonical mapping between <see cref="WorkingDaysOfWeek" /> values and their
    /// expected selected <see cref="DayOfWeek" /> sets.
    /// </summary>
    public static IEnumerable<object[]> GetNamedPresetTestData()
    {
        yield return new object[] { WorkingDaysOfWeek.MondayToFriday, new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday } };
        yield return new object[] { WorkingDaysOfWeek.MondayToSaturday, new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday } };
        yield return new object[] { WorkingDaysOfWeek.MondayToThursdayAndSaturday, new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Saturday } };
        yield return new object[] { WorkingDaysOfWeek.SaturdayToThursday, new[] { DayOfWeek.Saturday, DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday } };
        yield return new object[] { WorkingDaysOfWeek.SaturdayToWednesday, new[] { DayOfWeek.Saturday, DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday } };
        yield return new object[] { WorkingDaysOfWeek.SundayToFriday, new[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday } };
        yield return new object[] { WorkingDaysOfWeek.SundayToThursday, new[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday } };
        yield return new object[] { WorkingDaysOfWeek.AllDays, new[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday } };
    }

    /// <summary>
    /// Verifies that <see cref="WorkingDaysOfWeekExtensions.ToWeekPattern" /> throws
    /// <see cref="ArgumentException" /> when called with <see cref="WorkingDaysOfWeek.Custom" />.
    /// </summary>
    [TestMethod]
    public void ToWeekPattern_WhenCustom_ShouldThrowExactlyArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = WorkingDaysOfWeek.Custom.ToWeekPattern();
        });
    }

    /// <summary>
    /// Verifies that <see cref="WorkingDaysOfWeekExtensions.ToWeekPattern" /> maps each named preset to a
    /// <see cref="WeekPattern" /> that selects exactly the expected days.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetNamedPresetTestData))]
    public void ToWeekPattern_WhenNamedPreset_ShouldSelectExpectedDays(WorkingDaysOfWeek value, DayOfWeek[] expectedDays)
    {
        var pattern = value.ToWeekPattern();

        Assert.AreEqual(expectedDays.Length, pattern.Count);
        foreach (DayOfWeek day in expectedDays)
            Assert.IsTrue(pattern.Contains(day), $"Expected pattern for {value} to include {day}.");
    }

    /// <summary>
    /// Verifies that <see cref="WorkingDaysOfWeekExtensions.ToWeekPattern" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the enum value is not defined.
    /// </summary>
    [TestMethod]
    public void ToWeekPattern_WhenUndefinedEnumValue_ShouldThrowExactlyArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = ((WorkingDaysOfWeek)99).ToWeekPattern();
        });
    }

    /// <summary>
    /// Verifies that <see cref="WorkingDaysOfWeekExtensions.ToWorkingDaysOfWeek" /> returns
    /// <see cref="WorkingDaysOfWeek.Custom" /> for a <see cref="WeekPattern" /> that does not match any named preset.
    /// </summary>
    [TestMethod]
    public void ToWorkingDaysOfWeek_WhenNotANamedPreset_ShouldReturnCustom()
    {
        var oddPattern = new WeekPattern(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);

        var value = oddPattern.ToWorkingDaysOfWeek();

        Assert.AreEqual(WorkingDaysOfWeek.Custom, value);
    }

    /// <summary>
    /// Verifies that <see cref="WorkingDaysOfWeekExtensions.ToWorkingDaysOfWeek" /> round-trips every named
    /// preset back to its original <see cref="WorkingDaysOfWeek" /> value.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetNamedPresetTestData))]
    public void ToWorkingDaysOfWeek_WhenRoundTripFromNamedPreset_ShouldReturnOriginal(WorkingDaysOfWeek value, DayOfWeek[] _)
    {
        var pattern = value.ToWeekPattern();

        var roundTripped = pattern.ToWorkingDaysOfWeek();

        Assert.AreEqual(value, roundTripped);
    }

    /// <summary>
    /// Verifies that <see cref="WorkingDaysOfWeekExtensions.TryGetWorkingDaysOfWeek" /> returns
    /// <see langword="true" /> and the expected value for every named preset.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetNamedPresetTestData))]
    public void TryGetWorkingDaysOfWeek_WhenNamedPreset_ShouldReturnTrueAndExpectedValue(WorkingDaysOfWeek value, DayOfWeek[] _)
    {
        var pattern = value.ToWeekPattern();

        var success = pattern.TryGetWorkingDaysOfWeek(out WorkingDaysOfWeek actual);

        Assert.IsTrue(success);
        Assert.AreEqual(value, actual);
    }

    /// <summary>
    /// Verifies that <see cref="WorkingDaysOfWeekExtensions.TryGetWorkingDaysOfWeek" /> returns
    /// <see langword="false" /> and yields <see cref="WorkingDaysOfWeek.Custom" /> when no named preset matches.
    /// </summary>
    [TestMethod]
    public void TryGetWorkingDaysOfWeek_WhenNotANamedPreset_ShouldReturnFalseAndCustom()
    {
        var oddPattern = new WeekPattern(DayOfWeek.Tuesday, DayOfWeek.Thursday);

        var success = oddPattern.TryGetWorkingDaysOfWeek(out WorkingDaysOfWeek value);

        Assert.IsFalse(success);
        Assert.AreEqual(WorkingDaysOfWeek.Custom, value);
    }

    /// <summary>
    /// Verifies that <see cref="WorkingDaysOfWeekExtensions.ToWeekPattern(WorkingDaysOfWeek)" /> maps
    /// <see cref="WorkingDaysOfWeek.AllDays" /> to a <see cref="WeekPattern" /> selecting all seven days.
    /// </summary>
    [TestMethod]
    public void ToWeekPattern_WhenAllDays_ShouldSelectAllSevenDays()
    {
        var pattern = WorkingDaysOfWeek.AllDays.ToWeekPattern();

        Assert.AreEqual(7, pattern.Count);
        for (var i = 0; i < 7; i++)
            Assert.IsTrue(pattern.Contains((DayOfWeek)i), $"Expected AllDays to include {(DayOfWeek)i}.");
    }

    /// <summary>
    /// Verifies that <see cref="WorkingDaysOfWeekExtensions.ToWeekPattern(WorkingDaysOfWeek, IWeekendDefinitionProvider?)" />
    /// routes <see cref="WorkingDaysOfWeek.Custom" /> through the supplied provider.
    /// </summary>
    [TestMethod]
    public void ToWeekPattern_WhenCustomWithProvider_ShouldReturnProviderImpliedPattern()
    {
        IWeekendDefinitionProvider provider = new FridayOnlyWeekendProvider();

        var pattern = WorkingDaysOfWeek.Custom.ToWeekPattern(provider);

        Assert.AreEqual(6, pattern.Count);
        Assert.IsFalse(pattern.Contains(DayOfWeek.Friday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Saturday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Sunday));
    }

    /// <summary>
    /// Verifies that <see cref="WorkingDaysOfWeekExtensions.ToWeekPattern(WorkingDaysOfWeek, IWeekendDefinitionProvider?)" />
    /// throws <see cref="ArgumentNullException" /> when called with <see cref="WorkingDaysOfWeek.Custom" /> and a <see langword="null" /> provider.
    /// </summary>
    [TestMethod]
    public void ToWeekPattern_WhenCustomAndProviderIsNull_ShouldThrowExactlyArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = WorkingDaysOfWeek.Custom.ToWeekPattern(null);
        });
    }

    /// <summary>
    /// Verifies that <see cref="IWeekendDefinitionProviderExtensions.ToWeekPattern" /> projects the provider's weekend
    /// definition onto the complement <see cref="WeekPattern" />.
    /// </summary>
    [TestMethod]
    public void IWeekendDefinitionProviderToWeekPattern_WhenProviderReturnsFridayOnly_ShouldSelectAllOtherDays()
    {
        IWeekendDefinitionProvider provider = new FridayOnlyWeekendProvider();

        var pattern = provider.ToWeekPattern();

        Assert.AreEqual(6, pattern.Count);
        Assert.IsFalse(pattern.Contains(DayOfWeek.Friday));
    }

    /// <summary>
    /// Verifies that <see cref="IWeekendDefinitionProviderExtensions.ToWeekPattern" /> throws
    /// <see cref="ArgumentNullException" /> when the provider argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void IWeekendDefinitionProviderToWeekPattern_WhenProviderIsNull_ShouldThrowExactlyArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ((IWeekendDefinitionProvider)null!).ToWeekPattern();
        });
    }

    /// <summary>
    /// Test helper that flags Friday as the weekend and every other day as a working day.
    /// </summary>
    private sealed class FridayOnlyWeekendProvider
        : IWeekendDefinitionProvider
    {
        /// <inheritdoc />
        public bool IsWeekend(DayOfWeek dayOfWeek) => dayOfWeek == DayOfWeek.Friday;
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WorkingDaysOfWeekTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
    /// Verifies that the enum does not carry the <see cref="FlagsAttribute" />: each member names one whole working
    /// week, so members are not combined.
    /// </summary>
    [TestMethod]
    public void Type_ShouldNotDeclareFlagsAttribute() => Assert.IsFalse(typeof(WorkingDaysOfWeek).IsDefined(typeof(FlagsAttribute), inherit: false));

    /// <summary>
    /// Verifies that <see cref="WorkingDaysOfWeekExtensions.ToDayOfWeekSet" /> throws
    /// <see cref="ArgumentException" /> when called with <see cref="WorkingDaysOfWeek.Custom" />.
    /// </summary>
    [TestMethod]
    public void ToDayOfWeekSet_WhenCustom_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = WorkingDaysOfWeek.Custom.ToDayOfWeekSet();
        });
    }

    /// <summary>
    /// Verifies that <see cref="WorkingDaysOfWeekExtensions.ToDayOfWeekSet" /> maps each named preset to a
    /// <see cref="DayOfWeekSet" /> that selects exactly the expected days.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetNamedPresetTestData))]
    public void ToDayOfWeekSet_WhenNamedPreset_ShouldSelectExpectedDays(WorkingDaysOfWeek value, DayOfWeek[] expectedDays)
    {
        var days = value.ToDayOfWeekSet();

        Assert.AreEqual(expectedDays.Length, days.Count);
        foreach (DayOfWeek day in expectedDays)
            Assert.IsTrue(days.Contains(day), $"Expected the set for {value} to include {day}.");
    }

    /// <summary>
    /// Verifies that <see cref="WorkingDaysOfWeekExtensions.ToDayOfWeekSet" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the enum value is not defined.
    /// </summary>
    [TestMethod]
    public void ToDayOfWeekSet_WhenUndefinedEnumValue_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = ((WorkingDaysOfWeek)99).ToDayOfWeekSet();
        });
    }

    /// <summary>
    /// Verifies that <see cref="WorkingDaysOfWeekExtensions.ToWorkingDaysOfWeek" /> returns
    /// <see cref="WorkingDaysOfWeek.Custom" /> for a <see cref="DayOfWeekSet" /> that does not match any named preset.
    /// </summary>
    [TestMethod]
    public void ToWorkingDaysOfWeek_WhenNotANamedPreset_ShouldReturnCustom()
    {
        var oddPattern = new DayOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);

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
        var days = value.ToDayOfWeekSet();

        var roundTripped = days.ToWorkingDaysOfWeek();

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
        var days = value.ToDayOfWeekSet();

        bool success = days.TryGetWorkingDaysOfWeek(out WorkingDaysOfWeek actual);

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
        var oddPattern = new DayOfWeekSet(DayOfWeek.Tuesday, DayOfWeek.Thursday);

        bool success = oddPattern.TryGetWorkingDaysOfWeek(out WorkingDaysOfWeek value);

        Assert.IsFalse(success);
        Assert.AreEqual(WorkingDaysOfWeek.Custom, value);
    }

    /// <summary>
    /// Verifies that <see cref="WorkingDaysOfWeekExtensions.ToDayOfWeekSet(WorkingDaysOfWeek)" /> maps
    /// <see cref="WorkingDaysOfWeek.AllDays" /> to a <see cref="DayOfWeekSet" /> selecting all seven days.
    /// </summary>
    [TestMethod]
    public void ToDayOfWeekSet_WhenAllDays_ShouldSelectAllSevenDays()
    {
        var days = WorkingDaysOfWeek.AllDays.ToDayOfWeekSet();

        Assert.AreEqual(7, days.Count);
        for (int i = 0; i < 7; i++)
            Assert.IsTrue(days.Contains((DayOfWeek)i), $"Expected AllDays to include {(DayOfWeek)i}.");
    }

    /// <summary>
    /// Verifies that <see cref="WorkingDaysOfWeekExtensions.ToDayOfWeekSet(WorkingDaysOfWeek, IWeekendDefinitionProvider?)" />
    /// routes <see cref="WorkingDaysOfWeek.Custom" /> through the supplied provider.
    /// </summary>
    [TestMethod]
    public void ToDayOfWeekSet_WhenCustomWithProvider_ShouldReturnProviderImpliedSet()
    {
        IWeekendDefinitionProvider provider = new FridayOnlyWeekendProvider();

        var days = WorkingDaysOfWeek.Custom.ToDayOfWeekSet(provider);

        Assert.AreEqual(6, days.Count);
        Assert.IsFalse(days.Contains(DayOfWeek.Friday));
        Assert.IsTrue(days.Contains(DayOfWeek.Saturday));
        Assert.IsTrue(days.Contains(DayOfWeek.Sunday));
    }

    /// <summary>
    /// Verifies that <see cref="WorkingDaysOfWeekExtensions.ToDayOfWeekSet(WorkingDaysOfWeek, IWeekendDefinitionProvider?)" />
    /// throws <see cref="ArgumentNullException" /> when called with <see cref="WorkingDaysOfWeek.Custom" /> and a <see langword="null" /> provider.
    /// </summary>
    [TestMethod]
    public void ToDayOfWeekSet_WhenCustomAndProviderIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = WorkingDaysOfWeek.Custom.ToDayOfWeekSet(null);
        });
    }

    /// <summary>
    /// Verifies that <see cref="IWeekendDefinitionProviderExtensions.ToWorkingWeek" /> projects the provider's weekend
    /// definition onto the complement <see cref="DayOfWeekSet" />.
    /// </summary>
    [TestMethod]
    public void ToWorkingWeek_WhenProviderReturnsFridayOnly_ShouldSelectAllOtherDays()
    {
        IWeekendDefinitionProvider provider = new FridayOnlyWeekendProvider();

        var days = provider.ToWorkingWeek();

        Assert.AreEqual(6, days.Count);
        Assert.IsFalse(days.Contains(DayOfWeek.Friday));
    }

    /// <summary>
    /// Verifies that <see cref="IWeekendDefinitionProviderExtensions.ToWorkingWeek" /> throws
    /// <see cref="ArgumentNullException" /> when the provider argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ToWorkingWeek_WhenProviderIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ((IWeekendDefinitionProvider)null!).ToWorkingWeek();
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

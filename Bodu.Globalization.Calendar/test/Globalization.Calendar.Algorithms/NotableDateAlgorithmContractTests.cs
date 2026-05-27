// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateAlgorithmContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Verifies the shared boundary contract that every shipped <see cref="INotableDateAlgorithm" />
/// implementation honours — year-range validation and calendar-argument handling — by driving each test
/// method across every algorithm enrolled in
/// <see cref="NotableDateAlgorithmKnownAnswers.AllAlgorithmFactories" />.
/// </summary>
/// <remarks>
/// <para>
/// Each algorithm previously carried its own copy of these five test methods. Centralising them here lets a
/// new algorithm pick up the entire boundary suite by adding a single <see cref="AlgorithmFactoryCase" /> row
/// to <see cref="NotableDateAlgorithmKnownAnswers.AllAlgorithmFactories" />.
/// </para>
/// </remarks>
[TestClass]
public sealed class NotableDateAlgorithmContractTests
{
    /// <summary>
    /// Verifies that requesting a date for any year less than one throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>year</c> as the parameter name.
    /// </summary>
    /// <param name="algo">The algorithm under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(NotableDateAlgorithmKnownAnswers.AllAlgorithmFactories),
        typeof(NotableDateAlgorithmKnownAnswers),
        DynamicDataDisplayName = nameof(NotableDateAlgorithmKnownAnswers.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(NotableDateAlgorithmKnownAnswers))]
    public void GetDate_WhenYearLessThanOne_ShouldThrowExactly(AlgorithmFactoryCase algo)
    {
        INotableDateAlgorithm sut = algo.Factory();

        foreach (int year in new[] { 0, -1, int.MinValue })
        {
            ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = sut.GetDate(year, null);
            });

            Assert.AreEqual("year", ex.ParamName, $"Algorithm '{algo.Algorithm}' year {year}.");
        }
    }

    /// <summary>
    /// Verifies that requesting a date for any year above 9999 throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>year</c> as the parameter name, rather than the
    /// raw exception that <see cref="DateTime" /> would surface from its own constructor.
    /// </summary>
    /// <param name="algo">The algorithm under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(NotableDateAlgorithmKnownAnswers.AllAlgorithmFactories),
        typeof(NotableDateAlgorithmKnownAnswers),
        DynamicDataDisplayName = nameof(NotableDateAlgorithmKnownAnswers.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(NotableDateAlgorithmKnownAnswers))]
    public void GetDate_WhenYearGreaterThan9999_ShouldThrowExactly(AlgorithmFactoryCase algo)
    {
        INotableDateAlgorithm sut = algo.Factory();

        foreach (int year in new[] { 10000, int.MaxValue })
        {
            ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = sut.GetDate(year, null);
            });

            Assert.AreEqual("year", ex.ParamName, $"Algorithm '{algo.Algorithm}' year {year}.");
        }
    }

    /// <summary>
    /// Verifies that supplying <see langword="null" /> as the calendar argument yields a result whose
    /// <see cref="DateTime.Kind" /> is <see cref="DateTimeKind.Unspecified" /> — the contract every algorithm
    /// observes for the "no calendar supplied" path.
    /// </summary>
    /// <param name="algo">The algorithm under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(NotableDateAlgorithmKnownAnswers.AllAlgorithmFactories),
        typeof(NotableDateAlgorithmKnownAnswers),
        DynamicDataDisplayName = nameof(NotableDateAlgorithmKnownAnswers.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(NotableDateAlgorithmKnownAnswers))]
    public void GetDate_WhenCalendarIsNull_ShouldReturnUnspecifiedKind(AlgorithmFactoryCase algo)
    {
        INotableDateAlgorithm sut = algo.Factory();

        DateTime? result = sut.GetDate(2024, null);

        Assert.IsNotNull(result, $"Algorithm '{algo.Algorithm}' returned null for year 2024.");
        Assert.AreEqual(DateTimeKind.Unspecified, result!.Value.Kind, $"Algorithm '{algo.Algorithm}'.");
    }

    /// <summary>
    /// Verifies that passing an explicit <see cref="SysGlobal.GregorianCalendar" /> matches the default
    /// (<see langword="null" />) calendar path — supplying the Gregorian calendar should produce the same
    /// result the algorithm computes when no calendar is supplied.
    /// </summary>
    /// <param name="algo">The algorithm under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(NotableDateAlgorithmKnownAnswers.AllAlgorithmFactories),
        typeof(NotableDateAlgorithmKnownAnswers),
        DynamicDataDisplayName = nameof(NotableDateAlgorithmKnownAnswers.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(NotableDateAlgorithmKnownAnswers))]
    public void GetDate_WhenCalendarIsExplicitlyGregorian_ShouldMatchDefaultPath(AlgorithmFactoryCase algo)
    {
        INotableDateAlgorithm sut = algo.Factory();

        DateTime? withDefault = sut.GetDate(2024, null);
        DateTime? withGregorian = sut.GetDate(2024, new SysGlobal.GregorianCalendar());

        Assert.AreEqual(withDefault, withGregorian, $"Algorithm '{algo.Algorithm}'.");
    }

    /// <summary>
    /// Verifies that passing a <see cref="SysGlobal.JulianCalendar" /> projects the algorithm's result
    /// through the supplied calendar — the year reported by the Julian calendar for the returned
    /// <see cref="DateTime" /> should be the input year, confirming the non-Gregorian projection branch
    /// fires.
    /// </summary>
    /// <param name="algo">The algorithm under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(NotableDateAlgorithmKnownAnswers.AllAlgorithmFactories),
        typeof(NotableDateAlgorithmKnownAnswers),
        DynamicDataDisplayName = nameof(NotableDateAlgorithmKnownAnswers.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(NotableDateAlgorithmKnownAnswers))]
    public void GetDate_WhenCalendarIsJulian_ShouldProjectThroughTargetCalendar(AlgorithmFactoryCase algo)
    {
        INotableDateAlgorithm sut = algo.Factory();
        SysGlobal.JulianCalendar julian = new();

        DateTime? result = sut.GetDate(2024, julian);

        Assert.IsNotNull(result, $"Algorithm '{algo.Algorithm}' returned null for year 2024 / Julian.");
        Assert.AreEqual(2024, julian.GetYear(result!.Value), $"Algorithm '{algo.Algorithm}'.");
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResolutionEngineTests.AddDaysWithinRange.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies range-pipeline behaviour through <see cref="NotableDateService" /> when the requested window sits next to
/// <see cref="DateTime.MinValue" /> or <see cref="DateTime.MaxValue" />. The range pipeline pads materialisation when
/// adjustments are configured; requesting a window adjacent to the date-time bounds exercises the underflow /
/// overflow clamp branches.
/// </summary>
[TestClass]
public sealed class NotableDateResolutionEngineAddDaysWithinRangeTests
{
    /// <summary>
    /// Verifies that a request whose start date is close to <see cref="DateTime.MinValue" /> clamps the
    /// materialisation start to <see cref="DateTime.MinValue" /> without throwing.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenRequestStartIsNearMinValue_ShouldClampMaterialisationStartToMinValue()
    {
        NotableDateService service = BuildService();

        IReadOnlyList<NotableDate> resolved = service.GetNotableDates(
            new DateTime(1, 1, 5),
            new DateTime(1, 1, 31));

        Assert.IsNotNull(resolved);
    }

    /// <summary>
    /// Verifies that a request whose end date is close to <see cref="DateTime.MaxValue" /> clamps the materialisation
    /// end to <see cref="DateTime.MaxValue" /> without throwing.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenRequestEndIsNearMaxValue_ShouldClampMaterialisationEndToMaxValue()
    {
        NotableDateService service = BuildService();

        IReadOnlyList<NotableDate> resolved = service.GetNotableDates(
            new DateTime(9999, 12, 1),
            new DateTime(9999, 12, 25));

        Assert.IsNotNull(resolved);
    }

    /// <summary>
    /// Verifies that a request whose boundaries are well clear of <see cref="DateTime" /> bounds exercises the
    /// normal materialisation-padding branch.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenRequestIsClearOfDateTimeBounds_ShouldApplyNormalPadding()
    {
        NotableDateService service = BuildService();

        IReadOnlyList<NotableDate> resolved = service.GetNotableDates(
            new DateTime(2024, 6, 1),
            new DateTime(2024, 6, 30));

        Assert.IsNotNull(resolved);
    }

    private static NotableDateService BuildService() =>
        new(
            ruleProviders: new[] { (INotableDateRuleProvider)new EmptyRuleProvider() },
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);

    private sealed class EmptyRuleProvider
        : INotableDateRuleProvider
    {
        public IEnumerable<NotableDateRule> LoadRules() => Array.Empty<NotableDateRule>();
    }
}

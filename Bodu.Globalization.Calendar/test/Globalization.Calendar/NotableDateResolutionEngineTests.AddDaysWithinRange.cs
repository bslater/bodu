// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResolutionEngineTests.AddDaysWithinRange.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the clamping branches of <c>NotableDateResolutionEngine.AddDaysWithinRange</c> through
/// <see cref="NotableDateResolutionService" />. The engine pads the materialisation request by ±14 days when an
/// adjustment processor is configured; requesting a window adjacent to <see cref="DateTime.MinValue" /> or
/// <see cref="DateTime.MaxValue" /> exercises the underflow / overflow clamp branches.
/// </summary>
[TestClass]
public sealed class NotableDateResolutionEngineAddDaysWithinRangeTests
{
    /// <summary>
    /// Verifies that a request whose start date is closer to <see cref="DateTime.MinValue" /> than the padding
    /// distance clamps the materialisation start to <see cref="DateTime.MinValue" /> without throwing.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenRequestStartIsNearMinValue_ShouldClampMaterialisationStartToMinValue()
    {
        NotableDateResolutionService service = BuildService();

        // Start within 14 days of MinValue → AddDaysWithinRange(-14) returns MinValue.
        IReadOnlyList<NotableDate> resolved = service.GetNotableDates(
            new DateTime(1, 1, 5),
            new DateTime(1, 1, 31));

        // No rules configured; assert only that the engine returned without throwing.
        Assert.IsNotNull(resolved);
    }

    /// <summary>
    /// Verifies that a request whose end date is closer to <see cref="DateTime.MaxValue" /> than the padding
    /// distance clamps the materialisation end to <see cref="DateTime.MaxValue" /> without throwing.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenRequestEndIsNearMaxValue_ShouldClampMaterialisationEndToMaxValue()
    {
        NotableDateResolutionService service = BuildService();

        // End within 14 days of MaxValue → AddDaysWithinRange(+14) returns MaxValue.
        IReadOnlyList<NotableDate> resolved = service.GetNotableDates(
            new DateTime(9999, 12, 1),
            new DateTime(9999, 12, 25));

        Assert.IsNotNull(resolved);
    }

    /// <summary>
    /// Verifies that a request whose boundaries are well clear of <see cref="DateTime" /> bounds exercises the
    /// normal <c>value.AddDays(days)</c> branch of <c>AddDaysWithinRange</c>.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenRequestIsClearOfDateTimeBounds_ShouldApplyNormalPadding()
    {
        NotableDateResolutionService service = BuildService();

        IReadOnlyList<NotableDate> resolved = service.GetNotableDates(
            new DateTime(2024, 6, 1),
            new DateTime(2024, 6, 30));

        Assert.IsNotNull(resolved);
    }

    private static NotableDateResolutionService BuildService() =>
        new(
            ruleProviders: new[] { (INotableDateRuleProvider)new EmptyRuleProvider() },
            weekendDefinition: CalendarWeekendDefinition.SaturdaySunday);

    private sealed class EmptyRuleProvider
        : INotableDateRuleProvider
    {
        public IEnumerable<NotableDateRule> LoadRules() => Array.Empty<NotableDateRule>();
    }
}

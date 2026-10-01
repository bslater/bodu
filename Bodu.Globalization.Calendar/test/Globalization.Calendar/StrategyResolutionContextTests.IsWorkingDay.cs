// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StrategyResolutionContextTests.IsWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies <see cref="StrategyResolutionContext.IsWorkingDay" /> when the non-working rules it consults themselves
/// count working days.
/// </summary>
public partial class StrategyResolutionContextTests
{
    private const string Territory = "XX";

    /// <summary>
    /// Builds a resource holding one non-working rule whose occurrence is calculated by the supplied strategy.
    /// </summary>
    /// <param name="strategy">The strategy that calculates the non-working rule's occurrence.</param>
    /// <returns>The resource.</returns>
    private static NotableDateResource NonWorkingResource(IDateCalculationStrategy strategy)
    {
        RuleApplicability everywhere = new(CalendarSystem.Gregorian, null, null, [], [], []);
        NotableDateRule rule = new("r", 0, null, nonWorking: true, null, everywhere, strategy, [], []);
        NotableDateDefinition definition = new("business-day-holiday", "Business-Day Holiday", NotableDateCategory.PublicHoliday, defaultNonWorkingDay: true, defaultDurationDays: 1, [], [rule]);

        return new NotableDateResource("test.working-days", "1.0", ResolutionPolicy.Default, [], [definition]);
    }

    /// <summary>
    /// Verifies that testing a date builds the non-working days of only its own year and the year before it when a
    /// non-working rule counts working days, rather than building every earlier year in turn back to year 1.
    /// </summary>
    [TestMethod]
    public void IsWorkingDay_WhenNonWorkingRuleCountsWorkingDays_ShouldCalculateOnlyDateYearAndPreviousYear()
    {
        YearRecordingStrategy strategy = new(new WorkingDayInMonthStrategy(3, 1));
        var context = new StrategyResolutionContext(NonWorkingResource(strategy), null, Territory);

        _ = context.IsWorkingDay(new DateOnly(100, 6, 1), Territory);

        CollectionAssert.AreEquivalent(new[] { 99, 100 }, strategy.Years.Distinct().ToArray());
    }

    /// <summary>
    /// Verifies that testing a date in the last representable year, when a non-working rule counts working days,
    /// completes on a thread with a 256 KiB stack and reports the day that rule claims as non-working, rather than
    /// overflowing the stack.
    /// </summary>
    [TestMethod]
    public void IsWorkingDay_WhenNonWorkingRuleCountsWorkingDaysInLastYearOnConstrainedStack_ShouldNotOverflow()
    {
        var context = new StrategyResolutionContext(NonWorkingResource(new WorkingDayInMonthStrategy(3, 1)), null, Territory);

        bool? isWorkingDay = null;
        Exception? captured = null;
        var worker = new Thread(
            () =>
            {
                try
                {
                    // Monday 1 March 9999 is the first working day of the month, which the non-working rule claims.
                    isWorkingDay = context.IsWorkingDay(new DateOnly(9999, 3, 1), Territory);
                }
                catch (Exception ex)
                {
                    captured = ex;
                }
            },
            maxStackSize: 256 << 10);

        worker.Start();
        worker.Join();

        Assert.IsNull(captured);
        Assert.IsFalse(isWorkingDay);
    }

    /// <summary>
    /// A calculation strategy that records each year it is asked to calculate before delegating to an inner strategy.
    /// </summary>
    private sealed class YearRecordingStrategy
        : IDateCalculationStrategy
    {
        /// <summary>The strategy the calculation is delegated to.</summary>
        private readonly IDateCalculationStrategy _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="YearRecordingStrategy" /> class.
        /// </summary>
        /// <param name="inner">The strategy the calculation is delegated to.</param>
        public YearRecordingStrategy(IDateCalculationStrategy inner) =>
            _inner = inner;

        /// <summary>
        /// Gets the years the strategy has been asked to calculate, in request order.
        /// </summary>
        public List<int> Years { get; } = new();

        /// <inheritdoc />
        public DateOnly? Calculate(int year, StrategyResolutionContext context)
        {
            Years.Add(year);

            return _inner.Calculate(year, context);
        }
    }
}

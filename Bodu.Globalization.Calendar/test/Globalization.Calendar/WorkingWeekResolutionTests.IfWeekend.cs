// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WorkingWeekResolutionTests.IfWeekend.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class WorkingWeekResolutionTests
{
    /// <summary>
    /// Verifies that under a Sunday-to-Thursday working week the observed-only emission suppresses the Friday actual
    /// date, since the configured weekend treats Friday 2 January 2026 as a weekend day.
    /// </summary>
    [TestMethod]
    public void IfWeekend_WhenWorkingWeekIsSundayToThursday_ForFridayHoliday_ShouldSuppressActualDate()
    {
        INotableDateService service = Build("workingDays=\"1111100\"");

        Assert.IsEmpty(service.Resolve(new DateOnly(2026, 1, 2), Territory));
    }

    /// <summary>
    /// Verifies that under a Sunday-to-Thursday working week a Friday holiday is observed on the following Sunday,
    /// skipping the configured Friday and Saturday weekend. The substitute lands on Sunday 4 January 2026 carrying the
    /// 2 January actual date.
    /// </summary>
    [TestMethod]
    public void IfWeekend_WhenWorkingWeekIsSundayToThursday_ForFridayHoliday_ShouldObserveOnFollowingSunday()
    {
        INotableDateService service = Build("workingDays=\"1111100\"");

        IReadOnlyList<NotableDate> observed = service.Resolve(new DateOnly(2026, 1, 4), Territory);

        Assert.HasCount(1, observed);
        Assert.AreEqual(
            (true, (DateOnly?)new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 4)),
            (observed[0].IsObserved, observed[0].ActualDate, observed[0].Date));
    }

    /// <summary>
    /// Verifies that under the default Monday-to-Friday working week a Friday holiday is a working-day occurrence, so
    /// the weekend trigger does not fire and the holiday is emitted on its actual Friday 2 January 2026 date.
    /// </summary>
    [TestMethod]
    public void IfWeekend_WhenDefaultWorkingWeek_ForFridayHoliday_ShouldEmitActualDate()
    {
        INotableDateService service = Build(string.Empty);

        IReadOnlyList<NotableDate> actual = service.Resolve(new DateOnly(2026, 1, 2), Territory);

        Assert.HasCount(1, actual);
        Assert.AreEqual(
            (false, new DateOnly(2026, 1, 2)),
            (actual[0].IsObserved, actual[0].Date));
    }

    /// <summary>
    /// Verifies that under the default Monday-to-Friday working week the Friday holiday emits no observed occurrence on
    /// the following Sunday, since the weekend trigger never fires.
    /// </summary>
    [TestMethod]
    public void IfWeekend_WhenDefaultWorkingWeek_ForFridayHoliday_ShouldNotEmitObservedOccurrence()
    {
        INotableDateService service = Build(string.Empty);

        Assert.IsEmpty(service.Resolve(new DateOnly(2026, 1, 4), Territory));
    }
}

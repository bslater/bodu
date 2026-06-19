// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WorkingDayExtensionMatrixTests.IsNonWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class WorkingDayExtensionMatrixTests
{
    /// <summary>
    /// Verifies that the non-working classification across every day of the first business week of 2026 is the inverse
    /// of the working classification.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="month">The month component.</param>
    /// <param name="day">The day component.</param>
    /// <param name="expectedWorking">The expected working-day classification whose inverse is asserted.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(WorkingDayClassificationRows))]
    public void IsNonWorkingDay_WhenScannedAcrossWeek_ShouldReturnInverseOfWorkingClassification(int year, int month, int day, bool expectedWorking)
    {
        Assert.AreEqual(!expectedWorking, new DateOnly(year, month, day).IsNonWorkingDay(Service, "XX"));
    }
}

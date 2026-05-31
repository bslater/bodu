// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SmokeTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Algorithms;

namespace Bodu.Smoke;

/// <summary>
/// Provides smoke-tier coverage for the primary public types in <c>Bodu.Globalization.Calendar</c>. Each test
/// exercises one happy-path entry point so that catastrophic breakage (rule-resolver dispatch fault, calendar
/// computus regression, service construction error) is caught by the smallest possible build run.
/// </summary>
[TestClass]
public sealed class SmokeTests
{
    /// <summary>
    /// Verifies that <see cref="EasterSundayNotableDateAlgorithm.GetDate(int, System.Globalization.Calendar?)" />
    /// resolves a known-correct Gregorian Easter Sunday date.
    /// </summary>
    [TestMethod]
    public void EasterSundayAlgorithm_GetDate_ForKnownYear_ShouldReturnExpectedDate()
    {
        EasterSundayNotableDateAlgorithm algorithm = new();

        DateTime? actual = algorithm.GetDate(2024, null);

        Assert.IsNotNull(actual);
        Assert.AreEqual(new DateTime(2024, 3, 31), actual);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService" /> can be constructed and that
    /// <see cref="NotableDateService.IsWeekend" /> returns true for a Sunday under the default rule set.
    /// </summary>
    [TestMethod]
    public void NotableDateService_IsWeekend_ForSunday_ShouldReturnTrue()
    {
        NotableDateService service = new();

        var actual = service.IsWeekend(new DateTime(2024, 6, 9));

        Assert.IsTrue(actual);
    }
}

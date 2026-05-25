// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateAlgorithmAssertions.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Provides the shared assertion routine that every <see cref="NotableDateAlgorithmKnownAnswer" />-driven
/// test consumes. Centralising the routine keeps each <c>*NotableDateAlgorithmTests.cs</c> file free of
/// duplicated tolerance-check boilerplate.
/// </summary>
public static class NotableDateAlgorithmAssertions
{
    /// <summary>
    /// Invokes the algorithm identified by <paramref name="knownAnswer" /> and asserts that the result
    /// matches the expected date — exactly when <see cref="NotableDateAlgorithmKnownAnswer.ToleranceDays" />
    /// is zero, or within the symmetric +/- day window otherwise. Materialises the
    /// <see cref="SysGlobal.Calendar" /> argument from
    /// <see cref="NotableDateAlgorithmKnownAnswer.CalendarKind" />.
    /// </summary>
    /// <param name="knownAnswer">The row to verify.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="knownAnswer" /> is
    /// <see langword="null" />.</exception>
    public static void AssertResultWithinTolerance(NotableDateAlgorithmKnownAnswer knownAnswer)
    {
        ArgumentNullException.ThrowIfNull(knownAnswer);

        INotableDateAlgorithm sut = knownAnswer.Factory();
        SysGlobal.Calendar? calendar = knownAnswer.CalendarKind switch
        {
            AlgorithmCalendarKind.Gregorian => new SysGlobal.GregorianCalendar(),
            AlgorithmCalendarKind.Julian => new SysGlobal.JulianCalendar(),
            _ => null,
        };

        DateTime? result = sut.GetDate(knownAnswer.Year, calendar);

        Assert.IsNotNull(result, $"{knownAnswer.Algorithm} {knownAnswer.Year}: algorithm returned null.");

        if (knownAnswer.ToleranceDays == 0)
        {
            Assert.AreEqual(knownAnswer.ExpectedDate, result!.Value);
            return;
        }

        int dayDiff = Math.Abs((result!.Value - knownAnswer.ExpectedDate).Days);
        Assert.IsTrue(
            dayDiff <= knownAnswer.ToleranceDays,
            $"{knownAnswer.Algorithm} {knownAnswer.Year}: expected within +/- {knownAnswer.ToleranceDays} day(s) of {knownAnswer.ExpectedDate:yyyy-MM-dd}, got {result.Value:yyyy-MM-dd} (diff {dayDiff} day(s)).");
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompanyFoundingDayAlgorithm.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Algorithms;

namespace Bodu.Globalization.Calendar.Samples.CustomAlgorithm;

/// <summary>
/// A consumer-written <see cref="INotableDateAlgorithm" />: the anniversary of the company's founding
/// (12 March 1998), celebrated on the Friday of that week so it always lands adjacent to a weekend.
/// An algorithm answers one question — "what date in this year?" — and returns <see langword="null" />
/// for years it does not apply to; everything else (categories, adjustments, territories, emission)
/// stays declarative in the rule that references it by key.
/// </summary>
public sealed class CompanyFoundingDayAlgorithm : INotableDateAlgorithm
{
    /// <summary>The year the company was founded; earlier years have no occurrence.</summary>
    private const int FoundingYear = 1998;

    /// <summary>
    /// Calculates the celebration date for <paramref name="year" />: the Friday of the week
    /// containing 12 March.
    /// </summary>
    /// <param name="year">The Gregorian year to calculate.</param>
    /// <returns>The celebration date, or <see langword="null" /> before the founding year.</returns>
    public DateOnly? Calculate(int year)
    {
        if (year < FoundingYear)
            return null;

        var anniversary = new DateOnly(year, 3, 12);

        // Roll forward or back to the Friday of the anniversary's week (Mon-Sun week).
        var daysToFriday = DayOfWeek.Friday - anniversary.DayOfWeek;
        if (anniversary.DayOfWeek == DayOfWeek.Sunday)
            daysToFriday = -2;   // Sunday belongs to the week just ended

        return anniversary.AddDays(daysToFriday);
    }
}

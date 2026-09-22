// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FoundingDayAlgorithm.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Algorithms;

namespace Bodu.Globalization.Calendar.Samples.Plugin.Contoso;

/// <summary>
/// The algorithm the plugin contributes: the anniversary of the company's founding (12 March 1998),
/// observed on the Friday of that week so it always sits adjacent to a weekend.
/// </summary>
/// <remarks>
/// An algorithm answers exactly one question — "what date in this year?" — and returns
/// <see langword="null" /> for years it does not apply to. Everything else (category, territory,
/// weekend adjustment, whether it is a working day) stays declarative in the rule that references it.
/// </remarks>
public sealed class FoundingDayAlgorithm
    : INotableDateAlgorithm
{
    /// <summary>The year the company was founded; earlier years have no occurrence.</summary>
    private const int FoundingYear = 1998;

    /// <summary>
    /// Calculates the observance date for <paramref name="year" />.
    /// </summary>
    /// <param name="year">The Gregorian year to calculate.</param>
    /// <returns>The observance date, or <see langword="null" /> before the founding year.</returns>
    public DateOnly? Calculate(int year)
    {
        if (year < FoundingYear)
            return null;

        var anniversary = new DateOnly(year, 3, 12);

        // Roll to the Friday of the anniversary's week (Monday-Sunday week).
        var daysToFriday = DayOfWeek.Friday - anniversary.DayOfWeek;
        if (anniversary.DayOfWeek == DayOfWeek.Sunday)
            daysToFriday = -2;   // Sunday closes the week that has just ended

        return anniversary.AddDays(daysToFriday);
    }
}

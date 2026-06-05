// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MatarikiCalendar.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Provides the official gazetted dates of the New Zealand Matariki public holiday.
/// </summary>
/// <remarks>
/// <para>
/// Matariki is determined annually by the Matariki Advisory Group from the Maramataka (the Māori lunar calendar) rather
/// than by a closed-form rule, so the dates are published as a fixed schedule by the Te Kāhui o Matariki Public Holiday
/// Act 2022. Years outside the gazetted schedule return no date.
/// </para>
/// </remarks>
internal static class MatarikiCalendar
{
    /// <summary>
    /// The gazetted Matariki observance dates, keyed by Gregorian year, from the schedule to the Te Kāhui o Matariki
    /// Public Holiday Act 2022.
    /// </summary>
    private static readonly Dictionary<int, DateOnly> s_dates = new()
    {
        [2022] = new(2022, 6, 24),
        [2023] = new(2023, 7, 14),
        [2024] = new(2024, 6, 28),
        [2025] = new(2025, 6, 20),
        [2026] = new(2026, 7, 10),
        [2027] = new(2027, 6, 25),
        [2028] = new(2028, 7, 14),
        [2029] = new(2029, 7, 6),
        [2030] = new(2030, 6, 21),
        [2031] = new(2031, 7, 11),
        [2032] = new(2032, 7, 2),
        [2033] = new(2033, 6, 24),
        [2034] = new(2034, 7, 7),
        [2035] = new(2035, 6, 29),
        [2036] = new(2036, 7, 18),
        [2037] = new(2037, 7, 10),
        [2038] = new(2038, 6, 25),
        [2039] = new(2039, 7, 15),
        [2040] = new(2040, 7, 6),
        [2041] = new(2041, 7, 19),
        [2042] = new(2042, 7, 11),
        [2043] = new(2043, 7, 3),
        [2044] = new(2044, 6, 24),
        [2045] = new(2045, 7, 7),
        [2046] = new(2046, 6, 29),
        [2047] = new(2047, 7, 19),
        [2048] = new(2048, 7, 3),
        [2049] = new(2049, 6, 25),
        [2050] = new(2050, 7, 15),
        [2051] = new(2051, 6, 30),
        [2052] = new(2052, 6, 21),
    };

    /// <summary>
    /// Returns the gazetted Matariki date for the supplied year.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <returns>The Matariki date, or <see langword="null" /> when the year is outside the gazetted schedule.</returns>
    public static DateOnly? Resolve(int year) =>
        s_dates.TryGetValue(year, out DateOnly date) ? date : null;
}

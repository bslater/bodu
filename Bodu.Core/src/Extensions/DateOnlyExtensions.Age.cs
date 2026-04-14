// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateOnlyExtensions.Age.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Calculates the age in full calendar years as at today’s date ( <see cref="DateTime.Today"/>).
    /// </summary>
    /// <param name="date">The earlier date to calculate from, typically representing a birth date or historical reference.</param>
    /// <returns>
    /// The number of full calendar years that have elapsed between <paramref name="date"/> and today. Returns 0 if
    /// <paramref name="date"/> occurs after today.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If <paramref name="date"/> is February 29 in a leap year and today is not a leap year, the age is calculated as if the birthday
    /// were on February 28.
    /// </para>
    /// <para>The result is clamped to 0 to avoid returning negative values for future dates.</para>
    /// </remarks>
    public static int Age(this DateOnly date) => date.Age(DateTime.Today.ToDateOnly());

    /// <summary>
    /// Calculates the age in full calendar years as at a specified reference date.
    /// </summary>
    /// <param name="date">The earlier date to calculate from, typically representing a birth date or historical reference.</param>
    /// <param name="asAtDate">The later date to calculate to, representing the point in time at which the age is evaluated.</param>
    /// <returns>
    /// The number of full calendar years that have elapsed between <paramref name="date"/> and <paramref name="asAtDate"/>. Returns 0
    /// if <paramref name="asAtDate"/> occurs before <paramref name="date"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If <paramref name="date"/> is February 29 in a leap year and <paramref name="asAtDate"/> occurs in a non-leap year, the age is
    /// calculated as if the birthday were on February 28.
    /// </para>
    /// <para>The result is clamped to 0 to avoid returning negative values for future dates.</para>
    /// </remarks>
    public static int Age(this DateOnly date, DateOnly asAtDate)
    {
        date.GetDateParts(out int birthYear, out int birthMonth, out int birthDay);
        asAtDate.GetDateParts(out int asAtYear, out int asAtMonth, out int asAtDay);

        if (birthMonth == 2 && birthDay == 29 && !DateTime.IsLeapYear(asAtYear))
            birthDay = 28;

        int age = asAtYear - birthYear;

        if (asAtMonth < birthMonth || (asAtMonth == birthMonth && asAtDay < birthDay))
            age--;

        return age < 0 ? 0 : age;
    }
}
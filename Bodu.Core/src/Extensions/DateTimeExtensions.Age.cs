// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.Age.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns the age in full calendar years between the specified <paramref name="dateTime"/> and <see cref="DateTime.Today"/>.
    /// </summary>
    /// <param name="dateTime">The earlier date to calculate from, typically representing a birth date or other reference point.</param>
    /// <returns>The number of full calendar years that have elapsed between <paramref name="dateTime"/> and <see cref="DateTime.Today"/>. Returns <c>0</c> if <paramref name="dateTime"/> occurs after today.</returns>
    /// <remarks>
    /// <para>This overload determines the number of full years that have passed by comparing the year, month, and day components. If the month and day of <paramref name="dateTime"/> have not yet occurred in the current year, the result is decremented by one.</para>
    /// <para>If <paramref name="dateTime"/> is February 29 in a leap year and today is not a leap year, the comparison is performed as if the date were February 28.</para>
    /// <para>The result is clamped to <c>0</c> to avoid returning negative values when <paramref name="dateTime"/> is in the future. The <see cref="DateTime.Kind"/> property is ignored when calculating the result.</para>
    /// </remarks>
    public static int Age(this DateTime dateTime) => dateTime.Age(DateTime.Today);

    /// <summary>
    /// Returns the age in full calendar years between the specified <paramref name="dateTime"/> and a supplied reference date.
    /// </summary>
    /// <param name="dateTime">The earlier date to calculate from, typically representing a birth date or other reference point.</param>
    /// <param name="atDate">The later date to calculate to, representing the point in time at which the age is evaluated.</param>
    /// <returns>The number of full calendar years that have elapsed between <paramref name="dateTime"/> and <paramref name="atDate"/>. Returns <c>0</c> if <paramref name="atDate"/> occurs before <paramref name="dateTime"/>.</returns>
    /// <remarks>
    /// <para>This overload determines the number of full years that have passed by comparing the year, month, and day components. If the month and day of <paramref name="dateTime"/> have not yet occurred in the year of <paramref name="atDate"/>, the result is decremented by one.</para>
    /// <para>If <paramref name="dateTime"/> is February 29 in a leap year and <paramref name="atDate"/> is in a non-leap year, the comparison is performed as if the date were February 28.</para>
    /// <para>The result is clamped to <c>0</c> to avoid returning negative values when <paramref name="dateTime"/> is after <paramref name="atDate"/>. The <see cref="DateTime.Kind"/> property is ignored when calculating the result.</para>
    /// </remarks>
    public static int Age(this DateTime dateTime, DateTime atDate)
    {
        dateTime.GetDateParts(out int birthYear, out int birthMonth, out int birthDay);
        atDate.GetDateParts(out int atYear, out int atMonth, out int atDay);

        if (birthMonth == 2 && birthDay == 29 && !DateTime.IsLeapYear(atYear))
            birthDay = 28;

        int age = atYear - birthYear;

        if (atMonth < birthMonth || (atMonth == birthMonth && atDay < birthDay))
            age--;

        return age < 0 ? 0 : age;
    }
}

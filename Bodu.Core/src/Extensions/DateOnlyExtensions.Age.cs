// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.Age.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns the age in full calendar years between the specified <paramref name="date" /> and today's date.
    /// </summary>
    /// <param name="date">
    /// The earlier date to calculate from, typically representing a birth date or other reference point.
    /// </param>
    /// <returns>
    /// The number of full calendar years that have elapsed between <paramref name="date" /> and today. Returns <c>0</c>
    /// if <paramref name="date" /> occurs after today.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload determines the number of full years that have passed by comparing the year, month, and day
    /// components. If the month and day of <paramref name="date" /> have not yet occurred in the current year, the
    /// result is decremented by one.
    /// </para>
    /// <para>
    /// If <paramref name="date" /> is February 29 in a leap year and today is not a leap year, the comparison is
    /// performed as if the date were February 28.
    /// </para>
    /// <para>
    /// The result is clamped to <c>0</c> to avoid returning negative values when <paramref name="date" /> is in the
    /// future.
    /// </para>
    /// </remarks>
    public static int Age(this DateOnly date) => date.Age(DateTime.Today.ToDateOnly());

    /// <summary>
    /// Returns the age in full calendar years between the specified <paramref name="date" /> and a supplied reference
    /// date.
    /// </summary>
    /// <param name="date">
    /// The earlier date to calculate from, typically representing a birth date or other reference point.
    /// </param>
    /// <param name="asAtDate">
    /// The later date to calculate to, representing the point in time at which the age is evaluated.
    /// </param>
    /// <returns>
    /// The number of full calendar years that have elapsed between <paramref name="date" /> and
    /// <paramref name="asAtDate" />. Returns <c>0</c> if <paramref name="asAtDate" /> occurs before
    /// <paramref name="date" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload determines the number of full years that have passed by comparing the year, month, and day
    /// components. If the month and day of <paramref name="date" /> have not yet occurred in the year of
    /// <paramref name="asAtDate" />, the result is decremented by one.
    /// </para>
    /// <para>
    /// If <paramref name="date" /> is February 29 in a leap year and <paramref name="asAtDate" /> is in a non-leap
    /// year, the comparison is performed as if the date were February 28.
    /// </para>
    /// <para>
    /// The result is clamped to <c>0</c> to avoid returning negative values when <paramref name="date" /> is after
    /// <paramref name="asAtDate" />.
    /// </para>
    /// </remarks>
    public static int Age(this DateOnly date, DateOnly asAtDate)
    {
        date.GetDateParts(out var birthYear, out var birthMonth, out var birthDay);
        asAtDate.GetDateParts(out var asAtYear, out var asAtMonth, out var asAtDay);

        if (birthMonth == 2 && birthDay == 29 && !DateTime.IsLeapYear(asAtYear))
            birthDay = 28;

        var age = asAtYear - birthYear;

        if (asAtMonth < birthMonth || (asAtMonth == birthMonth && asAtDay < birthDay))
            age--;

        return age < 0 ? 0 : age;
    }
}

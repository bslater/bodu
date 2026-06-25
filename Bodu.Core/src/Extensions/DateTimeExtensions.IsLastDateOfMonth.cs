// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.IsLastDateOfMonth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
#if BODU_EXTENSION_MEMBERS

    extension(DateTime dateTime)
    {
        /// <summary>
        /// Gets a value indicating whether this date falls on the last day of its calendar month.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if this date represents the last day of its month; otherwise,
        /// <see langword="false" />.
        /// </value>
        /// <remarks>
        /// <para>
        /// This compares the <see cref="DateTime.Day" /> component to the total number of days in the same month and
        /// year, accounting for leap-year adjustments to February.
        /// </para>
        /// <para>
        /// Equivalent to checking whether <c>dateTime.Day == DateTime.DaysInMonth(dateTime.Year, dateTime.Month)</c>.
        /// </para>
        /// </remarks>
        public bool IsLastDateOfMonth =>
            dateTime.Day == DateTime.DaysInMonth(dateTime.Year, dateTime.Month);
    }

#else

    /// <summary>
    /// Determines whether the specified <see cref="DateTime" /> falls on the last day of its calendar month.
    /// </summary>
    /// <param name="dateTime">The date and time value to evaluate.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="dateTime" /> represents the last day of its month; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method compares the <see cref="DateTime.Day" /> component to the total number of days in the same month and
    /// year, accounting for leap-year adjustments to February.
    /// </para>
    /// <para>
    /// Equivalent to checking whether <c>dateTime.Day == DateTime.DaysInMonth(dateTime.Year, dateTime.Month)</c>.
    /// </para>
    /// </remarks>
    public static bool IsLastDateOfMonth(this DateTime dateTime) => dateTime.Day == DateTime.DaysInMonth(dateTime.Year, dateTime.Month);

#endif
}

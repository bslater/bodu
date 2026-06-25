// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.WeekOrdinalOfMonth.cs" company="Bodu Pty. Ltd.">
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
        /// Gets the <see cref="WeekOrdinal" /> represented by this date, indicating the ordinal occurrence of its
        /// <see cref="DateTime.DayOfWeek" /> within the month.
        /// </summary>
        /// <value>
        /// A <see cref="WeekOrdinal" /> value indicating which occurrence of the weekday this date represents within
        /// its calendar month.
        /// </value>
        /// <remarks>
        /// <para>
        /// The result is calculated by counting how many full seven-day intervals have passed since the start of the
        /// month, based on <see cref="DateTime.Day" />. For example, the 1st through 7th of the month yield
        /// <see cref="WeekOrdinal.First" />, while the 8th through 14th yield <see cref="WeekOrdinal.Second" />, and so
        /// on.
        /// </para>
        /// <para>
        /// <see cref="WeekOrdinal.Fifth" /> is only returned when a month contains five occurrences of the given
        /// <see cref="DateTime.DayOfWeek" />.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if the calculated ordinal does not correspond to a defined value of the <see cref="WeekOrdinal" />
        /// enumeration.
        /// </exception>
        public WeekOrdinal WeekOrdinalOfMonth
        {
            get
            {
                int ordinal = ((dateTime.Day - 1) / 7) + 1;

                ThrowHelper.ThrowIfEnumValueIsUndefined<WeekOrdinal>((WeekOrdinal)ordinal);

                return (WeekOrdinal)ordinal;
            }
        }
    }

#else

    /// <summary>
    /// Returns the <see cref="WeekOrdinal" /> represented by the specified <see cref="DateTime" />, indicating the
    /// ordinal occurrence of its <see cref="DateTime.DayOfWeek" /> within the month.
    /// </summary>
    /// <param name="dateTime">The date and time value to evaluate.</param>
    /// <returns>
    /// A <see cref="WeekOrdinal" /> value indicating which occurrence of the weekday <paramref name="dateTime" />
    /// represents within its calendar month.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The result is calculated by counting how many full seven-day intervals have passed since the start of the month,
    /// based on <see cref="DateTime.Day" />. For example, the 1st through 7th of the month yield
    /// <see cref="WeekOrdinal.First" />, while the 8th through 14th yield <see cref="WeekOrdinal.Second" />, and so on.
    /// </para>
    /// <para>
    /// <see cref="WeekOrdinal.Fifth" /> is only returned when a month contains five occurrences of the given
    /// <see cref="DateTime.DayOfWeek" />.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the calculated ordinal does not correspond to a defined value of the <see cref="WeekOrdinal" />
    /// enumeration.
    /// </exception>
    public static WeekOrdinal WeekOrdinalOfMonth(this DateTime dateTime)
    {
        int ordinal = ((dateTime.Day - 1) / 7) + 1;

        ThrowHelper.ThrowIfEnumValueIsUndefined<WeekOrdinal>((WeekOrdinal)ordinal);

        return (WeekOrdinal)ordinal;
    }

#endif
}

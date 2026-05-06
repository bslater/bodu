// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.IsoYear.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

using System.Globalization;
using System.Runtime.CompilerServices;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns the ISO 8601 year associated with the specified <paramref name="date"/>.
    /// </summary>
    /// <param name="date">The date and time value to evaluate.</param>
    /// <returns>The ISO 8601 calendar year that contains the ISO week of <paramref name="date"/>.</returns>
    /// <remarks>
    /// <para>The ISO 8601 year may differ from the calendar year of <paramref name="date"/>. A date near the start or end of a calendar year may belong to the ISO year of the adjacent calendar year, depending on which ISO week it falls into. For example, January 1 may belong to the last week of the previous ISO year, and December 31 may belong to week 1 of the following ISO year.</para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IsoYear(this DateTime date)
    {
#if NETSTANDARD2_0

        // ISO 8601 rule: Week 1 is the week with Jan 4 in it. So we shift the date to Thursday (which is always in the same ISO week)
        DayOfWeek day = date.DayOfWeek;
        int delta = DayOfWeek.Thursday - day;
        DateTime adjusted = date.AddDays(delta);

        return adjusted.Year;
#else
        return ISOWeek.GetYear(date);
#endif
    }
}

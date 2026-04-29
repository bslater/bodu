// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.ToTimeSpan.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="TimeSpan"/> representing the time-of-day portion of the specified <paramref name="dateTime"/>.
    /// </summary>
    /// <param name="dateTime">The date and time value from which to extract the time-of-day component.</param>
    /// <returns>A <see cref="TimeSpan"/> value representing the hours, minutes, seconds, and fractional seconds that have elapsed since midnight on the same calendar day as <paramref name="dateTime"/>.</returns>
    /// <remarks>
    /// <para>The returned value is equivalent to <see cref="DateTime.TimeOfDay"/> and is unaffected by the <see cref="DateTime.Kind"/> property.</para>
    /// </remarks>
    public static TimeSpan ToTimeSpan(this DateTime dateTime) => dateTime.TimeOfDay;
}

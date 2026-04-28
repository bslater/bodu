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
    /// Returns a new <see cref="TimeSpan"/> representing the time-of-day portion of the specified <see cref="DateTime"/>.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> from which to extract the time component.</param>
    /// <returns>
    /// An object whose value is set to the hours, minutes, seconds, and fractional seconds that have elapsed since midnight on the same
    /// calendar day as <paramref name="dateTime"/>.
    /// </returns>
    /// <remarks>
    /// The returned value is equivalent to <see cref="DateTime.TimeOfDay"/> and is unaffected by the <see cref="DateTime.Kind"/> property.
    /// </remarks>
    public static TimeSpan ToTimeSpan(this DateTime dateTime) => dateTime.TimeOfDay;
}

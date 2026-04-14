// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateTimeExtensions.Midnight.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing midnight (00:00:00) at the start of the same calendar day as the specified <paramref name="dateTime"/>.
    /// </summary>
    /// <param name="dateTime">The input <see cref="DateTime"/> whose calendar date is preserved.</param>
    /// <returns>
    /// An object whose value is set to 00:00:00 (midnight) on the same date as <paramref name="dateTime"/>, preserving its <see cref="DateTime.Kind"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is functionally equivalent to accessing the <see cref="DateTime.Date"/> property. It normalizes the time component to
    /// midnight while retaining the date and <see cref="DateTime.Kind"/> of the input.
    /// </para>
    /// <para>
    /// Use this method to explicitly express intent to retrieve the start of the day in scenarios where clarity and precision are preferred.
    /// </para>
    /// </remarks>
    public static DateTime Midnight(this DateTime dateTime) => dateTime.Date;
}

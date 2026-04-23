// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.IsFirstDayOfMonth.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns an indication whether the specified <see cref="DateTime"/> falls on the first day of its month.
    /// </summary>
    /// <param name="dateTime">The date and time value to evaluate.</param>
    /// <returns><see langword="true"/> if <paramref name="dateTime"/> represents the first day of its month; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>This method evaluates whether the <see cref="DateTime.Day"/> component is equal to <c>1</c>.</para>
    /// </remarks>
    public static bool IsFirstDayOfMonth(this DateTime dateTime) => dateTime.Day == 1;
}

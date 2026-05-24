// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.IsFirstDateOfMonth.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Determines whether the specified <see cref="DateTime" /> falls on the first day of its calendar month.
    /// </summary>
    /// <param name="dateTime">The date and time value to evaluate.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="dateTime" /> represents the first day of its month; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method evaluates whether the <see cref="DateTime.Day" /> component is equal to <c>1</c>.
    /// </para>
    /// </remarks>
    public static bool IsFirstDateOfMonth(this DateTime dateTime) => dateTime.Day == 1;
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.IsFirstDateOfMonth.cs" company="Bodu Pty. Ltd.">
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
        /// Gets a value indicating whether this date falls on the first day of its calendar month.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if this date represents the first day of its month; otherwise,
        /// <see langword="false" />.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This evaluates whether the <see cref="DateTime.Day" /> component is equal to <c>1</c>.
        /// </para>
        /// </remarks>
        public bool IsFirstDateOfMonth =>
            dateTime.Day == 1;
    }

#else

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

#endif
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.GetElapsedTimeSince.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a <see cref="TimeSpan"/> representing the time elapsed between the specified <see cref="DateTime"/> and the current UTC time.
    /// </summary>
    /// <param name="dateTime">The date and time value to compare against <see cref="DateTime.UtcNow"/>.</param>
    /// <returns>
    /// The time interval between <paramref name="dateTime"/> and the current UTC time; that is, <paramref name="dateTime"/> minus <see cref="DateTime.UtcNow"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If <paramref name="dateTime"/> is in local time ( <see cref="DateTimeKind.Local"/>), it is automatically converted to UTC using
    /// <see cref="DateTime.ToUniversalTime"/> before comparison.
    /// </para>
    /// <para>
    /// If the <paramref name="dateTime"/> has a <see cref="DateTimeKind"/> of <see cref="DateTimeKind.Unspecified"/>, an exception is
    /// thrown to avoid ambiguity regarding the time zone context.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown if <paramref name="dateTime"/> has a <see cref="DateTimeKind"/> of <see cref="DateTimeKind.Unspecified"/>.</exception>
    public static TimeSpan GetElapsedTimeSince(this DateTime dateTime)
    {
        DateTime utcInput = dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            _ => throw new ArgumentException(
                string.Format(ResourceStrings.Arg_Invalid_ValueForOperation, nameof(DateTime.Kind), $"{nameof(DateTimeKind.Utc)} or {nameof(DateTimeKind.Local)}", dateTime.Kind),
                nameof(dateTime))
        };

        return DateTime.UtcNow - utcInput;
    }
}

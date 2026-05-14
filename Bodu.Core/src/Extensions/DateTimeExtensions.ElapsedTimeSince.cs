// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.ElapsedTimeSince.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a <see cref="TimeSpan"/> representing the time that has elapsed between the specified <paramref name="dateTime"/> and <see cref="DateTime.UtcNow"/>.
    /// </summary>
    /// <param name="dateTime">The date and time value to compare against <see cref="DateTime.UtcNow"/>. Must have a <see cref="DateTime.Kind"/> of either <see cref="DateTimeKind.Utc"/> or <see cref="DateTimeKind.Local"/>.</param>
    /// <returns>The signed time interval between <paramref name="dateTime"/> and the current UTC time; that is, <see cref="DateTime.UtcNow"/> minus <paramref name="dateTime"/>.</returns>
    /// <remarks>
    /// <para>If <paramref name="dateTime"/> is in local time (<see cref="DateTimeKind.Local"/>), it is converted to UTC using <see cref="DateTime.ToUniversalTime"/> before comparison. <see cref="DateTimeKind.Unspecified"/> values are rejected to avoid ambiguity regarding the time zone context.</para>
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown if <paramref name="dateTime"/> has a <see cref="DateTime.Kind"/> of <see cref="DateTimeKind.Unspecified"/>.</exception>
    public static TimeSpan ElapsedTimeSince(this DateTime dateTime)
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

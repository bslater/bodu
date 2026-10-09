// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.TryToDateTimeOffset.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Attempts to create a <see cref="DateTimeOffset" /> with the same wall-clock value as
    /// <paramref name="dateTime" /> and the supplied <paramref name="offset" />.
    /// </summary>
    /// <param name="dateTime">The wall-clock date and time to convert.</param>
    /// <param name="offset">The UTC offset to assign.</param>
    /// <param name="result">The converted value on success; otherwise the default value.</param>
    /// <returns>
    /// <see langword="true" /> when the requested offset is valid, compatible with
    /// <see cref="DateTime.Kind" />, and the resulting UTC instant is representable; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// This is the non-throwing counterpart of <see cref="ToDateTimeOffset(DateTime, TimeSpan)" />.
    /// As with the constructor, <see cref="DateTimeKind.Unspecified" /> permits an explicit fixed offset,
    /// <see cref="DateTimeKind.Utc" /> requires a zero offset, and <see cref="DateTimeKind.Local" /> requires
    /// the corresponding system-local offset. No offset is inferred from the machine when the date is Unspecified.
    /// </remarks>
    public static bool TryToDateTimeOffset(this DateTime dateTime, TimeSpan offset, out DateTimeOffset result)
    {
        try
        {
            result = new DateTimeOffset(dateTime, offset);
            return true;
        }
        catch (ArgumentException)
        {
            result = default;
            return false;
        }
    }
}

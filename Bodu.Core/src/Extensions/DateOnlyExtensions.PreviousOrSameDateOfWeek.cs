// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.PreviousOrSameDateOfWeek.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the previous calendar occurrence of the specified
    /// <see cref="DayOfWeek" /> at or before the given <paramref name="date" />.
    /// </summary>
    /// <param name="date">The starting date value from which to search backward.</param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek" /> to locate. For example, <see cref="DayOfWeek.Monday" /> returns the previous Monday
    /// on or before <paramref name="date" />.
    /// </param>
    /// <returns>
    /// A <see cref="DateOnly" /> value set to the previous occurrence of <paramref name="dayOfWeek" /> at or before
    /// <paramref name="date" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If <paramref name="date" /> already falls on the specified <paramref name="dayOfWeek" />, the result is
    /// <paramref name="date" /> itself. This is the on-or-before counterpart of
    /// <see cref="PreviousDateOfWeek(DateOnly, DayOfWeek)" />, which always retreats by at least one day.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek" /> is not a defined value of the <see cref="DayOfWeek" /> enumeration.
    /// </exception>
    public static DateOnly PreviousOrSameDateOfWeek(this DateOnly date, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        return date.AddDays(-(((int)date.DayOfWeek - (int)dayOfWeek + 7) % 7));
    }
}

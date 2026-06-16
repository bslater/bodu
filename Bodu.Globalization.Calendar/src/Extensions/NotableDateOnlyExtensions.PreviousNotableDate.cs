// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.PreviousNotableDate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NotableDateOnlyExtensions
{
    /// <summary>
    /// Returns the most recent notable date emitted strictly before the date for the territory.
    /// </summary>
    /// <param name="date">The reference date.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="filter">An optional filter the occurrence must satisfy.</param>
    /// <returns>
    /// The previous matching occurrence, or <see langword="null" /> when none exists down to the minimum year.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static NotableDate? PreviousNotableDate(this DateOnly date, INotableDateService service, string territory, NotableDateFilter? filter = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(territory);

        for (int year = date.Year; year >= DateOnly.MinValue.Year; year--)
        {
            IReadOnlyList<NotableDate> resolved = ResolveYear(year, service, territory, filter);
            for (int i = resolved.Count - 1; i >= 0; i--)
            {
                if (resolved[i].Date < date)
                    return resolved[i];
            }
        }

        return null;
    }
}

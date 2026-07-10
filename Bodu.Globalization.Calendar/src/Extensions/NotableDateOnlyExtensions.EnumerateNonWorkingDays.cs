// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.EnumerateNonWorkingDays.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NotableDateOnlyExtensions
{
    /// <summary>
    /// Lazily enumerates the non-working days in the inclusive range, in ascending order.
    /// </summary>
    /// <param name="start">The inclusive start date.</param>
    /// <param name="end">The inclusive end date.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The non-working days in the range.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// INotableDateService service = AsiaPacificCalendarData.CreateService("AU");
    ///
    /// // Anzac week 2024: the weekend plus Anzac Day (Thursday 25 April).
    /// foreach (DateOnly closed in new DateOnly(2024, 4, 22).EnumerateNonWorkingDays(
    ///     new DateOnly(2024, 4, 28), service, "AU"))
    /// {
    ///     // 2024-04-25, 2024-04-27, 2024-04-28
    /// }
    ///]]>
    /// </code>
    /// </example>
    public static IEnumerable<DateOnly> EnumerateNonWorkingDays(this DateOnly start, DateOnly end, INotableDateService service, string territory, WeekPattern? workingWeek = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(territory);

        return Iterator();

        IEnumerable<DateOnly> Iterator()
        {
            for (DateOnly cursor = start; cursor <= end; cursor = cursor.AddDays(1))
            {
                if (cursor.IsNonWorkingDay(service, territory, workingWeek))
                    yield return cursor;
            }
        }
    }
}

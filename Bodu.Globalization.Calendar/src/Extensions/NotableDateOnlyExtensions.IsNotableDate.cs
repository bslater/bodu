// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.IsNotableDate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NotableDateOnlyExtensions
{
    /// <summary>
    /// Determines whether any notable date is emitted on the date for the territory.
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="filter">An optional filter the occurrence must satisfy.</param>
    /// <returns>
    /// <see langword="true" /> if at least one occurrence is emitted; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// INotableDateService service = AmericasCalendarData.CreateService("US");
    ///
    /// bool isHoliday = new DateOnly(2026, 7, 4).IsNotableDate(service, "US"); // true (Independence Day)
    ///]]>
    /// </code>
    /// </example>
    public static bool IsNotableDate(this DateOnly date, INotableDateService service, string territory, NotableDateFilter? filter = null) =>
        date.GetNotableDates(service, territory, filter).Count > 0;
}

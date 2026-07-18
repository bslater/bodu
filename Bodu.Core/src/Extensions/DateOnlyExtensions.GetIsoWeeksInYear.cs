// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.GetIsoWeeksInYear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns the number of ISO 8601 weeks in the specified <paramref name="year" />.
    /// </summary>
    /// <param name="year">
    /// The ISO 8601 year to evaluate. Must be between the <c>Year</c> property values of
    /// <see cref="DateOnly.MinValue" /> and <see cref="DateOnly.MaxValue" />, inclusive.
    /// </param>
    /// <returns>The number of ISO 8601 weeks in the supplied year — either 52 or 53.</returns>
    /// <remarks>
    /// <para>
    /// According to ISO 8601, a year contains 53 weeks if either of the following is true:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>January 1 of the supplied year falls on a Thursday;</description>
    /// </item>
    /// <item>
    /// <description>
    /// December 31 of the supplied year falls on a Thursday (equivalent to January 1 of the following year falling on a
    /// Friday).
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// All other years contain exactly 52 weeks. This member delegates to
    /// <see cref="DateTimeExtensions.GetIsoWeeksInYear(int)" /> — the twins share one implementation, so both surfaces
    /// always agree.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="year" /> is less than the <c>Year</c> of <see cref="DateOnly.MinValue" /> or greater
    /// than that of <see cref="DateOnly.MaxValue" />.
    /// </exception>
    public static int GetIsoWeeksInYear(int year) =>
        DateTimeExtensions.GetIsoWeeksInYear(year);
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateRangeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the range-vs-range containment and overlap operations on <see cref="DateRange" />.
/// </summary>
[TestClass]
public sealed partial class DateRangeTests
{
    /// <summary>
    /// Builds a January 2025 range from a start and end day.
    /// </summary>
    /// <param name="startDay">The start day of month.</param>
    /// <param name="endDay">The end day of month.</param>
    /// <returns>The range.</returns>
    private static DateRange Range(int startDay, int endDay) =>
        new(new DateOnly(2025, 1, startDay), new DateOnly(2025, 1, endDay));

}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.FirstAndLastDateData.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{

    /// <summary>
    /// Per-case input/expected pair used by the first-day/last-day data-driven tests across the weekend,
    /// culture, and month/year families. Only the fields relevant to a given test are set; the remainder
    /// default to <see langword="null" />.
    /// </summary>
    public struct FirstAndLastDateData
    {

        public CultureInfo? CultureInfo { get; set; }

        public DateTime ExpectedFirst { get; set; }

        public DateTime ExpectedLast { get; set; }

        public DateTime Input { get; set; }

        public CalendarWeekendDefinition? Weekend { get; set; }

    }

}

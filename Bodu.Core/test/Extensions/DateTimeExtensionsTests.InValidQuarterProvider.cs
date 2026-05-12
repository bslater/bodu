// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.InValidQuarterProvider.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{
    /// <summary>
    /// Test-only <see cref="IQuarterDefinitionProvider" /> that intentionally returns out-of-range quarter
    /// numbers and throws from every date-returning overload. Drives the validation paths in the
    /// provider-backed quarter extensions.
    /// </summary>
    public sealed class InValidQuarterProvider : IQuarterDefinitionProvider
    {
        /// <summary>
        /// Always returns an invalid quarter number (outside the expected range of 1–4).
        /// </summary>
        /// <param name="dateTime">The input <see cref="DateTime" />.</param>
        /// <returns>An invalid quarter number.</returns>
        public int GetQuarter(DateTime dateTime)
        {
            return dateTime.Month switch
            {
                1 => 0,  // Invalid (below range)
                2 => -5, // Invalid (negative)
                3 => 5,  // Invalid (above range)
                4 => 999, // Invalid (far above range)
                _ => 10  // Also invalid
            };
        }

        public int GetQuarter(DateOnly dateOnly) => throw new ArgumentOutOfRangeException(nameof(dateOnly), "This provider intentionally returns invalid quarter mappings.");

        /// <summary>
        /// Always throws <see cref="ArgumentOutOfRangeException" /> to simulate an invalid quarter mapping.
        /// </summary>
        /// <param name="dateTime">The input <see cref="DateTime" />.</param>
        public DateTime GetQuarterEnd(DateTime dateTime) => throw new ArgumentOutOfRangeException(nameof(dateTime), "This provider intentionally returns invalid quarter mappings.");

        public DateTime GetQuarterEnd(int quarter) => throw new ArgumentOutOfRangeException(nameof(quarter), "This provider intentionally returns invalid quarter mappings.");

        public DateOnly GetQuarterEndDate(DateOnly dateOnly) => throw new ArgumentOutOfRangeException(nameof(dateOnly), "This provider intentionally returns invalid quarter mappings.");

        public DateOnly GetQuarterEndDate(int quarter) => throw new ArgumentOutOfRangeException(nameof(quarter), "This provider intentionally returns invalid quarter mappings.");

        /// <summary>
        /// Always throws <see cref="ArgumentOutOfRangeException" /> to simulate an invalid quarter mapping.
        /// </summary>
        /// <param name="dateTime">The input <see cref="DateTime" />.</param>
        public DateTime GetQuarterStart(DateTime dateTime) => throw new ArgumentOutOfRangeException(nameof(dateTime), "This provider intentionally returns invalid quarter mappings.");

        public DateTime GetQuarterStart(int quarter) => throw new ArgumentOutOfRangeException(nameof(quarter), "This provider intentionally returns invalid quarter mappings.");

        public DateOnly GetQuarterStartDate(DateOnly dateOnly) => throw new ArgumentOutOfRangeException(nameof(dateOnly), "This provider intentionally returns invalid quarter mappings.");

        public DateOnly GetQuarterStartDate(int quarter) => throw new ArgumentOutOfRangeException(nameof(quarter), "This provider intentionally returns invalid quarter mappings.");

        public DateTime GetQuarterStart(int quarter, int fiscalYear) => throw new ArgumentOutOfRangeException(nameof(quarter), "This provider intentionally returns invalid quarter mappings.");

        public DateTime GetQuarterEnd(int quarter, int fiscalYear) => throw new ArgumentOutOfRangeException(nameof(quarter), "This provider intentionally returns invalid quarter mappings.");

        public DateOnly GetQuarterStartDate(int quarter, int fiscalYear) => throw new ArgumentOutOfRangeException(nameof(quarter), "This provider intentionally returns invalid quarter mappings.");

        public DateOnly GetQuarterEndDate(int quarter, int fiscalYear) => throw new ArgumentOutOfRangeException(nameof(quarter), "This provider intentionally returns invalid quarter mappings.");

        public bool Is53WeekFiscalYear(int fiscalYear) => throw new ArgumentOutOfRangeException(nameof(fiscalYear), "This provider intentionally returns invalid quarter mappings.");

        public int GetWeeksInFiscalYear(int fiscalYear) => throw new ArgumentOutOfRangeException(nameof(fiscalYear), "This provider intentionally returns invalid quarter mappings.");
    }
}

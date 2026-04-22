using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Defines a contract for computing the calendar date of a notable event (e.g., Easter, Lunar New Year) based on a given year and
/// calendar system.
/// </summary>
/// <remarks>
/// Implementations of <see cref="INotableDateCalculator" /> provide algorithmic calculation of dates that cannot be represented by a
/// fixed day and month, often varying by year and calendar type.
/// </remarks>
public interface INotableDateCalculator
{
	/// <summary>
	/// Computes the calendar date of the notable event for the specified year and optional calendar system.
	/// </summary>
	/// <param name="year">The target year for the calculation. Must be greater than or equal to 1.</param>
	/// <param name="calendar">
	/// Optional. A <see cref="SysGlobal.Calendar" /> instance representing the calendar system to use. If <c>null</c>, the default
	/// <see cref="SysGlobal.GregorianCalendar" /> is assumed.
	/// </param>
	/// <returns>
	/// The computed <see cref="DateTime" /> representing the notable event in the given year and calendar system, or <c>null</c> if the
	/// date cannot be determined.
	/// </returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="year" /> is less than 1.</exception>
	/// <exception cref="NotSupportedException">
	/// Thrown when the specified <paramref name="calendar" /> type is not supported by the calculator.
	/// </exception>
	DateTime? GetDate(int year, SysGlobal.Calendar? calendar = null);
}

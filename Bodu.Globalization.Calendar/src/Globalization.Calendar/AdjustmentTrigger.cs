namespace Bodu.Globalization.Calendar
{
	/// <summary>
	/// Identifies the condition under which an <see cref="ObservanceAdjustment" /> activates and applies its action.
	/// </summary>
	/// <remarks>
	/// This enumeration replaces <c>NotableDateAdjustmentRuleType</c>. The new name distinguishes the <em>trigger</em> (when an adjustment
	/// fires) from the <em>action</em> (what it does), and removes the redundant <c>...Type</c> suffix.
	/// </remarks>
	public enum AdjustmentTrigger
	{
		/// <summary>
		/// The adjustment always activates regardless of the calculated date.
		/// </summary>
		Always = 0,

		/// <summary>
		/// Activates when the calculated date falls on the day of week specified by <c>ObservanceAdjustment.DayOfWeek</c>.
		/// </summary>
		IfDayOfWeek,

		/// <summary>
		/// Activates when the calculated date falls on a weekend, as determined by the configured <c>CalendarWeekendDefinition</c>.
		/// </summary>
		IfWeekend,

		/// <summary>
		/// Activates when the calculated date falls on a weekday.
		/// </summary>
		IfWeekday,

		/// <summary>
		/// Activates when the calculated date is classified as a non-working day for the active territory and calendar.
		/// </summary>
		IfNonWorkingDay,

		/// <summary>
		/// Activates when the calculated date occurs strictly before <c>ObservanceAdjustment.ComparisonDate</c>.
		/// </summary>
		IfBeforeFixedDate,

		/// <summary>
		/// Activates when the calculated date occurs strictly after <c>ObservanceAdjustment.ComparisonDate</c>.
		/// </summary>
		IfAfterFixedDate,

		/// <summary>
		/// Activates when the calculated date falls within a leap year.
		/// </summary>
		IfLeapYear,

		/// <summary>
		/// Activates when the calculated date is the n-th occurrence of its weekday within its month, as specified by
		/// <c>ObservanceAdjustment.WeekOrdinal</c>.
		/// </summary>
		IfNthOccurrenceInMonth,

		/// <summary>
		/// Activation is delegated to a registered <see cref="IAdjustmentHandler" /> looked up by <c>ObservanceAdjustment.HandlerKey</c>.
		/// </summary>
		Custom,
	}
}

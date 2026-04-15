namespace Bodu.Globalization.Calendar
{
	/// <summary>
	/// Specifies a conditional adjustment that may shift the calculated date of a <see cref="NotableDateRule" /> based on contextual
	/// factors such as the day of week, the active territory, or the calendar system.
	/// </summary>
	/// <remarks>
	/// <para>
	/// An <see cref="ObservanceAdjustment" /> consists of a <see cref="Trigger" /> describing the activation condition and an
	/// <see cref="Action" /> describing the resulting modification. Adjustments are evaluated in <see cref="Priority" /> order during date
	/// generation and may be scoped to a single territory, calendar, or year range.
	/// </para>
	/// <para>
	/// This type replaces the earlier <c>NotableDateAdjustmentRule</c>. The new name distinguishes the adjustment <em>specification</em>
	/// from the resolution <em>rule</em> that owns it.
	/// </para>
	/// </remarks>
	public sealed record ObservanceAdjustment
	{
		/// <summary>
		/// Gets the condition that determines whether the adjustment activates for a given calculated date.
		/// </summary>
		public AdjustmentTrigger Trigger { get; init; }

		/// <summary>
		/// Gets the action applied when the adjustment activates.
		/// </summary>
		public AdjustmentAction Action { get; init; }

		/// <summary>
		/// Gets the day of week required by <see cref="AdjustmentTrigger.IfDayOfWeek" />, or <see langword="null" /> when not applicable.
		/// </summary>
		public DayOfWeek? DayOfWeek { get; init; }

		/// <summary>
		/// Gets a value overriding whether the adjusted date is treated as a non-working day. When <see langword="null" />, the
		/// non-working flag from the owning rule is preserved.
		/// </summary>
		public bool? IsNonWorkingDay { get; init; }

		/// <summary>
		/// Gets the integer day offset applied by <see cref="AdjustmentAction.AddDays" />. Negative values move the date backwards.
		/// </summary>
		public int OffsetDays { get; init; }

		/// <summary>
		/// Gets the comma-separated list of territory codes that scope the adjustment. When <see langword="null" /> or empty, the
		/// adjustment applies in every territory the owning rule applies to.
		/// </summary>
		public string? TerritoryCode { get; init; }

		/// <summary>
		/// Gets the calendar system that scopes the adjustment, or <see langword="null" /> for any calendar.
		/// </summary>
		public Type? CalendarType { get; init; }

		/// <summary>
		/// Gets the inclusive minimum year for which the adjustment is effective, or <see langword="null" /> for no lower bound.
		/// </summary>
		public int? EffectiveFromYear { get; init; }

		/// <summary>
		/// Gets the inclusive maximum year for which the adjustment is effective, or <see langword="null" /> for no upper bound.
		/// </summary>
		public int? EffectiveToYear { get; init; }

		/// <summary>
		/// Gets the comparison date used by <see cref="AdjustmentTrigger.IfBeforeFixedDate" /> and
		/// <see cref="AdjustmentTrigger.IfAfterFixedDate" />.
		/// </summary>
		/// <remarks>
		/// The year component is replaced with the year being resolved at evaluation time, so authors only need to supply a month and day.
		/// </remarks>
		public DateTime? ComparisonDate { get; init; }

		/// <summary>
		/// Gets the ordinal occurrence required by <see cref="AdjustmentTrigger.IfNthOccurrenceInMonth" />, or <see langword="null" /> when
		/// not applicable.
		/// </summary>
		public WeekOfMonthOrdinal? WeekOrdinal { get; init; }

		/// <summary>
		/// Gets the name of another <see cref="NotableDateRule" /> referenced by <see cref="AdjustmentAction.ReplaceWithNamedDate" />.
		/// </summary>
		public string? TargetRuleName { get; init; }

		/// <summary>
		/// Gets the evaluation priority. Lower values are evaluated first; the default of 100 leaves room for both higher- and
		/// lower-priority adjustments to be inserted later.
		/// </summary>
		public int Priority { get; init; } = 100;

		/// <summary>
		/// Gets the registry key used to look up an <see cref="IAdjustmentHandler" /> when <see cref="Trigger" /> or <see cref="Action" />
		/// is <see cref="AdjustmentTrigger.Custom" />/<see cref="AdjustmentAction.Custom" />.
		/// </summary>
		public string? HandlerKey { get; init; }

		/// <summary>
		/// Gets an optional dictionary of parameters forwarded to the registered <see cref="IAdjustmentHandler" />.
		/// </summary>
		public IReadOnlyDictionary<string, string>? HandlerParameters { get; init; }
	}
}

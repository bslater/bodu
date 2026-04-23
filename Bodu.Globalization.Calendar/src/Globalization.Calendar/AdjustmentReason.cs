
namespace Bodu.Globalization.Calendar;

/// <summary>
/// Describes why a <see cref="NotableDate" /> was shifted from its originally calculated date by an
/// <see cref="ObservanceAdjustment" />.
/// </summary>
/// <remarks>
/// <para>
/// An <see cref="AdjustmentReason" /> is attached to a <see cref="NotableDate" /> when, and only when, the date was relocated by a
/// triggered observance adjustment. Consumers can use it for diagnostics ("why is Anzac Day showing on a Monday?") or to render
/// observed-vs-actual labels in user interfaces.
/// </para>
/// </remarks>
public sealed record AdjustmentReason
{
	/// <summary>
	/// Initialises a new instance of the <see cref="AdjustmentReason" /> record.
	/// </summary>
	/// <param name="originalDate">The date that was originally calculated before the adjustment fired.</param>
	/// <param name="trigger">The trigger condition that activated the adjustment.</param>
	/// <param name="action">The action that the adjustment performed.</param>
	/// <param name="handlerKey">Optional key identifying the custom handler when <paramref name="action" /> is <see cref="AdjustmentAction.Custom" />.</param>
	public AdjustmentReason(DateTime originalDate, AdjustmentTrigger trigger, AdjustmentAction action, string? handlerKey = null)
	{
		OriginalDate = originalDate;
		Trigger = trigger;
		Action = action;
		HandlerKey = handlerKey;
	}

	/// <summary>
	/// Gets the date that was originally calculated for the notable date before the adjustment fired.
	/// </summary>
	public DateTime OriginalDate { get; }

	/// <summary>
	/// Gets the trigger condition that activated the adjustment.
	/// </summary>
	public AdjustmentTrigger Trigger { get; }

	/// <summary>
	/// Gets the action that the adjustment performed.
	/// </summary>
	public AdjustmentAction Action { get; }

	/// <summary>
	/// Gets the optional key identifying the custom handler that performed a <see cref="AdjustmentAction.Custom" /> action.
	/// </summary>
	public string? HandlerKey { get; }
}

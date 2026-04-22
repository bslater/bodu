
namespace Bodu.Globalization.Calendar;

/// <summary>
/// Supplies runtime modifications to the notable date rules loaded by base <see cref="INotableDateRuleProvider" /> sources.
/// </summary>
/// <remarks>
/// <para>
/// Override providers are layered on top of the base rule set, allowing applications to disable a holiday for a particular year, alter
/// an existing rule's territory or non-working flag, or add an entirely new one-off observance, without rebuilding the underlying
/// authoring source. The <see cref="NotableDateService" /> applies overrides in registration order: later providers override earlier
/// ones.
/// </para>
/// </remarks>
public interface INotableDateRuleOverrideProvider
{
	/// <summary>
	/// Returns the names of base rules that should be removed from the active rule set, optionally scoped to specific years.
	/// </summary>
	/// <returns>The override removals.</returns>
	IEnumerable<RuleRemoval> GetRemovals();

	/// <summary>
	/// Returns the additional <see cref="NotableDateRule" /> instances to layer on top of the base rule set. Rules with the same
	/// <see cref="NotableDateRule.Name" /> as a base rule replace that base rule entirely.
	/// </summary>
	/// <returns>The additional rules.</returns>
	IEnumerable<NotableDateRule> GetAdditions();
}

/// <summary>
/// Specifies that a notable date rule should be suppressed by an <see cref="INotableDateRuleOverrideProvider" />.
/// </summary>
/// <param name="RuleName">The name of the rule to suppress.</param>
/// <param name="FromYear">Optional inclusive first year of the suppression. <see langword="null" /> for no lower bound.</param>
/// <param name="ToYear">Optional inclusive last year of the suppression. <see langword="null" /> for no upper bound.</param>
/// <param name="TerritoryCode">Optional territory scope. When supplied, suppression applies only when resolving rules for the given territory.</param>
public sealed record RuleRemoval(string RuleName, int? FromYear = null, int? ToYear = null, string? TerritoryCode = null);

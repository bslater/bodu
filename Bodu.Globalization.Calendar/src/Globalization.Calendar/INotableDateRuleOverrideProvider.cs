// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateRuleOverrideProvider.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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

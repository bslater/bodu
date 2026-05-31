// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateRuleOverrideProvider.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Supplies runtime modifications to the notable date rules loaded by base <see cref="INotableDateRuleProvider" />
/// sources.
/// </summary>
/// <remarks>
/// <para>
/// Override providers are layered on top of the base rule set, allowing applications to disable a holiday for a
/// particular year, alter an existing rule's territory or non-working flag, or add an entirely new one-off observance,
/// without rebuilding the underlying authoring source. The <see cref="NotableDateService" /> applies overrides in
/// registration order: later providers override earlier ones.
/// </para>
/// </remarks>
/// <example>
/// <para>
/// Suppress Boxing Day for 2026 and inject a company-specific observance:
/// </para>
/// <code>
///<![CDATA[
/// public sealed class CompanyCalendarOverrides : INotableDateRuleOverrideProvider
/// {
///     public IEnumerable<RuleRemoval> GetRemovals()
///     {
///         // Remove Boxing Day for 2026 only:
///         yield return new RuleRemoval("Boxing Day", FromYear: 2026, ToYear: 2026);
///     }
///
///     public IEnumerable<NotableDateRule> GetAdditions()
///     {
///         yield return new NotableDateRule
///         {
///             Name = "Company Founding Day",
///             Strategy = DateResolutionStrategy.Fixed,
///             Category = NotableDateCategory.Observance,
///             Month = 6,
///             Day = 15,
///             IsNonWorkingDay = true,
///         };
///     }
/// }
///]]>
/// </code>
/// </example>
public interface INotableDateRuleOverrideProvider
{
    /// <summary>
    /// Returns the names of base rules that should be removed from the active rule set, optionally scoped to specific
    /// years.
    /// </summary>
    /// <returns>The override removals.</returns>
    IEnumerable<RuleRemoval> GetRemovals();

    /// <summary>
    /// Returns the additional <see cref="NotableDateRule" /> instances to layer on top of the base rule set. Rules with
    /// the same <see cref="NotableDateRule.Name" /> as a base rule replace that base rule entirely.
    /// </summary>
    /// <returns>The additional rules.</returns>
    IEnumerable<NotableDateRule> GetAdditions();
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RuleRemoval.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;


/// <summary>
/// Specifies that a notable date rule should be suppressed by an <see cref="INotableDateRuleOverrideProvider" />.
/// </summary>
/// <param name="RuleName">The name of the rule to suppress.</param>
/// <param name="FromYear">Optional inclusive first year of the suppression. <see langword="null" /> for no lower bound.</param>
/// <param name="ToYear">Optional inclusive last year of the suppression. <see langword="null" /> for no upper bound.</param>
/// <param name="TerritoryCode">Optional territory scope. When supplied, suppression applies only when resolving rules for the given territory.</param>
public sealed record RuleRemoval(string RuleName, int? FromYear = null, int? ToYear = null, string? TerritoryCode = null);

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RemoveRuleOverride.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Represents an override operation that removes exactly one rule, identified by its notable-date and rule ids.
/// </summary>
internal sealed class RemoveRuleOverride : NotableDateRuleOverride
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveRuleOverride" /> class.
    /// </summary>
    /// <param name="notableDateRef">The identifier of the targeted notable-date concept.</param>
    /// <param name="ruleRef">The identifier of the rule to remove.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="notableDateRef" /> or <paramref name="ruleRef" /> is <see langword="null" />.
    /// </exception>
    public RemoveRuleOverride(string notableDateRef, string ruleRef)
        : base(notableDateRef)
    {
        ThrowHelper.ThrowIfNull(ruleRef);

        this.RuleRef = ruleRef;
    }

    /// <summary>
    /// Gets the identifier of the rule to remove.
    /// </summary>
    /// <returns>The targeted rule id.</returns>
    public string RuleRef { get; }
}

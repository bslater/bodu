// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AddRuleOverride.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Represents an override operation that adds a new rule to an existing notable-date concept.
/// </summary>
internal sealed class AddRuleOverride : NotableDateRuleOverride
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddRuleOverride" /> class.
    /// </summary>
    /// <param name="notableDateRef">The identifier of the targeted notable-date concept.</param>
    /// <param name="rule">The rule to add.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="notableDateRef" /> or <paramref name="rule" /> is <see langword="null" />.
    /// </exception>
    public AddRuleOverride(string notableDateRef, NotableDateRule rule)
        : base(notableDateRef)
    {
        ThrowHelper.ThrowIfNull(rule);

        this.Rule = rule;
    }

    /// <summary>
    /// Gets the rule to add.
    /// </summary>
    /// <returns>The new rule.</returns>
    public NotableDateRule Rule { get; }
}

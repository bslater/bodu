// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OverrideEntry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Represents a single override operation captured by an <see cref="OverrideBuilder" />, carrying the target references
/// and, for add and patch operations, the configured rule builder.
/// </summary>
internal sealed class OverrideEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OverrideEntry" /> class.
    /// </summary>
    /// <param name="operation">The kind of override operation.</param>
    /// <param name="notableDateRef">The identifier of the targeted concept.</param>
    /// <param name="ruleRef">
    /// The identifier of the targeted rule, or <see langword="null" /> for an add operation.
    /// </param>
    /// <param name="rule">The configured rule builder for add and patch operations, or <see langword="null" />.</param>
    internal OverrideEntry(OverrideOperation operation, string notableDateRef, string? ruleRef, NotableDateRuleBuilder? rule)
    {
        Operation = operation;
        NotableDateRef = notableDateRef;
        RuleRef = ruleRef;
        Rule = rule;
    }

    /// <summary>
    /// Gets the kind of override operation.
    /// </summary>
    /// <value>The override operation kind.</value>
    internal OverrideOperation Operation { get; }

    /// <summary>
    /// Gets the identifier of the targeted concept.
    /// </summary>
    /// <value>The concept identifier.</value>
    internal string NotableDateRef { get; }

    /// <summary>
    /// Gets the identifier of the targeted rule.
    /// </summary>
    /// <value>The rule identifier, or <see langword="null" /> for an add operation.</value>
    internal string? RuleRef { get; }

    /// <summary>
    /// Gets the configured rule builder for add and patch operations.
    /// </summary>
    /// <value>The rule builder, or <see langword="null" /> for a remove operation.</value>
    internal NotableDateRuleBuilder? Rule { get; }

    /// <summary>
    /// Creates a deep copy of this override entry.
    /// </summary>
    /// <returns>A new <see cref="OverrideEntry" /> carrying the same state.</returns>
    internal OverrideEntry Clone() =>
        new(Operation, NotableDateRef, RuleRef, Rule?.Clone());
}

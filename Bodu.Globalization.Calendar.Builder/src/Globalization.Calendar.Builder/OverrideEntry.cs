// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OverrideEntry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Represents a single override operation captured by an <see cref="OverrideBuilder" />, carrying the target
/// references and, for add and patch operations, the configured rule builder.
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
        this.Operation = operation;
        this.NotableDateRef = notableDateRef;
        this.RuleRef = ruleRef;
        this.Rule = rule;
    }

    /// <summary>
    /// Gets the kind of override operation.
    /// </summary>
    /// <returns>The override operation kind.</returns>
    internal OverrideOperation Operation { get; }

    /// <summary>
    /// Gets the identifier of the targeted concept.
    /// </summary>
    /// <returns>The concept identifier.</returns>
    internal string NotableDateRef { get; }

    /// <summary>
    /// Gets the identifier of the targeted rule.
    /// </summary>
    /// <returns>The rule identifier, or <see langword="null" /> for an add operation.</returns>
    internal string? RuleRef { get; }

    /// <summary>
    /// Gets the configured rule builder for add and patch operations.
    /// </summary>
    /// <returns>The rule builder, or <see langword="null" /> for a remove operation.</returns>
    internal NotableDateRuleBuilder? Rule { get; }

    /// <summary>
    /// Creates a deep copy of this override entry.
    /// </summary>
    /// <returns>A new <see cref="OverrideEntry" /> carrying the same state.</returns>
    internal OverrideEntry Clone() =>
        new(this.Operation, this.NotableDateRef, this.RuleRef, this.Rule?.Clone());
}

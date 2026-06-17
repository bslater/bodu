// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OverrideBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Provides a fluent surface for authoring identifier-targeted override operations that add, patch, or remove rules
/// after the base concepts and imports are assembled.
/// </summary>
public sealed class OverrideBuilder
{
    /// <summary>The override operations, in declaration order.</summary>
    private readonly List<OverrideEntry> _entries = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="OverrideBuilder" /> class with no operations.
    /// </summary>
    internal OverrideBuilder()
    {
    }

    /// <summary>
    /// Gets the override operations.
    /// </summary>
    /// <returns>The override entries, in declaration order.</returns>
    internal IReadOnlyList<OverrideEntry> Entries =>
        _entries;

    /// <summary>
    /// Adds a new rule to an existing concept.
    /// </summary>
    /// <param name="notableDateRef">The identifier of the concept to extend.</param>
    /// <param name="ruleId">The stable identifier of the new rule.</param>
    /// <param name="configure">A delegate that configures the new rule.</param>
    /// <returns>The same <see cref="OverrideBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="notableDateRef" /> or <paramref name="ruleId" /> is <see langword="null" />, empty, or
    /// white-space.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="configure" /> is <see langword="null" />.</exception>
    public OverrideBuilder AddRule(string notableDateRef, string ruleId, Action<NotableDateRuleBuilder> configure)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(notableDateRef);
        ThrowHelper.ThrowIfNullOrWhiteSpace(ruleId);
        ThrowHelper.ThrowIfNull(configure);

        NotableDateRuleBuilder rule = new(ruleId);
        configure(rule);
        _entries.Add(new OverrideEntry(OverrideOperation.AddRule, notableDateRef, null, rule));
        return this;
    }

    /// <summary>
    /// Patches selected sections of an existing rule. Only the rule sections configured by the delegate are emitted.
    /// </summary>
    /// <param name="notableDateRef">The identifier of the concept that owns the rule.</param>
    /// <param name="ruleRef">The identifier of the rule to patch.</param>
    /// <param name="configure">A delegate that configures the sections to patch.</param>
    /// <returns>The same <see cref="OverrideBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="notableDateRef" /> or <paramref name="ruleRef" /> is <see langword="null" />, empty, or
    /// white-space.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="configure" /> is <see langword="null" />.</exception>
    public OverrideBuilder PatchRule(string notableDateRef, string ruleRef, Action<NotableDateRuleBuilder> configure)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(notableDateRef);
        ThrowHelper.ThrowIfNullOrWhiteSpace(ruleRef);
        ThrowHelper.ThrowIfNull(configure);

        NotableDateRuleBuilder rule = new(ruleRef);
        configure(rule);
        _entries.Add(new OverrideEntry(OverrideOperation.PatchRule, notableDateRef, ruleRef, rule));
        return this;
    }

    /// <summary>
    /// Removes an existing rule from a concept.
    /// </summary>
    /// <param name="notableDateRef">The identifier of the concept that owns the rule.</param>
    /// <param name="ruleRef">The identifier of the rule to remove.</param>
    /// <returns>The same <see cref="OverrideBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="notableDateRef" /> or <paramref name="ruleRef" /> is <see langword="null" />, empty, or
    /// white-space.
    /// </exception>
    public OverrideBuilder RemoveRule(string notableDateRef, string ruleRef)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(notableDateRef);
        ThrowHelper.ThrowIfNullOrWhiteSpace(ruleRef);

        _entries.Add(new OverrideEntry(OverrideOperation.RemoveRule, notableDateRef, ruleRef, null));
        return this;
    }

    /// <summary>
    /// Adds a pre-built override entry when reconstructing the override set from a parsed document.
    /// </summary>
    /// <param name="entry">The override entry to add.</param>
    internal void AddEntry(OverrideEntry entry) =>
        _entries.Add(entry);

    /// <summary>
    /// Creates a deep copy of this override builder.
    /// </summary>
    /// <returns>A new <see cref="OverrideBuilder" /> carrying the same operations.</returns>
    internal OverrideBuilder Clone()
    {
        OverrideBuilder clone = new();
        foreach (OverrideEntry entry in _entries)
            clone._entries.Add(entry.Clone());

        return clone;
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDefinitionBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Provides a fluent surface for authoring a notable-date concept: its identity, default category, duration and
/// non-working flags, concept-level tags, and one or more calculation rules.
/// </summary>
public sealed class NotableDateDefinitionBuilder
{
    /// <summary>
    /// The concept-level tags, in declaration order.
    /// </summary>
    private readonly List<string> _tags = new();

    /// <summary>
    /// The rules belonging to the concept, in declaration order.
    /// </summary>
    private readonly List<NotableDateRuleBuilder> _rules = new();

    /// <summary>
    /// The stable identifier of the concept.
    /// </summary>
    private string _id;

    /// <summary>
    /// The human-readable display name of the concept.
    /// </summary>
    private string _displayName;

    /// <summary>
    /// The category of the concept.
    /// </summary>
    private NotableDateCategory _category;

    /// <summary>
    /// The default duration in days, or <see langword="null" /> when the schema default of one day applies.
    /// </summary>
    private int? _defaultDurationDays;

    /// <summary>
    /// The default non-working flag, or <see langword="null" /> when unset.
    /// </summary>
    private bool? _defaultNonWorkingDay;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateDefinitionBuilder" /> class.
    /// </summary>
    /// <param name="id">The stable identifier of the concept.</param>
    /// <param name="displayName">The human-readable display name of the concept.</param>
    /// <param name="category">The category of the concept.</param>
    internal NotableDateDefinitionBuilder(string id, string displayName, NotableDateCategory category)
    {
        this._id = id;
        this._displayName = displayName;
        this._category = category;
    }

    /// <summary>
    /// Gets the stable identifier of the concept.
    /// </summary>
    /// <returns>The concept identifier.</returns>
    internal string Id =>
        this._id;

    /// <summary>
    /// Gets the human-readable display name of the concept.
    /// </summary>
    /// <returns>The display name.</returns>
    internal string DisplayName =>
        this._displayName;

    /// <summary>
    /// Gets the category of the concept.
    /// </summary>
    /// <returns>The category.</returns>
    internal NotableDateCategory Category =>
        this._category;

    /// <summary>
    /// Gets the default duration in days.
    /// </summary>
    /// <returns>The default duration, or <see langword="null" /> when unset.</returns>
    internal int? DefaultDurationDays =>
        this._defaultDurationDays;

    /// <summary>
    /// Gets the default non-working flag.
    /// </summary>
    /// <returns>The default flag, or <see langword="null" /> when unset.</returns>
    internal bool? DefaultNonWorkingDay =>
        this._defaultNonWorkingDay;

    /// <summary>
    /// Gets the concept-level tags.
    /// </summary>
    /// <returns>The tags; empty when none are configured.</returns>
    internal IReadOnlyList<string> Tags =>
        this._tags;

    /// <summary>
    /// Gets the rules belonging to the concept.
    /// </summary>
    /// <returns>The rule builders, in declaration order.</returns>
    internal IReadOnlyList<NotableDateRuleBuilder> Rules =>
        this._rules;

    /// <summary>
    /// Sets the human-readable display name of the concept.
    /// </summary>
    /// <param name="displayName">The display name.</param>
    /// <returns>The same <see cref="NotableDateDefinitionBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="displayName" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    public NotableDateDefinitionBuilder WithDisplayName(string displayName)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(displayName);

        this._displayName = displayName;
        return this;
    }

    /// <summary>
    /// Sets the category of the concept.
    /// </summary>
    /// <param name="category">The category.</param>
    /// <returns>The same <see cref="NotableDateDefinitionBuilder" /> instance, enabling chained calls.</returns>
    public NotableDateDefinitionBuilder WithCategory(NotableDateCategory category)
    {
        this._category = category;
        return this;
    }

    /// <summary>
    /// Sets the default duration in days for occurrences of the concept.
    /// </summary>
    /// <param name="days">The default duration in days.</param>
    /// <returns>The same <see cref="NotableDateDefinitionBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="days" /> is less than 1.</exception>
    public NotableDateDefinitionBuilder WithDefaultDurationDays(int days)
    {
        ThrowHelper.ThrowIfLessThan(days, 1);

        this._defaultDurationDays = days;
        return this;
    }

    /// <summary>
    /// Sets whether occurrences of the concept are non-working days by default.
    /// </summary>
    /// <param name="value">The default non-working flag.</param>
    /// <returns>The same <see cref="NotableDateDefinitionBuilder" /> instance, enabling chained calls.</returns>
    public NotableDateDefinitionBuilder AsNonWorkingByDefault(bool value = true)
    {
        this._defaultNonWorkingDay = value;
        return this;
    }

    /// <summary>
    /// Adds a single tag to the concept.
    /// </summary>
    /// <param name="tag">The tag value.</param>
    /// <returns>The same <see cref="NotableDateDefinitionBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="tag" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    public NotableDateDefinitionBuilder AddTag(string tag)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(tag);

        this._tags.Add(tag);
        return this;
    }

    /// <summary>
    /// Replaces the concept's tags with the supplied collection.
    /// </summary>
    /// <param name="tags">The tag values.</param>
    /// <returns>The same <see cref="NotableDateDefinitionBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="tags" /> is <see langword="null" />.</exception>
    public NotableDateDefinitionBuilder WithTags(params string[] tags)
    {
        ThrowHelper.ThrowIfNull(tags);

        this._tags.Clear();
        this._tags.AddRange(tags);
        return this;
    }

    /// <summary>
    /// Adds a rule to the concept and configures it through the supplied delegate.
    /// </summary>
    /// <param name="id">The stable identifier of the rule within the concept.</param>
    /// <param name="configure">A delegate that configures the rule.</param>
    /// <returns>The same <see cref="NotableDateDefinitionBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="id" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="configure" /> is <see langword="null" />.</exception>
    public NotableDateDefinitionBuilder AddRule(string id, Action<NotableDateRuleBuilder> configure)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(id);
        ThrowHelper.ThrowIfNull(configure);

        NotableDateRuleBuilder rule = new(id);
        configure(rule);
        this._rules.Add(rule);
        return this;
    }

    /// <summary>
    /// Adds a pre-built rule builder when reconstructing a concept from a parsed document.
    /// </summary>
    /// <param name="rule">The rule builder to add.</param>
    internal void AddRule(NotableDateRuleBuilder rule) =>
        this._rules.Add(rule);

    /// <summary>
    /// Sets the concept scalars and tags directly when reconstructing a builder from a parsed document.
    /// </summary>
    /// <param name="defaultDurationDays">The default duration in days, or <see langword="null" />.</param>
    /// <param name="defaultNonWorkingDay">The default non-working flag, or <see langword="null" />.</param>
    /// <param name="tags">The concept-level tags.</param>
    internal void SetParsedValues(int? defaultDurationDays, bool? defaultNonWorkingDay, IEnumerable<string> tags)
    {
        this._defaultDurationDays = defaultDurationDays;
        this._defaultNonWorkingDay = defaultNonWorkingDay;
        this._tags.Clear();
        this._tags.AddRange(tags);
    }

    /// <summary>
    /// Creates a deep copy of this concept builder.
    /// </summary>
    /// <returns>A new <see cref="NotableDateDefinitionBuilder" /> carrying the same configured state.</returns>
    internal NotableDateDefinitionBuilder Clone()
    {
        NotableDateDefinitionBuilder clone = new(this._id, this._displayName, this._category);
        clone.SetParsedValues(this._defaultDurationDays, this._defaultNonWorkingDay, this._tags);
        foreach (NotableDateRuleBuilder rule in this._rules)
            clone._rules.Add(rule.Clone());

        return clone;
    }
}

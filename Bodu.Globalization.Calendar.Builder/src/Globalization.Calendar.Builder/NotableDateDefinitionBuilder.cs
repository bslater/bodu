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
/// <remarks>
/// <para>
/// A concept groups the rules that express the same notable date across territories or eras — for example a national
/// rule and a regional variant that calculate the same holiday differently. Set concept-level defaults (category,
/// duration, non-working) once, then add each variant with
/// <see cref="AddRule(string, System.Action{NotableDateRuleBuilder})" />; a rule may override the defaults it needs to.
/// Configure the builder inside
/// <see cref="NotableDateDocumentBuilder.AddNotableDate(string, string, NotableDateCategory, System.Action{NotableDateDefinitionBuilder})" />
/// .
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// document.AddNotableDate("anzac-day", "Anzac Day", NotableDateCategory.PublicHoliday, c => c
///     .AsNonWorkingByDefault()
///     .AddTag("Remembrance")
///     .AddRule("au", r => r.ForTerritory("AU").Fixed(4, 25))
///     .AddRule("nz", r => r.ForTerritory("NZ").Fixed(4, 25)));
///]]>
/// </code>
/// </example>
/// <seealso cref="NotableDateDocumentBuilder" /> <seealso cref="NotableDateRuleBuilder" />
/// <seealso href="../guides/calendar/rule-authoring.html">Authoring notable date rules (guide)</seealso>
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
        _id = id;
        _displayName = displayName;
        _category = category;
    }

    /// <summary>
    /// Gets the stable identifier of the concept.
    /// </summary>
    /// <returns>The concept identifier.</returns>
    internal string Id =>
        _id;

    /// <summary>
    /// Gets the human-readable display name of the concept.
    /// </summary>
    /// <returns>The display name.</returns>
    internal string DisplayName =>
        _displayName;

    /// <summary>
    /// Gets the category of the concept.
    /// </summary>
    /// <returns>The category.</returns>
    internal NotableDateCategory Category =>
        _category;

    /// <summary>
    /// Gets the default duration in days.
    /// </summary>
    /// <returns>The default duration, or <see langword="null" /> when unset.</returns>
    internal int? DefaultDurationDays =>
        _defaultDurationDays;

    /// <summary>
    /// Gets the default non-working flag.
    /// </summary>
    /// <returns>The default flag, or <see langword="null" /> when unset.</returns>
    internal bool? DefaultNonWorkingDay =>
        _defaultNonWorkingDay;

    /// <summary>
    /// Gets the concept-level tags.
    /// </summary>
    /// <returns>The tags; empty when none are configured.</returns>
    internal IReadOnlyList<string> Tags =>
        _tags;

    /// <summary>
    /// Gets the rules belonging to the concept.
    /// </summary>
    /// <returns>The rule builders, in declaration order.</returns>
    internal IReadOnlyList<NotableDateRuleBuilder> Rules =>
        _rules;

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

        _displayName = displayName;
        return this;
    }

    /// <summary>
    /// Sets the category of the concept.
    /// </summary>
    /// <param name="category">The category.</param>
    /// <returns>The same <see cref="NotableDateDefinitionBuilder" /> instance, enabling chained calls.</returns>
    public NotableDateDefinitionBuilder WithCategory(NotableDateCategory category)
    {
        _category = category;
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

        _defaultDurationDays = days;
        return this;
    }

    /// <summary>
    /// Sets whether occurrences of the concept are non-working days by default.
    /// </summary>
    /// <param name="value">The default non-working flag.</param>
    /// <returns>The same <see cref="NotableDateDefinitionBuilder" /> instance, enabling chained calls.</returns>
    public NotableDateDefinitionBuilder AsNonWorkingByDefault(bool value = true)
    {
        _defaultNonWorkingDay = value;
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

        _tags.Add(tag);
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

        _tags.Clear();
        _tags.AddRange(tags);
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
        _rules.Add(rule);
        return this;
    }

    /// <summary>
    /// Adds a pre-built rule builder when reconstructing a concept from a parsed document.
    /// </summary>
    /// <param name="rule">The rule builder to add.</param>
    internal void AddRule(NotableDateRuleBuilder rule) =>
        _rules.Add(rule);

    /// <summary>
    /// Sets the concept scalars and tags directly when reconstructing a builder from a parsed document.
    /// </summary>
    /// <param name="defaultDurationDays">The default duration in days, or <see langword="null" />.</param>
    /// <param name="defaultNonWorkingDay">The default non-working flag, or <see langword="null" />.</param>
    /// <param name="tags">The concept-level tags.</param>
    internal void SetParsedValues(int? defaultDurationDays, bool? defaultNonWorkingDay, IEnumerable<string> tags)
    {
        _defaultDurationDays = defaultDurationDays;
        _defaultNonWorkingDay = defaultNonWorkingDay;
        _tags.Clear();
        _tags.AddRange(tags);
    }

    /// <summary>
    /// Creates a deep copy of this concept builder.
    /// </summary>
    /// <returns>A new <see cref="NotableDateDefinitionBuilder" /> carrying the same configured state.</returns>
    internal NotableDateDefinitionBuilder Clone()
    {
        NotableDateDefinitionBuilder clone = new(_id, _displayName, _category);
        clone.SetParsedValues(_defaultDurationDays, _defaultNonWorkingDay, _tags);
        foreach (NotableDateRuleBuilder rule in _rules)
            clone._rules.Add(rule.Clone());

        return clone;
    }
}

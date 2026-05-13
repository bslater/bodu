// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateBuilder.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides a fluent interface for constructing a single named notable date entry — comprising one or more
/// <see cref="NotableDateRule" /> instances — for inclusion in a <see cref="NotableDateDocumentBuilder" />.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="NotableDateBuilder" /> is obtained via
/// <see cref="NotableDateDocumentBuilder.AddDate(string, System.Action{NotableDateBuilder})" />. Each call to
/// <see cref="AddRule(System.Action{NotableDateRuleBuilder})" /> appends one resolution rule to the entry. At least one rule must be
/// added before the document can be built or serialised.
/// </para>
/// <para>
/// In the <c>NotableDates</c> XML vocabulary a single <c>&lt;NotableDate&gt;</c> element may carry multiple <c>&lt;Rule&gt;</c>
/// children — for example, to express era splits, regional variants, or alternate calendar systems. Each rule is modelled by a
/// separate <see cref="NotableDateRuleBuilder" />.
/// </para>
/// </remarks>
public sealed class NotableDateBuilder
{
    /// <summary>The list of rule builders accumulated by <see cref="AddRule(System.Action{NotableDateRuleBuilder})" />.</summary>
    private readonly List<NotableDateRuleBuilder> _rules = [];

    /// <summary>
    /// Adds a resolution rule to this notable date entry.
    /// </summary>
    /// <param name="configure">
    /// A callback that configures the <see cref="NotableDateRuleBuilder" /> for this rule. Must not be <see langword="null" />.
    /// </param>
    /// <returns>This builder instance, for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure" /> is <see langword="null" />.</exception>
    public NotableDateBuilder AddRule(Action<NotableDateRuleBuilder> configure)
    {
        ThrowHelper.ThrowIfNull(configure);

        NotableDateRuleBuilder ruleBuilder = new();
        configure(ruleBuilder);
        _rules.Add(ruleBuilder);
        return this;
    }

    /// <summary>
    /// Builds the <see cref="NotableDateRule" /> instances for this entry.
    /// </summary>
    /// <param name="notableDateName">The canonical name shared by all rules in this entry.</param>
    /// <returns>A sequence of constructed rules.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no rules have been added to this entry.</exception>
    internal IEnumerable<NotableDateRule> Build(string notableDateName)
    {
        if (_rules.Count == 0)
            throw new InvalidOperationException($"At least one rule must be added to the notable date entry '{notableDateName}'.");

        return _rules.Select(r => r.Build(notableDateName));
    }

    /// <summary>
    /// Builds a schema-valid <c>&lt;NotableDate&gt;</c> <see cref="XElement" /> from the current builder state.
    /// </summary>
    /// <param name="notableDateName">The canonical name for the <c>name</c> attribute.</param>
    /// <param name="ns">The XML namespace for the <c>NotableDates</c> schema.</param>
    /// <returns>The constructed <see cref="XElement" />.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no rules have been added to this entry.</exception>
    internal XElement ToXElement(string notableDateName, XNamespace ns)
    {
        if (_rules.Count == 0)
            throw new InvalidOperationException($"At least one rule must be added to the notable date entry '{notableDateName}'.");

        XElement element = new(ns + "NotableDate", new XAttribute("name", notableDateName));

        foreach (NotableDateRuleBuilder rule in _rules)
            element.Add(rule.ToXElement(notableDateName, ns));

        return element;
    }

    /// <summary>
    /// Builds a schema-valid <c>notableDate</c> <see cref="JsonObject" /> from the current builder state.
    /// </summary>
    /// <param name="notableDateName">The canonical name for the <c>name</c> property.</param>
    /// <returns>The constructed <see cref="JsonObject" /> conforming to the notable-date entry of <c>NotableDates.schema.json</c>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no rules have been added to this entry.</exception>
    internal JsonObject ToJsonNode(string notableDateName)
    {
        if (_rules.Count == 0)
            throw new InvalidOperationException($"At least one rule must be added to the notable date entry '{notableDateName}'.");

        JsonArray rules = [];

        foreach (NotableDateRuleBuilder rule in _rules)
            rules.Add(rule.ToJsonNode(notableDateName));

        return new JsonObject
        {
            ["name"] = notableDateName,
            ["rules"] = rules,
        };
    }

    /// <summary>
    /// Creates an independent copy of this builder, deep-cloning every authored rule so that subsequent
    /// mutations of either builder do not bleed across the boundary.
    /// </summary>
    /// <returns>A new <see cref="NotableDateBuilder" /> with the same rule set as this instance.</returns>
    public NotableDateBuilder Clone()
    {
        NotableDateBuilder copy = new();
        foreach (NotableDateRuleBuilder rule in _rules)
            copy._rules.Add(rule.Clone());
        return copy;
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="InlineNotableDateRuleProvider.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Supplies <see cref="NotableDateRule" /> instances to a <see cref="NotableDateService" /> from an in-memory collection
/// produced by a <see cref="NotableDateDocumentBuilder" />.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="InlineNotableDateRuleProvider" /> implements <see cref="INotableDateRuleProvider" /> by wrapping a pre-built list of
/// rules. It is the return type of <see cref="NotableDateDocumentBuilder.ToProvider" /> and is suitable for use wherever an
/// <see cref="INotableDateRuleProvider" /> is accepted, including the <see cref="NotableDateService" /> constructor.
/// </para>
/// <para>
/// Unlike <see cref="XmlResourceNotableDateRuleProvider" />, this implementation performs no I/O and does not defer rule
/// loading — the supplied rules are returned directly from <see cref="LoadRules" />.
/// </para>
/// </remarks>
/// <example>
/// <para>Constructing a service from a programmatically built rule set:</para>
/// <code>
/// INotableDateRuleProvider provider = NotableDateDocumentBuilder.Create()
///     .AddDate("Christmas Day", date => date
///         .AddRule(rule => rule
///             .Category(NotableDateCategory.Holiday)
///             .NonWorking()
///             .Fixed(12, 25)))
///     .ToProvider();
///
/// NotableDateService service = new(new[] { provider });
/// </code>
/// </example>
public sealed class InlineNotableDateRuleProvider : INotableDateRuleProvider
{
    private readonly IReadOnlyList<NotableDateRule> _rules;

    /// <summary>
    /// Initialises a new <see cref="InlineNotableDateRuleProvider" /> with the specified rules.
    /// </summary>
    /// <param name="rules">The rules to expose. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rules" /> is <see langword="null" />.</exception>
    public InlineNotableDateRuleProvider(IReadOnlyList<NotableDateRule> rules)
    {
        ThrowHelper.ThrowIfNull(rules);
        _rules = rules;
    }

    /// <summary>
    /// Returns all <see cref="NotableDateRule" /> instances supplied at construction time.
    /// </summary>
    /// <returns>The rules in the order they were added to the original <see cref="NotableDateDocumentBuilder" />.</returns>
    public IEnumerable<NotableDateRule> LoadRules() => _rules;
}

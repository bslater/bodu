// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateFilter.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Represents a composable predicate applied in two stages when querying <see cref="NotableDate" /> instances from an
/// <see cref="INotableDateService" />: a primary gate evaluated against the originating <see cref="NotableDateRule" />
/// before the date is resolved, and a secondary gate evaluated against the materialized <see cref="NotableDate" />.
/// </summary>
/// <remarks>
/// <para>
/// Filters are created via the static factory methods on this class — for example <see cref="ForCategory" />,
/// <see cref="WithTag" />, <see cref="InDateRange" /> — and combined through <see cref="And" />, <see cref="Or" />,
/// <see cref="AllOf" />, and <see cref="AnyOf" />. Every instance is immutable; composition always produces a new
/// <see cref="NotableDateFilter" />.
/// </para>
/// <para>
/// <b>Primary gate (rule-level)</b>: Predicates backed by <see cref="NotableDateRule" /> metadata that the service
/// evaluates before resolving the date. When a rule fails the primary gate the service skips the date-resolution step
/// entirely, avoiding the cost of algorithm invocation, calendar conversion, and observance-adjustment evaluation for
/// rules that cannot produce matching dates. Filters whose criteria are fully known from the rule — such as
/// <see cref="ForCategory" />, <see cref="WithTag" />, <see cref="WithName" />, and <see cref="IsNonWorkingDay" /> —
/// are deterministic at this stage and maximize the optimization.
/// </para>
/// <para>
/// <b>Secondary gate (date-level)</b>: Predicates that require the materialized <see cref="NotableDate" /> — such as
/// <see cref="InDateRange" /> and <see cref="WasAdjusted" /> — are evaluated after the date is resolved. These
/// predicates always allow the rule to pass the primary gate; all filtering occurs on the resolved date.
/// </para>
/// <para>
/// <b>Composition behavior</b>: When filters are combined with <see cref="And" />, a rule is skipped as soon as any
/// primary-capable clause evaluates to <see langword="false" />. When combined with <see cref="Or" />, a rule is
/// skipped only if every branch's primary gate returns <see langword="false" />.
/// </para>
/// <para>
/// <b>Caching</b>: Filtered queries bypass the per-year cache maintained by <see cref="NotableDateService" /> so that
/// unfiltered queries continue to receive complete, correctly cached results. The primary gate optimization therefore
/// applies on every filtered call rather than only on the first (cold) access.
/// </para>
/// </remarks>
/// <example>
/// <para>
/// Compose filters and query notable dates from a service:
/// </para>
/// <code>
///<![CDATA[
/// INotableDateService service = new NotableDateService();
///
/// Non-working public holidays for New South Wales in 2026:
/// NotableDateFilter filter = NotableDateFilter
///     .ForCategory(NotableDateCategory.Public)
///     .And(NotableDateFilter.IsNonWorkingDay());
///
/// IReadOnlyList<NotableDate> holidays = service.GetNotableDates(2026, filter, "AU-NSW");
///
/// Public or cultural dates tagged "Christian":
/// NotableDateFilter christian = NotableDateFilter
///     .ForAnyCategory(NotableDateCategory.Public, NotableDateCategory.Cultural)
///     .And(NotableDateFilter.WithTag("Christian"));
///
/// Dates whose span intersects Easter week 2026:
/// NotableDateFilter easterWeek = NotableDateFilter.InDateRange(
///     new DateTime(2026, 4, 5),
///     new DateTime(2026, 4, 12));
///
/// Combine multiple constraints using AllOf:
/// NotableDateFilter combined = NotableDateFilter.AllOf(
///     NotableDateFilter.ForCategory(NotableDateCategory.Public),
///     NotableDateFilter.IsNonWorkingDay(),
///     NotableDateFilter.WithName("Christmas Day"));
///]]>
/// </code>
/// </example>
public sealed class NotableDateFilter
{
    /// <summary>
    /// The secondary gate predicate evaluated against each materialized <see cref="NotableDate" />.
    /// </summary>
    private readonly Func<NotableDate, bool> _dateGate;

    /// <summary>
    /// The primary gate predicate evaluated against each <see cref="NotableDateRule" /> before date resolution.
    /// </summary>
    private readonly Func<NotableDateRule, bool> _ruleGate;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateFilter" /> class with explicit primary and secondary
    /// gate delegates.
    /// </summary>
    /// <param name="ruleGate">The primary gate predicate evaluated against each rule before date resolution.</param>
    /// <param name="dateGate">The secondary gate predicate evaluated against each materialized date.</param>
    private NotableDateFilter(Func<NotableDateRule, bool> ruleGate, Func<NotableDate, bool> dateGate)
    {
        _ruleGate = ruleGate;
        _dateGate = dateGate;
    }

    /// <summary>
    /// Returns a new filter that passes only when every filter in <paramref name="filters" /> passes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Equivalent to chaining <see cref="And" /> across all supplied filters.
    /// </para>
    /// </remarks>
    /// <param name="filters">The filters to combine. Must not be <see langword="null" /> or empty.</param>
    /// <returns>A new <see cref="NotableDateFilter" /> representing the logical AND of all supplied filters.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="filters" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="filters" /> contains no elements.</exception>
    public static NotableDateFilter AllOf(params NotableDateFilter[] filters)
    {
        ThrowHelper.ThrowIfCollectionTooSmall(filters, 1);

        return filters.Aggregate((acc, next) => acc.And(next));
    }

    /// <summary>
    /// Returns a new filter that passes when at least one filter in <paramref name="filters" /> passes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Equivalent to chaining <see cref="Or" /> across all supplied filters.
    /// </para>
    /// </remarks>
    /// <param name="filters">The filters to combine. Must not be <see langword="null" /> or empty.</param>
    /// <returns>A new <see cref="NotableDateFilter" /> representing the logical OR of all supplied filters.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="filters" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="filters" /> contains no elements.</exception>
    public static NotableDateFilter AnyOf(params NotableDateFilter[] filters)
    {
        ThrowHelper.ThrowIfCollectionTooSmall(filters, 1);

        return filters.Aggregate((acc, next) => acc.Or(next));
    }

    /// <summary>
    /// Returns a filter that matches notable dates whose <see cref="NotableDate.Category" /> is one of the supplied
    /// <paramref name="categories" />.
    /// </summary>
    /// <param name="categories">The accepted categories. Must not be <see langword="null" /> or empty.</param>
    /// <returns>A new <see cref="NotableDateFilter" /> that matches any of the specified categories.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="categories" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="categories" /> contains no elements.</exception>
    public static NotableDateFilter ForAnyCategory(params NotableDateCategory[] categories)
    {
        ThrowHelper.ThrowIfCollectionTooSmall(categories, 1);

        HashSet<NotableDateCategory> set = [.. categories];
        return new(
            rule => set.Contains(rule.Category),
            date => set.Contains(date.Category));
    }

    // --------------------------------------------------------------------------------------
    // Rule-level (primary) factories
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Returns a filter that matches notable dates whose <see cref="NotableDate.Category" /> equals
    /// <paramref name="category" />.
    /// </summary>
    /// <param name="category">The required category.</param>
    /// <returns>A new <see cref="NotableDateFilter" /> that matches the specified category.</returns>
    public static NotableDateFilter ForCategory(NotableDateCategory category) => new(
            rule => rule.Category == category,
            date => date.Category == category);

    // --------------------------------------------------------------------------------------
    // Date-level (secondary) factories
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Returns a filter that matches notable dates whose span intersects the inclusive date range [
    /// <paramref name="startDate" />, <paramref name="endDate" />].
    /// </summary>
    /// <remarks>
    /// <para>
    /// This filter operates only at the date level; every rule passes the primary gate. A multi-day notable date is
    /// included when any day of its span falls within the supplied range.
    /// </para>
    /// </remarks>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">
    /// The inclusive end of the range. Must not be earlier than <paramref name="startDate" />.
    /// </param>
    /// <returns>A new <see cref="NotableDateFilter" /> that matches dates intersecting the range.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="endDate" /> is earlier than <paramref name="startDate" />.
    /// </exception>
    public static NotableDateFilter InDateRange(DateTime startDate, DateTime endDate)
    {
        ThrowHelper.ThrowIfLessThan(endDate, startDate);

        DateTime start = startDate.Date;
        DateTime end = endDate.Date;

        return new(
            _ => true,
            date => date.Date.Date <= end && date.EndDate.Date >= start);
    }

    /// <summary>
    /// Returns a filter that matches notable dates flagged as non-working days (
    /// <see cref="NotableDate.IsNonWorkingDay" /> is <see langword="true" />).
    /// </summary>
    /// <remarks>
    /// <para>
    /// At the rule level the primary gate passes rules whose <see cref="NotableDateRule.IsNonWorkingDay" /> is
    /// <see langword="true" /> or whose <see cref="NotableDateRule.Adjustments" /> include at least one entry that
    /// explicitly sets the non-working flag or delegates to a custom handler. Rules with no mechanism for producing a
    /// non-working date are skipped before resolution.
    /// </para>
    /// </remarks>
    /// <returns>A new <see cref="NotableDateFilter" /> that matches only non-working dates.</returns>
    public static NotableDateFilter IsNonWorkingDay() => new(
            rule => rule.IsNonWorkingDay == true
                || rule.Adjustments
        .Any(a => a.IsNonWorkingDay == true
                    || a.Action == AdjustmentAction.Custom
                    || a.Trigger == AdjustmentTrigger.Custom),
            date => date.IsNonWorkingDay);

    /// <summary>
    /// Returns a filter that matches only notable dates that were shifted from their originally calculated position by
    /// a triggered <see cref="ObservanceAdjustment" /> (<see cref="NotableDate.WasAdjusted" /> is
    /// <see langword="true" />).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This filter operates only at the date level; every rule passes the primary gate because the outcome of
    /// adjustment evaluation is not known until after the date is resolved.
    /// </para>
    /// </remarks>
    /// <returns>A new <see cref="NotableDateFilter" /> that matches only adjusted dates.</returns>
    public static NotableDateFilter WasAdjusted() => new(
            _ => true,
            date => date.WasAdjusted);

    /// <summary>
    /// Returns a filter that matches notable dates whose <see cref="NotableDate.Tags" /> collection contains every one
    /// of the supplied <paramref name="tags" /> (case-insensitive).
    /// </summary>
    /// <param name="tags">The required tags. Must not be <see langword="null" /> or empty.</param>
    /// <returns>A new <see cref="NotableDateFilter" /> that matches all of the specified tags.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="tags" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tags" /> contains no elements.</exception>
    public static NotableDateFilter WithAllTags(params string[] tags)
    {
        ThrowHelper.ThrowIfCollectionTooSmall(tags, 1);

        HashSet<string> required = new(tags, StringComparer.OrdinalIgnoreCase);
        return new(
            rule => required.All(t => rule.Tags.Any(rt => string.Equals(rt, t, StringComparison.OrdinalIgnoreCase))),
            date => required.All(t => date.Tags.Any(dt => string.Equals(dt, t, StringComparison.OrdinalIgnoreCase))));
    }

    /// <summary>
    /// Returns a filter that matches notable dates whose <see cref="NotableDate.Name" /> is one of the supplied
    /// <paramref name="names" /> (case-insensitive).
    /// </summary>
    /// <param name="names">The accepted names. Must not be <see langword="null" /> or empty.</param>
    /// <returns>A new <see cref="NotableDateFilter" /> that matches any of the specified names.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="names" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="names" /> contains no elements.</exception>
    public static NotableDateFilter WithAnyName(params string[] names)
    {
        ThrowHelper.ThrowIfCollectionTooSmall(names, 1);

        HashSet<string> set = new(names, StringComparer.OrdinalIgnoreCase);
        return new(
            rule => set.Contains(rule.Name),
            date => set.Contains(date.Name));
    }

    /// <summary>
    /// Returns a filter that matches notable dates whose <see cref="NotableDate.Tags" /> collection contains at least
    /// one of the supplied <paramref name="tags" /> (case-insensitive).
    /// </summary>
    /// <param name="tags">The accepted tags. Must not be <see langword="null" /> or empty.</param>
    /// <returns>A new <see cref="NotableDateFilter" /> that matches any of the specified tags.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="tags" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tags" /> contains no elements.</exception>
    public static NotableDateFilter WithAnyTag(params string[] tags)
    {
        ThrowHelper.ThrowIfCollectionTooSmall(tags, 1);

        HashSet<string> set = new(tags, StringComparer.OrdinalIgnoreCase);
        return new(
            rule => rule.Tags.Any(t => set.Contains(t)),
            date => date.Tags.Any(t => set.Contains(t)));
    }

    /// <summary>
    /// Returns a filter that matches notable dates whose <see cref="NotableDate.DurationDays" /> is at least
    /// <paramref name="minimumDays" />.
    /// </summary>
    /// <param name="minimumDays">The minimum duration in days. Must be at least one.</param>
    /// <returns>
    /// A new <see cref="NotableDateFilter" /> that matches dates spanning at least the minimum duration.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="minimumDays" /> is less than one.
    /// </exception>
    public static NotableDateFilter WithMinDuration(int minimumDays)
    {
        ThrowHelper.ThrowIfLessThan(minimumDays, 1);

        return new(
            rule => rule.DurationDays >= minimumDays,
            date => date.DurationDays >= minimumDays);
    }

    /// <summary>
    /// Returns a filter that matches notable dates whose <see cref="NotableDate.Name" /> equals
    /// <paramref name="name" /> (case-insensitive).
    /// </summary>
    /// <param name="name">The required name. Must not be <see langword="null" />, empty, or whitespace.</param>
    /// <returns>A new <see cref="NotableDateFilter" /> that matches the specified name.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name" /> is empty or whitespace.</exception>
    public static NotableDateFilter WithName(string name)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(name);

        return new(
            rule => string.Equals(rule.Name, name, StringComparison.OrdinalIgnoreCase),
            date => string.Equals(date.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Returns a filter that matches notable dates whose <see cref="NotableDate.Tags" /> collection contains
    /// <paramref name="tag" /> (case-insensitive).
    /// </summary>
    /// <param name="tag">The required tag. Must not be <see langword="null" />, empty, or whitespace.</param>
    /// <returns>A new <see cref="NotableDateFilter" /> that matches the specified tag.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="tag" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tag" /> is empty or whitespace.</exception>
    public static NotableDateFilter WithTag(string tag)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(tag);

        return new(
            rule => rule.Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)),
            date => date.Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)));
    }

    // --------------------------------------------------------------------------------------
    // Composition
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Returns a new filter that passes only when both this filter and <paramref name="other" /> pass.
    /// </summary>
    /// <remarks>
    /// <para>
    /// At the rule level both primary gates are evaluated with short-circuit semantics: if this filter's gate returns
    /// <see langword="false" /> the other filter's gate is not evaluated and the rule is skipped immediately.
    /// </para>
    /// </remarks>
    /// <param name="other">The filter to combine with this filter. Must not be <see langword="null" />.</param>
    /// <returns>A new <see cref="NotableDateFilter" /> representing the logical AND of the two filters.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="other" /> is <see langword="null" />.
    /// </exception>
    public NotableDateFilter And(NotableDateFilter other)
    {
        ThrowHelper.ThrowIfNull(other);

        return new(
            rule => _ruleGate(rule) && other._ruleGate(rule),
            date => _dateGate(date) && other._dateGate(date));
    }

    /// <summary>
    /// Returns a new filter that passes when at least one of this filter or <paramref name="other" /> passes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// At the rule level the combined primary gate returns <see langword="true" /> as soon as either branch's primary
    /// gate returns <see langword="true" />. A rule is only skipped when every branch's gate returns
    /// <see langword="false" />.
    /// </para>
    /// </remarks>
    /// <param name="other">The filter to combine with this filter. Must not be <see langword="null" />.</param>
    /// <returns>A new <see cref="NotableDateFilter" /> representing the logical OR of the two filters.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="other" /> is <see langword="null" />.
    /// </exception>
    public NotableDateFilter Or(NotableDateFilter other)
    {
        ThrowHelper.ThrowIfNull(other);

        return new(
            rule => _ruleGate(rule) || other._ruleGate(rule),
            date => _dateGate(date) || other._dateGate(date));
    }

    /// <summary>
    /// Evaluates the secondary gate against the materialized <paramref name="date" />.
    /// </summary>
    /// <param name="date">The resolved notable date to evaluate.</param>
    /// <returns>
    /// <see langword="true" /> if the date satisfies all filter criteria; otherwise <see langword="false" />.
    /// </returns>
    internal bool IsMatch(NotableDate date) => _dateGate(date);

    // --------------------------------------------------------------------------------------
    // Internal evaluation
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Evaluates the primary gate against <paramref name="rule" />.
    /// </summary>
    /// <param name="rule">The rule to evaluate.</param>
    /// <returns>
    /// <see langword="false" /> if the rule is definitively excluded and date resolution can be skipped entirely;
    /// <see langword="true" /> if the rule may produce dates that pass the secondary gate.
    /// </returns>
    internal bool IsRuleEligible(NotableDateRule rule) => _ruleGate(rule);
}

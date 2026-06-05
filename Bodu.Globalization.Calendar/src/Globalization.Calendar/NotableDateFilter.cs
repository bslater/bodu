// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateFilter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Represents a composable predicate over resolved <see cref="NotableDate" /> occurrences, used to restrict the results
/// returned by <see cref="INotableDateService" /> to those matching a category, tag, name, duration, or observed-state
/// criterion. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Construction.</strong> A filter is created through the static factory methods on this class — for example
/// <see cref="ForCategory(NotableDateCategory)" />, <see cref="WithTag(string)" />,
/// <see cref="IsNonWorkingDay()" />, and <see cref="InDateRange(DateOnly, DateOnly)" />. There is no public
/// constructor; every factory returns an immutable instance.
/// </para>
/// <para>
/// <strong>Composition.</strong> Filters combine through the instance operators <see cref="And(NotableDateFilter)" />,
/// <see cref="Or(NotableDateFilter)" />, and <see cref="Not()" />, and the variadic factories
/// <see cref="AllOf(NotableDateFilter[])" /> and <see cref="AnyOf(NotableDateFilter[])" />. Because every instance is
/// immutable, composition always produces a new filter and never mutates an operand, so a filter can be cached and
/// reused across queries and threads.
/// </para>
/// <para>
/// <strong>Evaluation.</strong> A filter is evaluated against each materialized occurrence after resolution: pass a
/// filter to one of the <c>Resolve</c> overloads on <see cref="INotableDateService" /> (which keeps only the
/// occurrences for which <see cref="Matches(NotableDate)" /> returns <see langword="true" />), or call
/// <see cref="Matches(NotableDate)" /> directly to test a single occurrence.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// INotableDateService service = AmericasCalendarData.CreateService("US");
/// DateRange year = new(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
///
/// // Non-working public holidays only.
/// NotableDateFilter publicHolidays = NotableDateFilter
///     .ForCategory(NotableDateCategory.PublicHoliday)
///     .And(NotableDateFilter.IsNonWorkingDay());
///
/// IReadOnlyList<NotableDate> holidays = service.Resolve(year, "US", publicHolidays);
///
/// // Religious or cultural dates tagged "Christian", excluding observed substitutes.
/// NotableDateFilter christian = NotableDateFilter
///     .ForAnyCategory(NotableDateCategory.Religious, NotableDateCategory.Cultural)
///     .And(NotableDateFilter.WithTag("Christian"))
///     .And(NotableDateFilter.WasAdjusted().Not());
///
/// IReadOnlyList<NotableDate> observances = service.Resolve(year, "US", christian);
///]]>
/// </code>
/// </example>
/// <seealso cref="NotableDate" />
/// <seealso cref="INotableDateService" />
/// <seealso cref="NotableDateCategory" />
/// <seealso href="../guides/calendar/notable-dates.html">Using NotableDateService (guide)</seealso>
public sealed class NotableDateFilter
{
    /// <summary>
    /// The underlying predicate evaluated against a resolved occurrence.
    /// </summary>
    private readonly Func<NotableDate, bool> _predicate;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateFilter" /> class.
    /// </summary>
    /// <param name="predicate">The predicate evaluated against a resolved occurrence.</param>
    /// <exception cref="ArgumentNullException"><paramref name="predicate" /> is <see langword="null" />.</exception>
    private NotableDateFilter(Func<NotableDate, bool> predicate)
    {
        ThrowHelper.ThrowIfNull(predicate);

        this._predicate = predicate;
    }

    /// <summary>
    /// Determines whether the supplied occurrence satisfies the filter.
    /// </summary>
    /// <param name="notableDate">The resolved occurrence to test.</param>
    /// <returns><see langword="true" /> if the occurrence matches; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="notableDate" /> is <see langword="null" />.</exception>
    public bool Matches(NotableDate notableDate)
    {
        ThrowHelper.ThrowIfNull(notableDate);

        return this._predicate(notableDate);
    }

    /// <summary>
    /// Creates a filter that matches occurrences of a specific category.
    /// </summary>
    /// <param name="category">The category to match.</param>
    /// <returns>A filter matching the category.</returns>
    public static NotableDateFilter ForCategory(NotableDateCategory category) =>
        new(n => n.Category == category);

    /// <summary>
    /// Creates a filter that matches occurrences belonging to any of the supplied categories.
    /// </summary>
    /// <param name="categories">The categories to match.</param>
    /// <returns>A filter matching any of the categories.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="categories" /> is <see langword="null" />.</exception>
    public static NotableDateFilter ForAnyCategory(params NotableDateCategory[] categories)
    {
        ThrowHelper.ThrowIfNull(categories);

        HashSet<NotableDateCategory> set = new(categories);
        return new NotableDateFilter(n => set.Contains(n.Category));
    }

    /// <summary>
    /// Creates a filter that matches occurrences flagged as non-working days.
    /// </summary>
    /// <returns>A filter matching non-working occurrences.</returns>
    public static NotableDateFilter IsNonWorkingDay() =>
        new(n => n.IsNonWorkingDay);

    /// <summary>
    /// Creates a filter that matches occurrences whose emitted date differs from the calculated date because an
    /// adjustment applied.
    /// </summary>
    /// <returns>A filter matching observed (adjusted) occurrences.</returns>
    public static NotableDateFilter WasAdjusted() =>
        new(n => n.IsObserved);

    /// <summary>
    /// Creates a filter that matches occurrences with a specific display name.
    /// </summary>
    /// <param name="displayName">The display name to match.</param>
    /// <returns>A filter matching the display name.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="displayName" /> is <see langword="null" />.</exception>
    public static NotableDateFilter WithName(string displayName)
    {
        ThrowHelper.ThrowIfNull(displayName);

        return new NotableDateFilter(n => string.Equals(n.DisplayName, displayName, StringComparison.Ordinal));
    }

    /// <summary>
    /// Creates a filter that matches occurrences whose display name equals any of the supplied names.
    /// </summary>
    /// <param name="displayNames">The display names to match.</param>
    /// <returns>A filter matching any of the names.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="displayNames" /> is <see langword="null" />.</exception>
    public static NotableDateFilter WithAnyName(params string[] displayNames)
    {
        ThrowHelper.ThrowIfNull(displayNames);

        HashSet<string> set = new(displayNames, StringComparer.Ordinal);
        return new NotableDateFilter(n => set.Contains(n.DisplayName));
    }

    /// <summary>
    /// Creates a filter that matches occurrences produced by a specific notable-date concept identifier.
    /// </summary>
    /// <param name="notableDateId">The notable-date concept identifier to match.</param>
    /// <returns>A filter matching the concept identifier.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="notableDateId" /> is <see langword="null" />.</exception>
    public static NotableDateFilter WithId(string notableDateId)
    {
        ThrowHelper.ThrowIfNull(notableDateId);

        return new NotableDateFilter(n => string.Equals(n.NotableDateId, notableDateId, StringComparison.Ordinal));
    }

    /// <summary>
    /// Creates a filter that matches occurrences carrying a specific tag.
    /// </summary>
    /// <param name="tag">The tag to match.</param>
    /// <returns>A filter matching occurrences with the tag.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="tag" /> is <see langword="null" />.</exception>
    public static NotableDateFilter WithTag(string tag)
    {
        ThrowHelper.ThrowIfNull(tag);

        return new NotableDateFilter(n => n.Tags.Contains(tag, StringComparer.Ordinal));
    }

    /// <summary>
    /// Creates a filter that matches occurrences carrying any of the supplied tags.
    /// </summary>
    /// <param name="tags">The tags to match.</param>
    /// <returns>A filter matching occurrences with any of the tags.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="tags" /> is <see langword="null" />.</exception>
    public static NotableDateFilter WithAnyTag(params string[] tags)
    {
        ThrowHelper.ThrowIfNull(tags);

        HashSet<string> set = new(tags, StringComparer.Ordinal);
        return new NotableDateFilter(n => n.Tags.Any(set.Contains));
    }

    /// <summary>
    /// Creates a filter that matches occurrences carrying all of the supplied tags.
    /// </summary>
    /// <param name="tags">The tags that must all be present.</param>
    /// <returns>A filter matching occurrences with every tag.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="tags" /> is <see langword="null" />.</exception>
    public static NotableDateFilter WithAllTags(params string[] tags)
    {
        ThrowHelper.ThrowIfNull(tags);

        string[] required = tags.ToArray();
        return new NotableDateFilter(n => required.All(t => n.Tags.Contains(t, StringComparer.Ordinal)));
    }

    /// <summary>
    /// Creates a filter that matches occurrences spanning at least the supplied number of days.
    /// </summary>
    /// <param name="days">The minimum inclusive duration in days.</param>
    /// <returns>A filter matching occurrences of at least the duration.</returns>
    public static NotableDateFilter WithMinDuration(int days) =>
        new(n => n.DurationDays >= days);

    /// <summary>
    /// Creates a filter that matches occurrences whose emitted date falls within an inclusive date range.
    /// </summary>
    /// <param name="startInclusive">The inclusive start date.</param>
    /// <param name="endInclusive">The inclusive end date.</param>
    /// <returns>A filter matching occurrences within the range.</returns>
    public static NotableDateFilter InDateRange(DateOnly startInclusive, DateOnly endInclusive) =>
        new(n => n.Date >= startInclusive && n.Date <= endInclusive);

    /// <summary>
    /// Creates a filter that matches an occurrence only when every supplied filter matches it.
    /// </summary>
    /// <param name="filters">The filters that must all match.</param>
    /// <returns>A filter matching the conjunction of the supplied filters.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="filters" /> is <see langword="null" />.</exception>
    public static NotableDateFilter AllOf(params NotableDateFilter[] filters)
    {
        ThrowHelper.ThrowIfNull(filters);

        NotableDateFilter[] all = filters.ToArray();
        return new NotableDateFilter(n => all.All(f => f._predicate(n)));
    }

    /// <summary>
    /// Creates a filter that matches an occurrence when any supplied filter matches it.
    /// </summary>
    /// <param name="filters">The filters, any of which may match.</param>
    /// <returns>A filter matching the disjunction of the supplied filters.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="filters" /> is <see langword="null" />.</exception>
    public static NotableDateFilter AnyOf(params NotableDateFilter[] filters)
    {
        ThrowHelper.ThrowIfNull(filters);

        NotableDateFilter[] any = filters.ToArray();
        return new NotableDateFilter(n => any.Any(f => f._predicate(n)));
    }

    /// <summary>
    /// Combines this filter with another so that both must match.
    /// </summary>
    /// <param name="other">The filter to combine with.</param>
    /// <returns>A filter matching the conjunction of the two filters.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    public NotableDateFilter And(NotableDateFilter other)
    {
        ThrowHelper.ThrowIfNull(other);

        return new NotableDateFilter(n => this._predicate(n) && other._predicate(n));
    }

    /// <summary>
    /// Combines this filter with another so that either may match.
    /// </summary>
    /// <param name="other">The filter to combine with.</param>
    /// <returns>A filter matching the disjunction of the two filters.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    public NotableDateFilter Or(NotableDateFilter other)
    {
        ThrowHelper.ThrowIfNull(other);

        return new NotableDateFilter(n => this._predicate(n) || other._predicate(n));
    }

    /// <summary>
    /// Negates this filter.
    /// </summary>
    /// <returns>A filter matching exactly the occurrences this filter does not.</returns>
    public NotableDateFilter Not() =>
        new(n => !this._predicate(n));
}

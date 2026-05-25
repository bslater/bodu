// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FilterScenarioKnownAnswer.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Represents a single known-answer test row for the <see cref="NotableDateFilter" /> integration with
/// <see cref="NotableDateService" />: a service fixture, a filter, the query year, and the set of holiday
/// names that should survive the filter.
/// </summary>
/// <remarks>
/// <para>
/// Covers the "year-and-filter -&gt; matching names" shape that the majority of filter tests in
/// <c>NotableDateServiceTests.Filter.cs</c> use. Tests that assert non-name-based contracts (the
/// <c>WasAdjusted</c> flag on the returned occurrence, exception parameter names, cache invalidation
/// behaviour, ordering guarantees) stay bespoke because they cannot be encoded as a name-set equivalence.
/// </para>
/// <para>
/// Each row's <see cref="ServiceFactory" /> and <see cref="FilterFactory" /> are invoked once per assertion
/// so test isolation is preserved between rows even when the underlying rule fixtures share names. The
/// assertion in <see cref="FilterScenarioAssertions.AssertResultNamesMatch" /> uses set equivalence
/// (<c>CollectionAssert.AreEquivalent</c>) so rows do not need to encode the canonical date ordering.
/// </para>
/// </remarks>
public sealed record FilterScenarioKnownAnswer
{
    /// <summary>
    /// Gets the short scenario label rendered by <see cref="FilterScenarioAssertions.GetDisplayName" /> as
    /// part of the MSTest row entry — for example <c>"CategoryFilter_HolidayOnly"</c> or
    /// <c>"AllOfFilter_Intersection"</c>.
    /// </summary>
    /// <returns>The scenario label.</returns>
    public required string Scenario { get; init; }

    /// <summary>
    /// Gets the delegate that constructs the fixture <see cref="NotableDateService" /> for this row.
    /// Invoked once per assertion so the rule set is materialised fresh.
    /// </summary>
    /// <returns>A factory returning the configured service.</returns>
    public required Func<NotableDateService> ServiceFactory { get; init; }

    /// <summary>
    /// Gets the delegate that constructs the <see cref="NotableDateFilter" /> applied by the assertion.
    /// </summary>
    /// <returns>A factory returning the filter.</returns>
    public required Func<NotableDateFilter> FilterFactory { get; init; }

    /// <summary>
    /// Gets the optional delegate that invokes the service under test. When <see langword="null" />, the
    /// default <c>(service, filter) =&gt; service.GetNotableDates(Year, filter)</c> is used. Set this to
    /// route the row through a different API overload — for example <c>GetNotableDates(startDate, endDate,
    /// filter)</c>, <c>GetNotableDates(date, filter)</c>, or <c>GetNotableDates(year, filter,
    /// territoryCode: "AU-NSW")</c>.
    /// </summary>
    /// <returns>The query invoker, or <see langword="null" /> to use the default year overload.</returns>
    public Func<NotableDateService, NotableDateFilter, IReadOnlyList<NotableDate>>? Query { get; init; }

    /// <summary>
    /// Gets the calendar year passed to the default <c>GetNotableDates(year, filter)</c> overload when
    /// <see cref="Query" /> is <see langword="null" />. Defaults to <c>2024</c>; scenarios that depend on a
    /// specific year override this. Ignored when <see cref="Query" /> is set.
    /// </summary>
    /// <returns>The query year.</returns>
    public int Year { get; init; } = 2024;

    /// <summary>
    /// Gets the set of holiday names that should appear in the filtered result, asserted with
    /// <c>CollectionAssert.AreEquivalent</c> (order-independent). An empty list asserts that the filter
    /// matches no rules for the year.
    /// </summary>
    /// <returns>The expected name set; never <see langword="null" />.</returns>
    public IReadOnlyList<string> ExpectedNames { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets an optional free-form annotation appended to the row's display name.
    /// </summary>
    /// <returns>The annotation, or <see langword="null" /> when none is recorded.</returns>
    public string? Note { get; init; }
}

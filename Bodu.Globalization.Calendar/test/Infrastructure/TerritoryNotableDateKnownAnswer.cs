// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TerritoryNotableDateKnownAnswer.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Globalization.Calendar.Data;

/// <summary>
/// Represents a single known-answer test row for a territory rule catalogue: a query (<see cref="Territory" />,
/// <see cref="Year" />, <see cref="Name" />) and the expected occurrence (<see cref="ExpectedDate" />, optional
/// adjustment, optional carrying territory code).
/// </summary>
/// <remarks>
/// <para>
/// Each row drives a single occurrence assertion: the test calls
/// <c>service.GetNotableDates(Year, Territory).Where(d =&gt; d.Name == Name).OrderBy(d =&gt; d.Date)</c> and asserts
/// that exactly one entry survives (when <see cref="ShouldExist" /> is <see langword="true" />, the default) or zero
/// entries survive (when <see langword="false" />). When an entry is expected, its <c>Date</c>, <c>TerritoryCode</c>,
/// <c>WasAdjusted</c>, and <c>AdjustmentReason.OriginalDate</c> are asserted against the populated fields below.
/// </para>
/// <para>
/// Rows live in project-local provider classes (for example
/// <c>Bodu.Globalization.Calendar.Data.AsiaPacific/test/AustraliaTerritoryAnswers.cs</c>) so each Data package can wire
/// its own provider factory; the record itself is authored once in the main calendar test project and linked into each
/// Data test csproj via
/// <c>&lt;Compile Include="..\..\Bodu.Globalization.Calendar\test\Infrastructure\TerritoryNotableDateKnownAnswer.cs" Link="..." /&gt;</c>
/// .
/// </para>
/// </remarks>
public sealed record TerritoryNotableDateKnownAnswer : IKat
{
    /// <inheritdoc />
    /// <remarks>Synthesizes a <c>"Territory Year Name"</c> label so the row remains identifiable through <see cref="KatDisplayName" /> even though the public <see cref="Name" /> property carries only the holiday name.</remarks>
    string IKat.Name => $"{Territory} {Year} {Name}";

    /// <summary>
    /// Gets the delegate that materialises the rule provider under test. Invoked once per row so each assertion runs
    /// against a fresh provider instance.
    /// </summary>
    /// <returns>A factory returning a configured <see cref="INotableDateRuleProvider" />.</returns>
    public required Func<INotableDateRuleProvider> ProviderFactory { get; init; }

    /// <summary>
    /// Gets the territory code passed to <c>GetNotableDates</c> — for example <c>"AU"</c>, <c>"AU-NSW"</c>, <c>"GB"</c>
    /// , or <c>"US"</c>.
    /// </summary>
    /// <returns>The query territory.</returns>
    public required string Territory { get; init; }

    /// <summary>
    /// Gets the calendar year passed to <c>GetNotableDates</c>.
    /// </summary>
    /// <returns>The query year.</returns>
    public required int Year { get; init; }

    /// <summary>
    /// Gets the canonical holiday name used to filter the returned occurrences — for example <c>"Anzac Day"</c> or
    /// <c>"Bastille Day"</c>.
    /// </summary>
    /// <returns>The holiday name asserted against <c>NotableDate.Name</c>.</returns>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the expected <c>NotableDate.Date</c> for the holiday in the queried year and territory. Ignored when
    /// <see cref="ShouldExist" /> is <see langword="false" />.
    /// </summary>
    /// <returns>The expected occurrence date.</returns>
    public DateTime ExpectedDate { get; init; }

    /// <summary>
    /// Gets the optional territory code expected on the returned <c>NotableDate.TerritoryCode</c>. When
    /// <see langword="null" /> (the default), the assertion compares against <see cref="Territory" />; set this when a
    /// subdivision query is expected to surface a canonical rule with a different code (for example querying
    /// <c>"AU-VIC"</c> for Anzac Day and expecting the canonical <c>"AU"</c> code).
    /// </summary>
    /// <returns>
    /// The expected carrying territory code, or <see langword="null" /> to assume <see cref="Territory" />.
    /// </returns>
    public string? ExpectedTerritoryCode { get; init; }

    /// <summary>
    /// Gets the expected <c>NotableDate.WasAdjusted</c> flag — <see langword="true" /> when the row models a weekend
    /// substitute that the rule's <c>ObservanceAdjustment</c> is expected to apply.
    /// </summary>
    /// <returns><see langword="true" /> when an adjustment is expected; otherwise <see langword="false" />.</returns>
    public bool WasAdjusted { get; init; }

    /// <summary>
    /// Gets the optional original (pre-adjustment) date asserted against
    /// <c>NotableDate.AdjustmentReason.OriginalDate</c>. Required when <see cref="WasAdjusted" /> is
    /// <see langword="true" />; ignored otherwise.
    /// </summary>
    /// <returns>
    /// The original date the rule resolved to before the substitute fired, or <see langword="null" /> when no
    /// adjustment is expected.
    /// </returns>
    public DateTime? OriginalDate { get; init; }

    /// <summary>
    /// Gets the optional expected day of week of <see cref="ExpectedDate" />. Used to make weekend-substitute rows
    /// self-documenting (the row reads as "expect Monday").
    /// </summary>
    /// <returns>The expected <see cref="DayOfWeek" />, or <see langword="null" /> to skip the check.</returns>
    public DayOfWeek? ExpectedDayOfWeek { get; init; }

    /// <summary>
    /// Gets a flag controlling whether an occurrence is expected at all. When <see langword="false" />, the assertion
    /// verifies that no <c>NotableDate</c> with <see cref="Name" /> exists for the (<see cref="Territory" />,
    /// <see cref="Year" />) query — used to encode rules suppressed by a <c>firstYear</c> bound or a date-range
    /// adjustment trigger.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> (the default) when an occurrence is expected; <see langword="false" /> to assert
    /// absence.
    /// </returns>
    public bool ShouldExist { get; init; } = true;

    /// <summary>
    /// Gets an optional free-form annotation appended to the row's display name, useful for embedding the real-world
    /// provenance of unusual rows (for example <c>"AU-NSW 2026-2027 Minns trial"</c>).
    /// </summary>
    /// <returns>The annotation text, or <see langword="null" /> when none is recorded.</returns>
    public string? Note { get; init; }
}

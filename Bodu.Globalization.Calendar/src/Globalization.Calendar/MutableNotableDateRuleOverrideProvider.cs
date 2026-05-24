// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MutableNotableDateRuleOverrideProvider.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides a thread-safe, runtime-mutable <see cref="INotableDateRuleOverrideProvider" /> whose additions and removals
/// can be modified after the consuming <see cref="NotableDateService" /> has been constructed.
/// </summary>
/// <remarks>
/// <para>
/// The provider exposes <see cref="AddRule" />, <see cref="RemoveRule" />, and <see cref="Clear" /> mutators that
/// each raise <see cref="Changed" /> after applying their update. Consumers that wire the provider through a service
/// composition root should subscribe <see cref="INotableDateService.Reload" /> to <see cref="Changed" /> so that
/// mutations take effect against the live service without rebuilding it. Without a corresponding
/// <see cref="INotableDateService.Reload" /> the service continues to observe the snapshot taken at construction.
/// </para>
/// <para>
/// Internal storage uses <see cref="ImmutableList{T}" /> so that concurrent enumerators returned by
/// <see cref="GetAdditions" /> and <see cref="GetRemovals" /> observe a coherent snapshot even while another thread is
/// mutating the provider. Updates are committed under a single internal gate to keep insertion ordering deterministic.
/// </para>
/// </remarks>
/// <example>
/// <para>
/// Authoring a one-off observance at runtime and refreshing the service:
/// </para>
/// <code>
///<![CDATA[
/// var overrides = new MutableNotableDateRuleOverrideProvider();
/// var service = new NotableDateService(
///     ruleProviders: AsiaPacificCalendarData.CreateProviders(),
///     workingWeek: WeekPattern.MondayToFriday,
///     options: new NotableDateServiceOptions { OverrideProviders = new[] { overrides } });
///
/// overrides.Changed += (_, _) => service.Reload();
///
/// overrides.AddRule(new NotableDateRule
/// {
///     Name = "Company Founding Day",
///     Strategy = DateResolutionStrategy.Fixed,
///     Category = NotableDateCategory.Observance,
///     Month = 6,
///     Day = 15,
///     IsNonWorkingDay = true,
/// });
///]]>
/// </code>
/// </example>
public sealed class MutableNotableDateRuleOverrideProvider : INotableDateRuleOverrideProvider
{
    /// <summary>
    /// Gate protecting writes to <see cref="_additions" /> and <see cref="_removals" />. Reads sample the immutable
    /// reference unsynchronised — atomic on a 64-bit runtime — so they incur no contention.
    /// </summary>
    private readonly object _gate = new();

    /// <summary>
    /// The current set of override additions in insertion order. Replaced on every mutation; never mutated in place.
    /// </summary>
    private ImmutableList<NotableDateRule> _additions = ImmutableList<NotableDateRule>.Empty;

    /// <summary>
    /// The current set of override removals in insertion order. Replaced on every mutation; never mutated in place.
    /// </summary>
    private ImmutableList<RuleRemoval> _removals = ImmutableList<RuleRemoval>.Empty;

    /// <summary>
    /// Raised after every mutation (<see cref="AddRule" />, <see cref="RemoveRule" />, <see cref="Clear" />) so that
    /// hosts can refresh dependent <see cref="INotableDateService" /> instances via
    /// <see cref="INotableDateService.Reload" />.
    /// </summary>
    /// <remarks>
    /// The event is raised after the internal state has been updated and outside the mutation gate, so handlers may
    /// safely call back into this provider. Handlers should treat the event as a coalescable hint, not a per-rule
    /// notification — a single <see cref="Clear" /> call raises one <see cref="Changed" /> event regardless of the
    /// number of rules dropped.
    /// </remarks>
    public event EventHandler? Changed;

    /// <summary>
    /// Adds a notable date rule to the override layer, replacing any same-name base rule when the service rebuilds.
    /// </summary>
    /// <param name="rule">The rule to add. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="rule" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Insertion order is preserved across <see cref="GetAdditions" /> calls. The same instance may be added more than
    /// once and will appear in the addition list each time, although the resolver applies same-name replacement, so
    /// duplicate additions of the same name collapse during service rebuild.
    /// </para>
    /// </remarks>
    public void AddRule(NotableDateRule rule)
    {
        ThrowHelper.ThrowIfNull(rule);

        lock (_gate)
        {
            _additions = _additions.Add(rule);
        }

        OnChanged();
    }

    /// <summary>
    /// Adds a removal entry that suppresses a base rule by name, optionally scoped to a year range and territory.
    /// </summary>
    /// <param name="name">The rule name to suppress. Must not be <see langword="null" /> or empty.</param>
    /// <param name="fromYear">Optional inclusive first year of the suppression.</param>
    /// <param name="toYear">Optional inclusive last year of the suppression.</param>
    /// <param name="territoryCode">Optional territory scope for the suppression.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name" /> is empty or whitespace.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The method appends a new <see cref="RuleRemoval" /> rather than coalescing overlapping removals. The
    /// <see cref="NotableDateService" /> evaluates the full removal list when applying overrides, so multiple removals
    /// targeting the same name compose by union of their year and territory scopes.
    /// </para>
    /// </remarks>
    public void RemoveRule(string name, int? fromYear = null, int? toYear = null, string? territoryCode = null)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(name);

        RuleRemoval removal = new(name, fromYear, toYear, territoryCode);

        lock (_gate)
        {
            _removals = _removals.Add(removal);
        }

        OnChanged();
    }

    /// <summary>
    /// Drops every authored addition and removal so that the provider contributes nothing to the next service rebuild.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A single <see cref="Changed" /> event is raised after both lists have been cleared, regardless of how many
    /// entries were present.
    /// </para>
    /// </remarks>
    public void Clear()
    {
        lock (_gate)
        {
            _additions = ImmutableList<NotableDateRule>.Empty;
            _removals = ImmutableList<RuleRemoval>.Empty;
        }

        OnChanged();
    }

    /// <summary>
    /// Returns a snapshot of the rules currently authored as additions, in the order they were added.
    /// </summary>
    /// <returns>The current addition list. The returned snapshot is stable under concurrent mutation.</returns>
    public IEnumerable<NotableDateRule> GetAdditions() => _additions;

    /// <summary>
    /// Returns a snapshot of the removals currently authored, in the order they were added.
    /// </summary>
    /// <returns>The current removal list. The returned snapshot is stable under concurrent mutation.</returns>
    public IEnumerable<RuleRemoval> GetRemovals() => _removals;

    /// <summary>
    /// Raises the <see cref="Changed" /> event with this provider as the sender and
    /// <see cref="EventArgs.Empty" /> as the payload.
    /// </summary>
    /// <remarks>
    /// The event is raised outside the mutation gate so that subscribed handlers may call back into this provider
    /// without re-entrancy hazards.
    /// </remarks>
    private void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);
}

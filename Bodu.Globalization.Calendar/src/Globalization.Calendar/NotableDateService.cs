// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateService.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides the canonical implementation of <see cref="INotableDateService" />, combining base
/// <see cref="INotableDateRuleProvider" /> sources with optional <see cref="INotableDateRuleOverrideProvider" /> layers and producing
/// resolved <see cref="NotableDate" /> instances on demand.
/// </summary>
/// <remarks>
/// <para>
/// The service caches resolved notable dates per year. Years are generated lazily on first access and cleared via
/// <see cref="Invalidate()" /> or <see cref="Invalidate(int)" />. The cache is thread-safe under concurrent reads and writes.
/// </para>
/// <para>
/// Multi-day events are supported: a <see cref="NotableDateRule" /> with <see cref="NotableDateRule.DurationDays" /> greater than one
/// produces a single <see cref="NotableDate" /> whose <see cref="NotableDate.EndDate" /> is the inclusive last day of the span. Range
/// and single-day queries return the span when any day within it intersects the query.
/// </para>
/// </remarks>
public sealed class NotableDateService : INotableDateService
{
	private const string DefaultResourceName = "Bodu.Globalization.Calendar.NotableDates.xml";

	private readonly ImmutableArray<NotableDateRule> _baseRules;
	private readonly IReadOnlyList<INotableDateRuleOverrideProvider> _overrideProviders;
	private readonly IReadOnlyList<NotableDateRule> _effectiveRules;
	private readonly NotableDateRuleResolver _resolver;
	private readonly NotableDateAdjuster _adjuster;
	private readonly INotableDateCollisionResolver _collisionResolver;
	private readonly INotableDateNameLocalizer? _nameLocalizer;
	private readonly CalendarWeekendDefinition _weekendDefinition;
	private readonly IWeekendDefinitionProvider? _weekendProvider;

	private readonly ConcurrentDictionary<int, IReadOnlyList<NotableDate>> _yearCache = new();
	private readonly object _gate = new();

	/// <summary>
	/// Initialises a new instance of the <see cref="NotableDateService" /> class using the embedded default rule set.
	/// </summary>
	public NotableDateService()
		: this(new[] { (INotableDateRuleProvider)new XmlResourceNotableDateRuleProvider(DefaultResourceName) },
			   CalendarWeekendDefinition.SaturdaySunday)
	{ }

	/// <summary>
	/// Initialises a new instance of the <see cref="NotableDateService" /> class.
	/// </summary>
	/// <param name="ruleProviders">Sources of base notable date rules. Must not be <see langword="null" />.</param>
	/// <param name="weekendDefinition">The weekend definition to apply when evaluating weekends.</param>
	/// <param name="weekendProvider">An optional custom weekend provider.</param>
	/// <param name="overrideProviders">Optional layered override providers, applied after the base rules in registration order.</param>
	/// <param name="calculatorRegistry">Optional registry used to resolve <see cref="DateResolutionStrategy.Calculator" /> rules.</param>
	/// <param name="adjustmentHandlers">Optional registry of custom <see cref="IAdjustmentHandler" /> instances.</param>
	/// <param name="collisionResolver">Optional collision resolver. Defaults to <see cref="DefaultNotableDateCollisionResolver" />.</param>
	/// <param name="nameLocalizer">Optional localizer used to translate notable date names into the active culture.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="ruleProviders" /> is <see langword="null" />.</exception>
	public NotableDateService(
		IEnumerable<INotableDateRuleProvider> ruleProviders,
		CalendarWeekendDefinition weekendDefinition,
		IWeekendDefinitionProvider? weekendProvider = null,
		IEnumerable<INotableDateRuleOverrideProvider>? overrideProviders = null,
		INotableDateCalculatorRegistry? calculatorRegistry = null,
		IAdjustmentHandlerRegistry? adjustmentHandlers = null,
		INotableDateCollisionResolver? collisionResolver = null,
		INotableDateNameLocalizer? nameLocalizer = null)
	{
		if (ruleProviders is null) throw new ArgumentNullException(nameof(ruleProviders));
		ThrowHelper.ThrowIfEnumValueIsUndefined(weekendDefinition);
		ThrowHelper.ThrowIfConditionallyRequiredParameterIsNull(weekendProvider, weekendDefinition, CalendarWeekendDefinition.Custom);

		_baseRules = ruleProviders.SelectMany(p => p.LoadRules()).ToImmutableArray();
		_overrideProviders = overrideProviders?.ToList() ?? (IReadOnlyList<INotableDateRuleOverrideProvider>)Array.Empty<INotableDateRuleOverrideProvider>();
		_weekendDefinition = weekendDefinition;
		_weekendProvider = weekendProvider;
		_collisionResolver = collisionResolver ?? new DefaultNotableDateCollisionResolver();
		_nameLocalizer = nameLocalizer;

		_effectiveRules = ApplyOverrides(_baseRules, _overrideProviders);
		_resolver = new NotableDateRuleResolver(_effectiveRules, calculatorRegistry);
		_adjuster = new NotableDateAdjuster(
			IsWeekend,
			IsNonWorkingDay,
			_weekendDefinition,
			_weekendProvider,
			adjustmentHandlers,
			ResolveByName);
	}

	// --------------------------------------------------------------------------------------
	// INotableDateService surface
	// --------------------------------------------------------------------------------------

	/// <inheritdoc />
	public bool IsWeekend(DateTime date) => date.IsWeekend(_weekendDefinition, _weekendProvider);

	/// <inheritdoc />
	public bool IsNonWorkingDay(DateTime date, string? territoryCode = null, Type? calendarType = null)
	{
		if (IsWeekend(date)) return true;

		var perYear = GetOrGenerateYear(date.Year);
		foreach (var notable in perYear)
		{
			if (!notable.IsNonWorkingDay) continue;
			if (!ContainsDay(notable, date.Date)) continue;
			if (!MatchesContext(notable, territoryCode, calendarType)) continue;

			return true;
		}

		return false;
	}

	/// <inheritdoc />
	public IReadOnlyList<NotableDate> GetNotableDates(int year, string? territoryCode = null, Type? calendarType = null)
	{
		var perYear = GetOrGenerateYear(year);
		return ProjectAndOrder(perYear, territoryCode, calendarType);
	}

	/// <inheritdoc />
	public IReadOnlyList<NotableDate> GetNotableDates(DateTime startDate, DateTime endDate, string? territoryCode = null, Type? calendarType = null)
	{
		if (endDate < startDate) (startDate, endDate) = (endDate, startDate);

		var results = new List<NotableDate>();
		for (int year = startDate.Year; year <= endDate.Year; year++)
		{
			var perYear = GetOrGenerateYear(year);
			foreach (var notable in perYear)
			{
				if (notable.EndDate < startDate.Date || notable.Date > endDate.Date) continue;
				if (!MatchesContext(notable, territoryCode, calendarType)) continue;

				results.Add(LocaliseIfNeeded(notable));
			}
		}

		// Apply the collision resolver per anchor day so that overlap rules see only same-day results, then concatenate in date order.
		return results
			.GroupBy(n => n.Date.Date)
			.OrderBy(g => g.Key)
			.SelectMany(g => _collisionResolver.Resolve(g.Key, g.ToList()))
			.ToList();
	}

	/// <inheritdoc />
	public IReadOnlyList<NotableDate> GetNotableDates(DateTime date, string? territoryCode = null, Type? calendarType = null)
	{
		var results = new List<NotableDate>();

		// Multi-day spans may have started in the previous year; check both years to cover wrap-around.
		foreach (var year in new[] { date.Year - 1, date.Year })
		{
			if (year < 1) continue;

			var perYear = GetOrGenerateYear(year);
			foreach (var notable in perYear)
			{
				if (!ContainsDay(notable, date.Date)) continue;
				if (!MatchesContext(notable, territoryCode, calendarType)) continue;

				results.Add(LocaliseIfNeeded(notable));
			}
		}

		return _collisionResolver.Resolve(date.Date, results);
	}

	/// <inheritdoc />
	public void Invalidate()
	{
		_yearCache.Clear();
	}

	/// <inheritdoc />
	public void Invalidate(int year)
	{
		_yearCache.TryRemove(year, out _);
	}

	// --------------------------------------------------------------------------------------
	// Generation pipeline
	// --------------------------------------------------------------------------------------

    /// <summary>
    /// Returns the cached per-year notable-date list for <paramref name="year" />, generating
    /// and caching it on first access.
    /// </summary>
    /// <param name="year">The civil year to resolve.</param>
    /// <returns>The notable dates for the specified year.</returns>
	private IReadOnlyList<NotableDate> GetOrGenerateYear(int year)
	{
		if (_yearCache.TryGetValue(year, out var cached))
			return cached;

		lock (_gate)
		{
			if (_yearCache.TryGetValue(year, out cached))
				return cached;

			var generated = GenerateYear(year);
			_yearCache[year] = generated;
			return generated;
		}
	}

    /// <summary>
    /// Materialises the notable dates for <paramref name="year" /> by invoking every configured
    /// rule, applying observance adjustments, and de-duplicating by rule identity.
    /// </summary>
    /// <param name="year">The civil year to generate.</param>
    /// <returns>The notable dates for <paramref name="year" />, in unspecified order.</returns>
	private IReadOnlyList<NotableDate> GenerateYear(int year)
	{
		var output = new List<NotableDate>();

		foreach (var rule in _effectiveRules)
		{
			if (!NotableDateRuleResolver.IsApplicable(rule, year))
				continue;

			DateTime? anchor;
			try
			{
				anchor = _resolver.ResolveAnchorDate(rule, year);
			}
			catch (InvalidOperationException)
			{
				// Surface broken rules at query time, not at construction time, so a single bad rule does not poison the cache.
				continue;
			}

			if (anchor is null)
				continue;

			foreach (var territory in ExpandTerritories(rule.TerritoryCode))
			{
				if (IsRemovedByOverride(rule, year, territory))
					continue;

				var baseDate = BuildNotableDate(rule, anchor.Value, territory, adjustmentReason: null);
				output.Add(baseDate);

				foreach (var adjustment in rule.Adjustments.OrderBy(a => a.Priority))
				{
					if (!NotableDateAdjuster.IsInScope(adjustment, year, territory, rule.CalendarType))
						continue;

					var result = _adjuster.Apply(adjustment, rule, anchor.Value, territory, rule.CalendarType);
					if (!result.Activated || result.AdjustedDate.Date == anchor.Value.Date)
						continue;

					bool isNonWorking = result.IsNonWorkingOverride ?? rule.IsNonWorkingDay ?? false;
					var reason = new AdjustmentReason(anchor.Value, result.Trigger, result.Action, result.HandlerKey);
					output.Add(BuildNotableDate(rule, result.AdjustedDate, territory, reason, isNonWorking));
				}
			}
		}

		return output;
	}

    /// <summary>
    /// Constructs a <see cref="NotableDate" /> from a rule, its resolved date, and any
    /// observance-adjustment metadata.
    /// </summary>
    /// <param name="rule">The originating notable-date rule.</param>
    /// <param name="date">The resolved observed date for the rule.</param>
    /// <param name="territory">The territory code, or <see langword="null" />.</param>
    /// <param name="adjustmentReason">The reason the observed date differs from the rule's base
    /// calculation, or <see cref="AdjustmentReason.None" /> if no adjustment was applied.</param>
    /// <param name="isNonWorkingOverride">If <see langword="true" />, the rule is flagged as a
    /// non-working day regardless of the underlying weekday.</param>
    /// <returns>The constructed <see cref="NotableDate" />.</returns>
	private static NotableDate BuildNotableDate(
		NotableDateRule rule,
		DateTime date,
		string? territory,
		AdjustmentReason? adjustmentReason,
		bool? isNonWorkingOverride = null)
	{
		return new NotableDate
		{
			Date = date,
			Name = rule.Name,
			Category = rule.Category,
			DurationDays = Math.Max(1, rule.DurationDays),
			IsNonWorkingDay = isNonWorkingOverride ?? rule.IsNonWorkingDay ?? false,
			CalendarType = rule.CalendarType,
			TerritoryCode = territory,
			Tags = rule.Tags,
			Comment = rule.Comment,
			AdjustmentReason = adjustmentReason,
		};
	}

    /// <summary>
    /// If a name-localiser is configured, replaces the name on <paramref name="notable" /> with
    /// its localised form; otherwise returns <paramref name="notable" /> unchanged.
    /// </summary>
    /// <param name="notable">The notable date to potentially localise.</param>
    /// <returns>The localised or original notable date.</returns>
	private NotableDate LocaliseIfNeeded(NotableDate notable)
	{
		if (_nameLocalizer is null) return notable;

		var localised = _nameLocalizer.GetDisplayName(notable, CultureInfo.CurrentCulture);
		if (string.Equals(localised, notable.Name, StringComparison.Ordinal))
			return notable;

		return notable with { Name = localised };
	}

    /// <summary>
    /// Filters the full-year notable-date list for the requested territory and calendar type,
    /// applies localisation, and returns the results ordered by observed date.
    /// </summary>
    /// <param name="perYear">The unfiltered notable dates for a single year.</param>
    /// <param name="territoryCode">The territory code filter, or <see langword="null" />.</param>
    /// <param name="calendarType">The calendar type filter, or <see langword="null" />.</param>
    /// <returns>The filtered, localised, and ordered notable-date list.</returns>
	private IReadOnlyList<NotableDate> ProjectAndOrder(IReadOnlyList<NotableDate> perYear, string? territoryCode, Type? calendarType)
	{
		var matching = new List<NotableDate>();
		foreach (var notable in perYear)
		{
			if (!MatchesContext(notable, territoryCode, calendarType)) continue;
			matching.Add(LocaliseIfNeeded(notable));
		}

		return matching
			.OrderBy(n => n.Date)
			.ThenBy(n => n.Name, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	// --------------------------------------------------------------------------------------
	// Override / scope helpers
	// --------------------------------------------------------------------------------------

    /// <summary>
    /// Applies the configured override provider to the base rule set for the given year, using
    /// the override's remove/add semantics.
    /// </summary>
    /// <param name="baseRules">The base set of rules to be overridden.</param>
    /// <param name="overrideProviders">The sequence of override providers whose overrides should
    /// be applied, in order.</param>
    /// <returns>The rule list after all overrides have been applied.</returns>
	private static IReadOnlyList<NotableDateRule> ApplyOverrides(
		ImmutableArray<NotableDateRule> baseRules,
		IReadOnlyList<INotableDateRuleOverrideProvider> overrideProviders)
	{
		if (overrideProviders.Count == 0)
			return baseRules.IsDefault ? (IReadOnlyList<NotableDateRule>)Array.Empty<NotableDateRule>() : baseRules;

		// Apply additions by composite (name, territory) key so that regional variants of the same notable date (e.g. multiple
		// Labour Day variants across Australian states) survive instead of collapsing into a single entry. Removals are evaluated
		// per-year inside GenerateYear so they can be scoped to specific years and territories.
		IEnumerable<NotableDateRule> source = baseRules.IsDefault
			? Enumerable.Empty<NotableDateRule>()
			: baseRules;

		var byKey = new Dictionary<(string Name, string Territory), NotableDateRule>();
		foreach (var rule in source)
		{
			byKey[CompositeKey(rule)] = rule;
		}

		foreach (var provider in overrideProviders)
		{
			foreach (var addition in provider.GetAdditions())
			{
				byKey[CompositeKey(addition)] = addition;
			}
		}

		return byKey.Values.ToList();
	}

	private static (string Name, string Territory) CompositeKey(NotableDateRule rule) =>
		(rule.Name ?? string.Empty, rule.TerritoryCode ?? string.Empty);

    /// <summary>
    /// Returns <see langword="true" /> if <paramref name="rule" /> has been suppressed for the
    /// specified year and territory by the configured override provider.
    /// </summary>
    /// <param name="rule">The candidate rule.</param>
    /// <param name="year">The civil year under evaluation.</param>
    /// <param name="territory">The territory code, or <see langword="null" /> for territory-neutral.</param>
    /// <returns><see langword="true" /> if the rule is removed; otherwise <see langword="false" />.</returns>
	private bool IsRemovedByOverride(NotableDateRule rule, int year, string? territory)
	{
		foreach (var provider in _overrideProviders)
		{
			foreach (var removal in provider.GetRemovals())
			{
				if (!string.Equals(removal.RuleName, rule.Name, StringComparison.OrdinalIgnoreCase))
					continue;

				if (removal.FromYear is { } from && year < from) continue;
				if (removal.ToYear is { } to && year > to) continue;

				if (!string.IsNullOrEmpty(removal.TerritoryCode))
				{
					if (string.IsNullOrEmpty(territory)) continue;
					if (!TerritoryCode.TryParse(removal.TerritoryCode, out var removalScope)) continue;
					if (!TerritoryCode.TryParse(territory, out var actual)) continue;
					if (!removalScope.Contains(actual)) continue;
				}

				return true;
			}
		}

		return false;
	}

    /// <summary>
    /// Splits a comma-separated territory list into individual codes, or yields a single
    /// <see langword="null" /> when <paramref name="value" /> is <see langword="null" />.
    /// </summary>
    /// <param name="value">A territory code, a comma-separated list, or <see langword="null" />.</param>
    /// <returns>The individual territory codes.</returns>
	private static IEnumerable<string?> ExpandTerritories(string? value)
	{
		if (string.IsNullOrEmpty(value))
		{
			yield return null;
			yield break;
		}

		foreach (var territory in TerritoryCode.ParseList(value))
			yield return territory.ToString();
	}

    /// <summary>
    /// Returns <see langword="true" /> if <paramref name="date" /> applies in the supplied
    /// territory and calendar context.
    /// </summary>
    /// <param name="date">The candidate notable date.</param>
    /// <param name="territoryCode">The territory code filter, or <see langword="null" /> to match any.</param>
    /// <param name="calendarType">The calendar type filter, or <see langword="null" /> to match any.</param>
    /// <returns><see langword="true" /> if the date matches; otherwise <see langword="false" />.</returns>
	private static bool MatchesContext(NotableDate date, string? territoryCode, Type? calendarType)
	{
		if (calendarType is not null && date.CalendarType is not null && date.CalendarType != calendarType)
			return false;

		if (string.IsNullOrEmpty(territoryCode) || string.IsNullOrEmpty(date.TerritoryCode))
			return true;

		if (!TerritoryCode.TryParse(territoryCode, out var requested))
			return false;
		if (!TerritoryCode.TryParse(date.TerritoryCode, out var owned))
			return false;

		// A query for "AU" matches both "AU" and any "AU-XXX" subdivision; a query for "AU-NSW" matches only itself or the parent "AU".
		return requested.Contains(owned) || owned.Contains(requested);
	}

    /// <summary>
    /// Returns <see langword="true" /> if <paramref name="notable" /> covers the calendar day
    /// of <paramref name="day" />, ignoring the time component.
    /// </summary>
    /// <param name="notable">The notable date.</param>
    /// <param name="day">The day under test.</param>
    /// <returns><see langword="true" /> if the day is covered.</returns>
	private static bool ContainsDay(NotableDate notable, DateTime day)
	{
		return day >= notable.Date.Date && day <= notable.EndDate.Date;
	}

    /// <summary>
    /// Resolves the observed date for a rule identified by <paramref name="ruleName" /> in the
    /// given year, territory, and calendar context.
    /// </summary>
    /// <param name="ruleName">The rule name.</param>
    /// <param name="year">The civil year.</param>
    /// <param name="territoryCode">The territory code, or <see langword="null" />.</param>
    /// <param name="calendarType">The calendar type, or <see langword="null" />.</param>
    /// <returns>The observed date, or <see langword="null" /> if no matching rule exists.</returns>
	private DateTime? ResolveByName(string ruleName, int year, string? territoryCode, Type? calendarType)
	{
		var perYear = GetOrGenerateYear(year);
		foreach (var notable in perYear)
		{
			if (!string.Equals(notable.Name, ruleName, StringComparison.OrdinalIgnoreCase)) continue;
			if (!MatchesContext(notable, territoryCode, calendarType)) continue;

			return notable.Date;
		}

		return null;
	}
}

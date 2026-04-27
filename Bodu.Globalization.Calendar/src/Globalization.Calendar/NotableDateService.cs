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
	private const string DefaultResourceName = "Bodu/Globalization/Calendar/Resources/global-all.xml";

	private readonly ImmutableArray<NotableDateRule> _baseRules;
	private readonly IReadOnlyList<INotableDateRuleOverrideProvider> _overrideProviders;
	private readonly IReadOnlyList<RuleRemoval> _overrideRemovals;
	private readonly IReadOnlyList<NotableDateRule> _effectiveRules;
	private readonly NotableDateRuleResolver _resolver;
	private readonly NotableDateAdjuster _adjuster;
	private readonly INotableDateCollisionResolver _collisionResolver;
	private readonly INotableDateNameLocalizer? _nameLocalizer;
	private readonly CalendarWeekendDefinition _weekendDefinition;
	private readonly IWeekendDefinitionProvider? _weekendProvider;
	private readonly IResourcePathResolver _resourcePathResolver;

	private readonly ConcurrentDictionary<int, IReadOnlyList<NotableDate>> _yearCache = new();
	private readonly object _gate = new();

	// Per-thread set of years currently being generated on this thread. Used by GetOrGenerateYear to short-circuit recursive
	// entry from within GenerateYear (for example, MoveToNextNonWorkingDay's walk calling back through IsNonWorkingDay) so that a
	// single rule cannot cause the generator to stack-overflow by consulting the very year it is in the middle of producing.
	private readonly ThreadLocal<HashSet<int>> _generatingYears = new(() => new HashSet<int>());

	/// <summary>
	/// Initializes a new instance of the <see cref="NotableDateService" /> class using the embedded default rule set.
	/// </summary>
	public NotableDateService()
		: this(new[] { (INotableDateRuleProvider)new XmlResourceNotableDateRuleProvider(DefaultResourceName, new ResourcePathResolver()) },
			   CalendarWeekendDefinition.SaturdaySunday)
	{ }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateService" /> class.
    /// </summary>
    /// <param name="ruleProviders">Sources of base notable date rules. Must not be <see langword="null" />.</param>
    /// <param name="weekendDefinition">The weekend definition to apply when evaluating weekends.</param>
    /// <param name="resourcePathResolver">An optional custom weekend provider.</param>
    /// <param name="weekendProvider">An optional custom weekend provider.</param>
    /// <param name="overrideProviders">Optional layered override providers, applied after the base rules in registration order.</param>
    /// <param name="algorithmRegistry">Optional registry used to resolve <see cref="DateResolutionStrategy.Algorithm" /> rules.</param>
    /// <param name="adjustmentHandlers">Optional registry of custom <see cref="IAdjustmentHandler" /> instances.</param>
    /// <param name="collisionResolver">Optional collision resolver. Defaults to <see cref="DefaultNotableDateCollisionResolver" />.</param>
    /// <param name="nameLocalizer">Optional localizer used to translate notable date names into the active culture.</param>
    /// <param name="plugins">Optional external plugins loaded via <see cref="Plugins.ExternalPluginLoader" />. Rule providers exposed by plugins are appended to <paramref name="ruleProviders" /> and participate in the normal flatten pipeline; named algorithms are registered onto an internal algorithm registry that falls back to <paramref name="algorithmRegistry" /> when supplied (caller-supplied registrations win on key collision).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ruleProviders" /> is <see langword="null" />.</exception>
    public NotableDateService(
		IEnumerable<INotableDateRuleProvider> ruleProviders,
		CalendarWeekendDefinition weekendDefinition,
        IResourcePathResolver? resourcePathResolver=null,
        IWeekendDefinitionProvider? weekendProvider = null,
		IEnumerable<INotableDateRuleOverrideProvider>? overrideProviders = null,
		INotableDateAlgorithmRegistry? algorithmRegistry = null,
		IAdjustmentHandlerRegistry? adjustmentHandlers = null,
		INotableDateCollisionResolver? collisionResolver = null,
		INotableDateNameLocalizer? nameLocalizer = null,
		IEnumerable<Plugins.INotableDatePlugin>? plugins = null)
	{
		if (ruleProviders is null) throw new ArgumentNullException(nameof(ruleProviders));
		ThrowHelper.ThrowIfEnumValueIsUndefined(weekendDefinition);
		ThrowHelper.ThrowIfConditionallyRequiredParameterIsNull(weekendProvider, weekendDefinition, CalendarWeekendDefinition.Custom);

		// Fan plugin contributions into the provider list and the algorithm registry. The merge order means host-level
		// rule providers are loaded first and therefore win composite-key collisions inside the flatten pipeline, and
		// host-supplied algorithm registrations take precedence over plugin-supplied ones with the same key.
		var effectiveProviders = ruleProviders.ToList();
		var effectiveRegistry = algorithmRegistry;

		if (plugins is not null)
		{
			var pluginAlgorithms = new List<KeyValuePair<string, INotableDateAlgorithm>>();

			foreach (var plugin in plugins)
			{
				if (plugin is Plugins.INotableDateRulePlugin rulePlugin)
				{
					foreach (var provider in rulePlugin.GetRuleProviders())
						effectiveProviders.Add(provider);
				}

				if (plugin is Plugins.INotableDateAlgorithmPlugin calcPlugin)
				{
					foreach (var pair in calcPlugin.GetAlgorithms())
						pluginAlgorithms.Add(pair);
				}
			}

			if (pluginAlgorithms.Count > 0)
			{
				var pluginRegistry = new NotableDateAlgorithmRegistry(pluginAlgorithms);
				effectiveRegistry = effectiveRegistry is null
					? pluginRegistry
					: new CompositeAlgorithmRegistry(effectiveRegistry, pluginRegistry);
			}
		}

		_baseRules = effectiveProviders.SelectMany(p => p.LoadRules()).ToImmutableArray();
		_overrideProviders = overrideProviders?.ToList() ?? (IReadOnlyList<INotableDateRuleOverrideProvider>)Array.Empty<INotableDateRuleOverrideProvider>();

		// Snapshot every override provider's removals at construction so that IsRemovedByOverride iterates a materialised list
		// once per rule × year × territory rather than re-invoking GetRemovals on every check. This pins the cost of any
		// non-trivial override provider (database-backed, configuration-bound, lazily-enumerated) to a single call per provider
		// and removes a runaway vector for providers that return fresh, infinite, or expensive enumerables on each invocation.
		_overrideRemovals = _overrideProviders.SelectMany(p => p.GetRemovals()).ToList();

		_weekendDefinition = weekendDefinition;
		_weekendProvider = weekendProvider;
		_collisionResolver = collisionResolver ?? new DefaultNotableDateCollisionResolver();
		_nameLocalizer = nameLocalizer;
        _resourcePathResolver= resourcePathResolver ?? new ResourcePathResolver();

        _effectiveRules = ApplyOverrides(_baseRules, _overrideProviders);
		_resolver = new NotableDateRuleResolver(_effectiveRules, effectiveRegistry);
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
		IReadOnlyList<NotableDate> perYear = GetOrGenerateYear(year);
		return ProjectAndOrder(perYear, territoryCode, calendarType);
	}

	/// <inheritdoc />
	public IReadOnlyList<NotableDate> GetNotableDates(int year, NotableDateFilter filter, string? territoryCode = null, Type? calendarType = null)
	{
		ThrowHelper.ThrowIfNull(filter);

		IReadOnlyList<NotableDate> perYear = GenerateYearFiltered(year, filter);
		return ProjectAndOrder(perYear, territoryCode, calendarType);
	}

	/// <inheritdoc />
	public IReadOnlyList<NotableDate> GetNotableDates(DateTime startDate, DateTime endDate, string? territoryCode = null, Type? calendarType = null)
	{
		if (endDate < startDate) (startDate, endDate) = (endDate, startDate);

		List<NotableDate> results = new();
		for (int year = startDate.Year; year <= endDate.Year; year++)
		{
			IReadOnlyList<NotableDate> perYear = GetOrGenerateYear(year);
			foreach (NotableDate notable in perYear)
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
	public IReadOnlyList<NotableDate> GetNotableDates(DateTime startDate, DateTime endDate, NotableDateFilter filter, string? territoryCode = null, Type? calendarType = null)
	{
		ThrowHelper.ThrowIfNull(filter);

		if (endDate < startDate) (startDate, endDate) = (endDate, startDate);

		List<NotableDate> results = new();
		for (int year = startDate.Year; year <= endDate.Year; year++)
		{
			IReadOnlyList<NotableDate> perYear = GenerateYearFiltered(year, filter);
			foreach (NotableDate notable in perYear)
			{
				if (notable.EndDate < startDate.Date || notable.Date > endDate.Date) continue;
				if (!MatchesContext(notable, territoryCode, calendarType)) continue;

				results.Add(LocaliseIfNeeded(notable));
			}
		}

		return results
			.GroupBy(n => n.Date.Date)
			.OrderBy(g => g.Key)
			.SelectMany(g => _collisionResolver.Resolve(g.Key, g.ToList()))
			.ToList();
	}

	/// <inheritdoc />
	public IReadOnlyList<NotableDate> GetNotableDates(DateTime date, string? territoryCode = null, Type? calendarType = null)
	{
		List<NotableDate> results = new();

		// Multi-day spans may have started in the previous year; check both years to cover wrap-around.
		foreach (int year in new[] { date.Year - 1, date.Year })
		{
			if (year < 1) continue;

			IReadOnlyList<NotableDate> perYear = GetOrGenerateYear(year);
			foreach (NotableDate notable in perYear)
			{
				if (!ContainsDay(notable, date.Date)) continue;
				if (!MatchesContext(notable, territoryCode, calendarType)) continue;

				results.Add(LocaliseIfNeeded(notable));
			}
		}

		return _collisionResolver.Resolve(date.Date, results);
	}

	/// <inheritdoc />
	public IReadOnlyList<NotableDate> GetNotableDates(DateTime date, NotableDateFilter filter, string? territoryCode = null, Type? calendarType = null)
	{
		ThrowHelper.ThrowIfNull(filter);

		List<NotableDate> results = new();

		// Multi-day spans may have started in the previous year; check both years to cover wrap-around.
		foreach (int year in new[] { date.Year - 1, date.Year })
		{
			if (year < 1) continue;

			IReadOnlyList<NotableDate> perYear = GenerateYearFiltered(year, filter);
			foreach (NotableDate notable in perYear)
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

		// Re-entry guard: if this thread is already generating any year higher up the call stack, do not start another generation
		// pass. MoveToNextNonWorkingDay's bounded walk calls back through IsNonWorkingDay → GetOrGenerateYear; without the guard
		// it recurses for the originating year (same-year re-entry) and, when the walk crosses a year boundary, would otherwise
		// open a fresh generation for the next year, leading to year-by-year recursion until DateTime overflows. Returning an
		// empty snapshot for any cache-miss query during nested generation collapses both vectors: dependent predicates see only
		// what is already cached, the bounded walk falls through its 366-iteration cap, and only the outer caller fully
		// materialises a year.
		HashSet<int> inProgress = _generatingYears.Value!;
		if (inProgress.Count > 0)
			return Array.Empty<NotableDate>();

		lock (_gate)
		{
			if (_yearCache.TryGetValue(year, out cached))
				return cached;

			inProgress.Add(year);
			try
			{
				var generated = GenerateYear(year);
				_yearCache[year] = generated;
				return generated;
			}
			finally
			{
				inProgress.Remove(year);
			}
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

					// When the adjustment declares a more specific territory than the rule itself (e.g. an Anzac
					// Day substitute scoped to AU-WA sitting on a country-level AU rule), tag the emitted
					// occurrence with that narrower scope so downstream filtering and delineation reflect where
					// the substitute is actually observed.
					string? emittedTerritory = !string.IsNullOrEmpty(adjustment.TerritoryCode)
						? adjustment.TerritoryCode
						: territory;

					bool isNonWorking = result.IsNonWorkingOverride ?? rule.IsNonWorkingDay ?? false;
					var reason = new AdjustmentReason(anchor.Value, result.Trigger, result.Action, result.HandlerKey);
					output.Add(BuildNotableDate(rule, result.AdjustedDate, emittedTerritory, reason, isNonWorking));
				}
			}
		}

		return output;
	}

    /// <summary>
    /// Materialises the notable dates for <paramref name="year" /> by invoking every rule that passes the primary gate of
    /// <paramref name="filter" />, applying observance adjustments, and retaining only those dates that pass the secondary gate.
    /// Results are never written to the per-year cache so that unfiltered queries continue to receive complete cached results.
    /// </summary>
    /// <param name="year">The civil year to generate.</param>
    /// <param name="filter">The filter whose primary gate is applied before date resolution and whose secondary gate is applied to
    /// each materialised date.</param>
    /// <returns>The filtered notable dates for <paramref name="year" />, in unspecified order.</returns>
	private IReadOnlyList<NotableDate> GenerateYearFiltered(int year, NotableDateFilter filter)
	{
		List<NotableDate> output = new();

		foreach (NotableDateRule rule in _effectiveRules)
		{
			if (!NotableDateRuleResolver.IsApplicable(rule, year))
				continue;

			if (!filter.IsRuleEligible(rule))
				continue;

			DateTime? anchor;
			try
			{
				anchor = _resolver.ResolveAnchorDate(rule, year);
			}
			catch (InvalidOperationException)
			{
				continue;
			}

			if (anchor is null)
				continue;

			foreach (string? territory in ExpandTerritories(rule.TerritoryCode))
			{
				if (IsRemovedByOverride(rule, year, territory))
					continue;

				NotableDate baseDate = BuildNotableDate(rule, anchor.Value, territory, adjustmentReason: null);
				if (filter.IsMatch(baseDate))
					output.Add(baseDate);

				foreach (ObservanceAdjustment adjustment in rule.Adjustments.OrderBy(a => a.Priority))
				{
					if (!NotableDateAdjuster.IsInScope(adjustment, year, territory, rule.CalendarType))
						continue;

					AdjustmentApplyResult result = _adjuster.Apply(adjustment, rule, anchor.Value, territory, rule.CalendarType);
					if (!result.Activated || result.AdjustedDate.Date == anchor.Value.Date)
						continue;

					string? emittedTerritory = !string.IsNullOrEmpty(adjustment.TerritoryCode)
						? adjustment.TerritoryCode
						: territory;

					bool isNonWorking = result.IsNonWorkingOverride ?? rule.IsNonWorkingDay ?? false;
					AdjustmentReason reason = new(anchor.Value, result.Trigger, result.Action, result.HandlerKey);
					NotableDate adjustedDate = BuildNotableDate(rule, result.AdjustedDate, emittedTerritory, reason, isNonWorking);
					if (filter.IsMatch(adjustedDate))
						output.Add(adjustedDate);
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
    /// calculation, or <see langword="null" /> if no adjustment was applied.</param>
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
		foreach (var removal in _overrideRemovals)
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

	/// <summary>
	/// Layers two <see cref="INotableDateAlgorithmRegistry" /> instances: <paramref name="primary" /> is consulted first; on a
	/// miss, <paramref name="fallback" /> is consulted. Used to compose host-supplied algorithms with plugin-supplied ones so
	/// the host retains precedence on key collisions.
	/// </summary>
	private sealed class CompositeAlgorithmRegistry : INotableDateAlgorithmRegistry
	{
		private readonly INotableDateAlgorithmRegistry _primary;
		private readonly INotableDateAlgorithmRegistry _fallback;

		public CompositeAlgorithmRegistry(INotableDateAlgorithmRegistry primary, INotableDateAlgorithmRegistry fallback)
		{
			_primary = primary;
			_fallback = fallback;
		}

		public bool Contains(string key) => _primary.Contains(key) || _fallback.Contains(key);

		public bool TryGet(string key, out INotableDateAlgorithm algorithm)
		{
			if (_primary.TryGet(key, out algorithm!))
				return true;

			return _fallback.TryGet(key, out algorithm!);
		}
	}
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateService.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Globalization;
using Bodu.Extensions;
using Bodu.Globalization.Calendar.Plugins;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides the canonical implementation of <see cref="INotableDateService" />, combining base
/// <see cref="INotableDateRuleProvider" /> sources with optional <see cref="INotableDateRuleOverrideProvider" /> layers
/// and producing resolved <see cref="NotableDate" /> instances on demand.
/// </summary>
/// <remarks>
/// <para>
/// This type is the orchestrator at the centre of the resolution pipeline. The pipeline reads
/// <c>Rule source → NotableDateRule → Resolution strategy → Nominal date → Adjustment pipeline → Resolved NotableDate → Consumer query</c>
/// ; the documentation introduction (<c>docs/calendar/index.md</c>) and the concepts page (
/// <c>docs/calendar/concepts.md</c>) render this flow as a diagram and provide the surrounding vocabulary, and the
/// <c>guides/calendar/*</c> walk-throughs cover authoring rules, working-day arithmetic, observance adjustments,
/// territories, and data packs.
/// </para>
/// <para>
/// The service caches resolved notable dates per year. Years are generated lazily on first access and cleared via
/// <see cref="Invalidate()" /> or <see cref="Invalidate(int)" />. The cache is thread-safe under concurrent reads and
/// writes.
/// </para>
/// <para>
/// Multi-day events are supported: a <see cref="NotableDateRule" /> with <see cref="NotableDateRule.DurationDays" />
/// greater than one produces a single <see cref="NotableDate" /> whose <see cref="NotableDate.EndDate" /> is the
/// inclusive last day of the span. Range and single-day queries return the span when any day within it intersects the
/// query.
/// </para>
/// <para>
/// The default no-argument constructor loads only the embedded minimal default rule set (currently New Year's Day) so a
/// service can be created without referencing any companion data pack. Region-specific public holidays — US federal
/// holidays, UK bank holidays, Australian state observances, and so on — ship in separate
/// <c>Bodu.Globalization.Calendar.Data.*</c> assemblies and are added by passing the pack's <c>CreateProvider()</c>
/// result through the full constructor. The full constructor accepts base rule providers, weekend definitions, override
/// providers, algorithm registries, custom adjustment handlers, collision resolvers, and name localizers, enabling
/// complete control over the resolution pipeline.
/// </para>
/// <para>
/// Pipeline stage cross-reference:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <b>Rule source</b> — supplied via the <c>ruleProviders</c> constructor parameter using
/// <see cref="INotableDateRuleProvider" /> implementations such as <see cref="XmlResourceNotableDateRuleProvider" /> or
/// <see cref="JsonResourceNotableDateRuleProvider" />, or via companion <c>Bodu.Globalization.Calendar.Data.*</c>
/// packs.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>NotableDateRule → Resolution strategy</b> — each <see cref="NotableDateRule" /> declares a
/// <see cref="DateResolutionStrategy" /> (<c>Fixed</c>, <c>DayOfWeekInMonth</c>, <c>OffsetFromAnchor</c>, or
/// <c>Algorithm</c>) that the resolver dispatches to per year, producing the <i>nominal</i> date.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>Adjustment pipeline</b> — <see cref="ObservanceAdjustment" /> entries on the rule fire through registered
/// <see cref="IAdjustmentHandler" /> instances (managed by <see cref="AdjustmentHandlerRegistry" />), yielding the <i>
/// observed</i> date and populating <see cref="NotableDate.WasAdjusted" /> /
/// <see cref="NotableDate.AdjustmentReason" />.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>Resolved NotableDate</b> — an immutable <see cref="NotableDate" /> is cached per year.
/// <see cref="Invalidate()" /> drops all years; <see cref="Invalidate(int)" /> drops a single year.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>Consumer query</b> — <c>GetNotableDates</c> and its overloads return the resolved set;
/// <see cref="NotableDateFilter" /> composes territory / category / tag predicates;
/// <c>Bodu.Extensions.NotableDateOnlyExtensions</c> and <c>NotableDateTimeExtensions</c> provide working-day arithmetic
/// (<c>IsWorkingDay</c>, <c>AddWorkingDays</c>, <c>NextWorkingDay</c>, …).
/// </description>
/// </item>
/// </list>
/// </remarks>
/// <example>
/// <para>
/// Construct with the built-in minimal rule set, then layer Australian public holidays from a companion data pack:
/// </para>
/// <code>
///<![CDATA[
/// // Simplest construction — loads only the embedded New Year's Day rule:
/// NotableDateService service = new NotableDateService();
///
/// // All notable dates for New South Wales in 2026 (requires the Asia-Pacific data pack):
/// IReadOnlyList<NotableDate> dates = service.GetNotableDates(2026, territoryCode: "AU-NSW");
///
/// // Full construction with a custom XML rule file and algorithm registry:
/// var registry = new NotableDateAlgorithmRegistry()
///     .Register("easter-sunday", new GregorianEasterSundayNotableDateProvider());
///
/// NotableDateService fullService = new NotableDateService(
///     ruleProviders: new[] { new XmlResourceNotableDateRuleProvider(
///         "MyApp/Calendar/Resources/custom-rules.xml",
///         new ResourcePathResolver()) },
///     workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday,
///     options: new NotableDateServiceOptions { AlgorithmRegistry = registry });
///
/// // Invalidate the cache when runtime overrides change:
/// service.Invalidate();
///]]>
/// </code>
/// </example>
/// <seealso cref="INotableDateService" /> <seealso cref="NotableDateRule" /> <seealso cref="NotableDate" />
/// <seealso cref="NotableDateFilter" /> <seealso cref="ObservanceAdjustment" />
/// <seealso cref="INotableDateRuleProvider" /> <seealso cref="INotableDateRuleOverrideProvider" />
/// <seealso cref="NotableDateServiceOptions" />
public sealed class NotableDateService : INotableDateService, IDisposable
{
    /// <summary>
    /// The embedded resource path for the minimal default rule set used by the parameterless constructor.
    /// </summary>
    private const string DefaultResourceName = "Bodu/Globalization/Calendar/Resources/default-minimal.xml";

    /// <summary>
    /// The optional registry of custom adjustment handlers used during generation.
    /// </summary>
    private readonly IAdjustmentHandlerRegistry? _adjustmentHandlers;

    /// <summary>
    /// The immutable snapshot of base rules loaded at construction from all rule providers.
    /// </summary>
    private readonly ImmutableArray<NotableDateRule> _baseRules;

    /// <summary>
    /// The resolver that arbitrates when multiple rules produce a date on the same day.
    /// </summary>
    private readonly INotableDateCollisionResolver _collisionResolver;

    /// <summary>
    /// The merged rule set after all overrides have been applied; drives every resolution pass. Rebuilt by
    /// <see cref="Reload" /> when an <see cref="INotableDateRuleOverrideProvider" /> mutates its contributions.
    /// </summary>
    private IReadOnlyList<NotableDateRule> _effectiveRules;

    /// <summary>
    /// Lock guarding atomic publication of effective rules, resolver, and lazy <see cref="_rangePipeline" /> across
    /// <see cref="Reload" /> and reader threads.
    /// </summary>
    private readonly object _gate = new();

    /// <summary>
    /// The optional localizer used to translate notable date names into the active culture.
    /// </summary>
    private readonly INotableDateNameLocalizer? _nameLocalizer;

    /// <summary>
    /// Identity-keyed set of every rule contributed by an <see cref="INotableDateRuleOverrideProvider" /> addition.
    /// Used downstream to exempt override additions from same-name <see cref="RuleRemoval" /> suppression, so an
    /// addition can replace a removed base rule of the same name. Rebuilt by <see cref="Reload" /> alongside
    /// <see cref="_effectiveRules" />.
    /// </summary>
    private HashSet<NotableDateRule> _overrideAdditions;

    /// <summary>
    /// The ordered list of override providers applied on top of the base rule set.
    /// </summary>
    private readonly IReadOnlyList<INotableDateRuleOverrideProvider> _overrideProviders;

    /// <summary>
    /// Snapshot of all override removals, materialized once at construction and refreshed by <see cref="Reload" /> so
    /// that downstream lookups iterate a materialized list once per rule × year × territory rather than re-invoking
    /// <see cref="INotableDateRuleOverrideProvider.GetRemovals" /> on every check.
    /// </summary>
    private IReadOnlyList<RuleRemoval> _overrideRemovals;

    /// <summary>
    /// The chronological windows resolved by <see cref="ResolveNotableDatesInRange" /> since the last
    /// <see cref="Invalidate()" /> call.
    /// </summary>
    private readonly RangeResolution.ResolvedWindowSet _resolvedWindows = new();

    /// <summary>
    /// Lock protecting concurrent updates to <see cref="_resolvedWindows" />.
    /// </summary>
    private readonly object _resolvedWindowsGate = new();

    /// <summary>
    /// The resolver that turns each rule into an anchor date for a given year. Rebuilt by <see cref="Reload" /> when
    /// the effective rule set changes.
    /// </summary>
    private NotableDateRuleResolver _resolver;

    /// <summary>
    /// The composed algorithm registry that was supplied to the resolver, retained so <see cref="Reload" /> can
    /// reconstruct an equivalent resolver against the refreshed rule set.
    /// </summary>
    private readonly INotableDateAlgorithmRegistry? _algorithmRegistry;

    /// <summary>
    /// The resolver used to locate embedded XML resource files.
    /// </summary>
    private readonly IResourcePathResolver _resourcePathResolver;

    /// <summary>
    /// The lazily constructed range-resolution pipeline used by <see cref="ResolveNotableDatesInRange" /> and the
    /// adapted <c>GetNotableDates</c> overloads. Marked <see langword="volatile" /> so the fast-path read in
    /// <see cref="GetOrBuildRangePipeline" /> reliably observes the publication performed inside <see cref="_gate" />.
    /// </summary>
    private volatile RangeResolution.NotableDateRangePipeline? _rangePipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateService" /> class using the embedded minimal default
    /// rule set (currently a single rule for New Year's Day). Region-specific holidays must be supplied via the full
    /// constructor by passing providers from the <c>Bodu.Globalization.Calendar.Data.*</c> companion assemblies.
    /// </summary>
    public NotableDateService()
        : this(
        [
            new XmlResourceNotableDateRuleProvider(DefaultResourceName, new ResourcePathResolver())
        ],
        WeekPattern.MondayToFriday)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateService" /> class using the embedded minimal default
    /// rule set and the supplied <see cref="WeekPattern" /> working week.
    /// </summary>
    /// <param name="workingWeek">The working-week pattern used when classifying dates.</param>
    /// <remarks>
    /// Equivalent to the parameterless constructor but with a caller-supplied working week. Use the full
    /// <see cref="NotableDateService(IEnumerable{INotableDateRuleProvider}, WeekPattern, NotableDateServiceOptions?)" />
    /// constructor when region-specific rule providers are required.
    /// </remarks>
    public NotableDateService(WeekPattern workingWeek)
        : this(
        [
            new XmlResourceNotableDateRuleProvider(DefaultResourceName, new ResourcePathResolver())
        ],
        workingWeek)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateService" /> class using the embedded minimal default
    /// rule set and the supplied named <see cref="WorkingDaysOfWeek" /> working week.
    /// </summary>
    /// <param name="workingDaysOfWeek">The named working-week pattern used when classifying dates.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="workingDaysOfWeek" /> is not a defined member of the
    /// <see cref="WorkingDaysOfWeek" /> enumeration.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="workingDaysOfWeek" /> is <see cref="WorkingDaysOfWeek.Custom" />, which has no
    /// canonical pattern.
    /// </exception>
    public NotableDateService(WorkingDaysOfWeek workingDaysOfWeek)
        : this(workingDaysOfWeek.ToWeekPattern())
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateService" /> class using a caller-supplied
    /// <see cref="WeekPattern" /> as the working week. This is the canonical constructor; every other overload routes
    /// through it.
    /// </summary>
    /// <param name="ruleProviders">Sources of base notable date rules. Must not be <see langword="null" />.</param>
    /// <param name="workingWeek">The working-week pattern used when classifying dates.</param>
    /// <param name="options">Optional service configuration. When <see langword="null" />, defaults apply.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="ruleProviders" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// To use a custom weekend supplied by an <see cref="IWeekendDefinitionProvider" />, convert it to a
    /// <see cref="WeekPattern" /> first via
    /// <see cref="Bodu.Extensions.IWeekendDefinitionProviderExtensions.ToWeekPattern(IWeekendDefinitionProvider)" />.
    /// </para>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// IWeekendDefinitionProvider provider = new MyCustomWeekend();
    /// WeekPattern workingWeek = provider.ToWeekPattern();
    /// var service = new NotableDateService(ruleProviders, workingWeek);
    ///
    /// // With advanced configuration:
    /// var options = new NotableDateServiceOptions
    /// {
    ///     OverrideProviders = new[] { myOverrideProvider },
    ///     AlgorithmRegistry  = myAlgorithmRegistry,
    /// };
    /// var configured = new NotableDateService(ruleProviders, workingWeek, options);
    ///]]>
    /// </code>
    /// </example>
    /// </remarks>
    public NotableDateService(
        IEnumerable<INotableDateRuleProvider> ruleProviders,
        WeekPattern workingWeek,
        NotableDateServiceOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(ruleProviders);

        NotableDateServiceOptions opts = options ?? new NotableDateServiceOptions();

        // Fan plugin contributions into the provider list and the algorithm registry. The merge order means host-level
        // rule providers are loaded first and therefore win composite-key collisions inside the flatten pipeline, and
        // host-supplied algorithm registrations take precedence over plugin-supplied ones with the same key.
        var effectiveProviders = ruleProviders.ToList();
        INotableDateAlgorithmRegistry? effectiveRegistry = opts.AlgorithmRegistry;

        if (opts.Plugins is not null)
        {
            var pluginAlgorithms = new List<KeyValuePair<string, INotableDateAlgorithm>>();

            foreach (INotableDatePlugin plugin in opts.Plugins)
            {
                if (plugin is Plugins.INotableDateRulePlugin rulePlugin)
                {
                    effectiveProviders.AddRange(rulePlugin.GetRuleProviders() ?? []);
                }

                if (plugin is Plugins.INotableDateAlgorithmPlugin calcPlugin)
                {
                    pluginAlgorithms.AddRange(calcPlugin.GetAlgorithms() ?? []);
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

        _baseRules = [.. effectiveProviders
            .SelectMany(p => p.LoadRules() ?? [])
            .Where(r => r is not null)];
        _overrideProviders = opts.OverrideProviders?.ToList() ?? (IReadOnlyList<INotableDateRuleOverrideProvider>)[];

        WorkingWeek = workingWeek;
        _collisionResolver = opts.CollisionResolver ?? new DefaultNotableDateCollisionResolver();
        _nameLocalizer = opts.NameLocalizer;
        _resourcePathResolver = opts.ResourcePathResolver ?? new ResourcePathResolver();
        _algorithmRegistry = effectiveRegistry;
        _adjustmentHandlers = opts.AdjustmentHandlers;

        (_overrideRemovals, _overrideAdditions, _effectiveRules, _resolver) = BuildOverrideState();
    }

    /// <summary>
    /// Re-queries every registered <see cref="INotableDateRuleOverrideProvider" /> and returns the freshly snapshotted
    /// override removals, addition identity set, merged effective rule set, and resolver. Shared by the constructor
    /// and <see cref="Reload" /> so that the two callers cannot drift.
    /// </summary>
    /// <returns>
    /// A tuple comprising the override-derived state needed by the resolution pipeline.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Snapshotting every override provider's contributions in a single pass pins the cost of any non-trivial override
    /// provider (database-backed, configuration-bound, lazily-enumerated) to a single call per provider and removes a
    /// runaway vector for providers that return fresh, infinite, or expensive enumerables on each invocation. Override
    /// additions are tracked by reference identity because <see cref="NotableDateRule" /> is a record with value
    /// equality — <see cref="ReferenceEqualityComparer" /> ensures we only exempt the specific instances contributed
    /// by override providers from same-name <see cref="RuleRemoval" /> suppression.
    /// </para>
    /// </remarks>
    private (IReadOnlyList<RuleRemoval> Removals, HashSet<NotableDateRule> Additions, IReadOnlyList<NotableDateRule> Effective, NotableDateRuleResolver Resolver) BuildOverrideState()
    {
        IReadOnlyList<RuleRemoval> removals = [.. _overrideProviders.SelectMany(p => p.GetRemovals())];
        List<NotableDateRule> additionList = [.. _overrideProviders.SelectMany(p => p.GetAdditions())];
        HashSet<NotableDateRule> additions = new(additionList, ReferenceEqualityComparer.Instance);
        IReadOnlyList<NotableDateRule> effective = ApplyOverrides(_baseRules, additionList);
        NotableDateRuleResolver resolver = new(effective, _algorithmRegistry);

        return (removals, additions, effective, resolver);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateService" /> class using a named
    /// <see cref="WorkingDaysOfWeek" /> preset as the working week.
    /// </summary>
    /// <param name="ruleProviders">Sources of base notable date rules. Must not be <see langword="null" />.</param>
    /// <param name="workingDaysOfWeek">The named working-week pattern used when classifying dates.</param>
    /// <param name="options">Optional service configuration. When <see langword="null" />, defaults apply.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="ruleProviders" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="workingDaysOfWeek" /> is not a defined member of the
    /// <see cref="WorkingDaysOfWeek" /> enumeration.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="workingDaysOfWeek" /> is <see cref="WorkingDaysOfWeek.Custom" />, which has no
    /// canonical pattern.
    /// </exception>
    public NotableDateService(
        IEnumerable<INotableDateRuleProvider> ruleProviders,
        WorkingDaysOfWeek workingDaysOfWeek,
        NotableDateServiceOptions? options = null)
        : this(ruleProviders, workingDaysOfWeek.ToWeekPattern(), options)
    { }

    /// <summary>
    /// Gets the chronological windows that have been resolved by <see cref="ResolveNotableDatesInRange" /> since the
    /// service was constructed or <see cref="Invalidate()" /> was last called. The list is sorted ascending by start
    /// date and contains the minimum number of disjoint intervals describing the same coverage.
    /// </summary>
    /// <returns>
    /// A snapshot of the disjoint <see cref="DateRange" /> instances representing the union of every requested window.
    /// An empty list indicates that no range request has been served yet.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The property reflects the windows the consumer has <em>asked about</em>, not the rule-set's effective range.
    /// Adjacent or overlapping requests are merged. The returned list is a snapshot; subsequent calls to
    /// <see cref="ResolveNotableDatesInRange" /> may extend it.
    /// </para>
    /// </remarks>
    public IReadOnlyList<DateRange> ResolvedWindows
    {
        get
        {
            lock (_resolvedWindowsGate)
            {
                return [.. _resolvedWindows.Ranges];
            }
        }
    }

    // --------------------------------------------------------------------------------------
    // INotableDateService surface
    // --------------------------------------------------------------------------------------

    /// <inheritdoc />
    public WeekPattern WorkingWeek { get; }

    /// <summary>
    /// Releases resources owned by the service. The current implementation has no unmanaged or disposable state to
    /// release; the method is retained on <see cref="IDisposable" /> for forward compatibility and so callers that
    /// own the service can still wrap it in <c>using</c> blocks.
    /// </summary>
    public void Dispose() => GC.SuppressFinalize(this);

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> GetNotableDates(int year, string? territoryCode = null, Type? calendarType = null)
        => ResolveRangeInternal(
            new DateTime(year, 1, 1),
            new DateTime(year, 12, 31),
            filter: null,
            territoryCode,
            calendarType,
            recordWindow: true);

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> GetNotableDates(DateTime date, string? territoryCode = null, Type? calendarType = null)
        => ResolveRangeInternal(
            date.Date,
            date.Date,
            filter: null,
            territoryCode,
            calendarType,
            recordWindow: true);

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> GetNotableDates(int year, NotableDateFilter filter, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(filter);

        return ResolveRangeInternal(
            new DateTime(year, 1, 1),
            new DateTime(year, 12, 31),
            filter,
            territoryCode,
            calendarType,
            recordWindow: true);
    }

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> GetNotableDates(DateTime startDate, DateTime endDate, string? territoryCode = null, Type? calendarType = null)
        => ResolveRangeInternal(
            startDate,
            endDate,
            filter: null,
            territoryCode,
            calendarType,
            recordWindow: true);

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> GetNotableDates(DateTime date, NotableDateFilter filter, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(filter);

        return ResolveRangeInternal(
            date.Date,
            date.Date,
            filter,
            territoryCode,
            calendarType,
            recordWindow: true);
    }

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> GetNotableDates(DateTime startDate, DateTime endDate, NotableDateFilter filter, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(filter);

        return ResolveRangeInternal(
            startDate,
            endDate,
            filter,
            territoryCode,
            calendarType,
            recordWindow: true);
    }

    /// <inheritdoc />
    public void Invalidate()
    {
        lock (_gate)
        {
            InvalidateCachesUnderGate();
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// The legacy per-year cache that this overload targeted has been retired. The current implementation delegates
    /// to <see cref="Invalidate()" /> — selective per-year invalidation no longer exists because the range pipeline
    /// caches per request rather than per civil year.
    /// </para>
    /// </remarks>
    public void Invalidate(int year) => Invalidate();

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Re-snapshots every registered <see cref="INotableDateRuleOverrideProvider" /> by calling
    /// <see cref="INotableDateRuleOverrideProvider.GetAdditions" /> and
    /// <see cref="INotableDateRuleOverrideProvider.GetRemovals" />, rebuilds the merged effective rule set, recreates
    /// the resolver, and clears all cached year results. Base <see cref="INotableDateRuleProvider" /> sources are not
    /// re-queried; their contribution is fixed for the lifetime of the service.
    /// </para>
    /// <para>
    /// The effective-rule rebuild and the cache/pipeline invalidation are performed atomically under a single
    /// <c>_gate</c> critical section so that no concurrent reader can observe new effective rules paired with a stale
    /// year cache or a stale range pipeline. Resolved-window state is cleared identically to <see cref="Invalidate()" />.
    /// </para>
    /// </remarks>
    public void Reload()
    {
        lock (_gate)
        {
            (_overrideRemovals, _overrideAdditions, _effectiveRules, _resolver) = BuildOverrideState();
            InvalidateCachesUnderGate();
        }
    }

    /// <summary>
    /// Drops the lazy range pipeline and resets the resolved-window set. Must be invoked while holding
    /// <see cref="_gate" /> so the reset is coherent with any concurrent effective-rule rebuild.
    /// </summary>
    private void InvalidateCachesUnderGate()
    {
        _rangePipeline = null;
        lock (_resolvedWindowsGate)
        {
            _resolvedWindows.Clear();
        }
    }

    /// <summary>
    /// Returns the lazily constructed <see cref="RangeResolution.NotableDateRangePipeline" />, building it under
    /// <see cref="_gate" /> on first access so it cannot be assembled from rules that <see cref="Reload" /> has already
    /// swapped out.
    /// </summary>
    /// <returns>The current range pipeline, coherent with the effective rule set at the moment of construction.</returns>
    private RangeResolution.NotableDateRangePipeline GetOrBuildRangePipeline()
    {
        RangeResolution.NotableDateRangePipeline? snapshot = _rangePipeline;
        if (snapshot is not null)
            return snapshot;

        lock (_gate)
        {
            return _rangePipeline ??= BuildRangePipeline();
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Projects the effective rule set's <see cref="NotableDateRule.TerritoryCode" /> values through case-insensitive
    /// distinctness, eliding rules whose territory is <see langword="null" />, empty, or whitespace. Returned codes
    /// preserve their authored casing.
    /// </para>
    /// </remarks>
    public IReadOnlyCollection<string> GetSupportedTerritories()
    {
        IReadOnlyList<NotableDateRule> snapshot = _effectiveRules;
        SortedSet<string> territories = new(StringComparer.OrdinalIgnoreCase);

        foreach (NotableDateRule rule in snapshot)
        {
            if (!string.IsNullOrWhiteSpace(rule.TerritoryCode))
            {
                _ = territories.Add(rule.TerritoryCode);
            }
        }

        return territories;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Projects the effective rule set's <see cref="NotableDateRule.CalendarType" /> values through reference
    /// distinctness, eliding rules whose calendar is <see langword="null" /> (global / unscoped rules).
    /// </para>
    /// </remarks>
    public IReadOnlyCollection<Type> GetSupportedCalendars()
    {
        IReadOnlyList<NotableDateRule> snapshot = _effectiveRules;
        HashSet<Type> calendars = [];

        foreach (NotableDateRule rule in snapshot)
        {
            if (rule.CalendarType is not null)
            {
                _ = calendars.Add(rule.CalendarType);
            }
        }

        return calendars;
    }

    /// <inheritdoc />
    public bool IsHolidayNonWorkingDay(DateTime date, string? territoryCode = null, Type? calendarType = null)
    {
        IReadOnlyList<NotableDate> sameDay = ResolveRangeInternal(
            date.Date,
            date.Date,
            filter: null,
            territoryCode,
            calendarType,
            recordWindow: false);

        foreach (NotableDate notable in sameDay)
        {
            if (notable.IsNonWorkingDay && ContainsDay(notable, date.Date))
                return true;
        }

        return false;
    }

    /// <inheritdoc />
    public bool IsNonWorkingDay(DateTime date, string? territoryCode = null, Type? calendarType = null) =>
        IsWeekend(date) || IsHolidayNonWorkingDay(date, territoryCode, calendarType);

    /// <summary>
    /// Determines whether the supplied chronological range has already been resolved in its entirety by a previous call
    /// to <see cref="ResolveNotableDatesInRange" />.
    /// </summary>
    /// <param name="startDate">The inclusive start of the range to test.</param>
    /// <param name="endDate">
    /// The inclusive end of the range to test. Must not be earlier than <paramref name="startDate" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when every day in the supplied range is covered by a single resolved window; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="endDate" /> is earlier than <paramref name="startDate" />.
    /// </exception>
    public bool IsRangeResolved(DateTime startDate, DateTime endDate)
    {
        if (endDate < startDate)
            throw new ArgumentException(CalendarResourceStrings.Arg_Invalid_EndDateBeforeStartDate, nameof(endDate));

        DateRange probe = new(startDate.Date, endDate.Date);

        lock (_resolvedWindowsGate)
        {
            return _resolvedWindows.Covers(probe);
        }
    }

    /// <inheritdoc />
    public bool IsWeekend(DateTime date) => !WorkingWeek.Contains(date.DayOfWeek);

    /// <summary>
    /// Resolves notable dates whose observed date intersects the supplied chronological window using the
    /// range-resolution pipeline. Collateral dates that originate outside the requested range are admitted when an
    /// observance adjustment rolls them into the window or when an offset rule projects an out-of-window anchor date
    /// inside.
    /// </summary>
    /// <param name="startDate">The inclusive start date of the requested window.</param>
    /// <param name="endDate">
    /// The inclusive end date of the requested window. Must not be earlier than <paramref name="startDate" />.
    /// </param>
    /// <param name="territoryCode">The optional territory context.</param>
    /// <param name="calendarType">The optional calendar context.</param>
    /// <param name="filter">The optional notable-date filter.</param>
    /// <returns>The resolved notable dates ordered by observed date.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="endDate" /> is earlier than <paramref name="startDate" />.
    /// </exception>
    public IReadOnlyList<NotableDate> ResolveNotableDatesInRange(
        DateTime startDate,
        DateTime endDate,
        string? territoryCode = null,
        Type? calendarType = null,
        NotableDateFilter? filter = null)
        => ResolveRangeInternal(startDate, endDate, filter, territoryCode, calendarType, recordWindow: true);

    /// <summary>
    /// Resolves notable dates for the supplied window through the range pipeline, optionally recording the requested
    /// window in <see cref="_resolvedWindows" />. This is the canonical implementation shared by
    /// <see cref="ResolveNotableDatesInRange" /> and the legacy <c>GetNotableDates</c> overloads.
    /// </summary>
    /// <param name="startDate">The inclusive start date.</param>
    /// <param name="endDate">The inclusive end date.</param>
    /// <param name="filter">The optional filter applied during pipeline resolution.</param>
    /// <param name="territoryCode">The optional territory context.</param>
    /// <param name="calendarType">The optional calendar context.</param>
    /// <param name="recordWindow">
    /// <see langword="true" /> to add the request window to <see cref="_resolvedWindows" />; <see langword="false" />
    /// for hot-path predicate queries (for example single-day non-working checks) that should not extend the resolved
    /// coverage set.
    /// </param>
    /// <returns>The resolved notable dates ordered by observed date.</returns>
    private IReadOnlyList<NotableDate> ResolveRangeInternal(
        DateTime startDate,
        DateTime endDate,
        NotableDateFilter? filter,
        string? territoryCode,
        Type? calendarType,
        bool recordWindow)
    {
        RangeResolution.NotableDateRangeRequest request = new(
            startDate,
            endDate,
            territoryCode,
            calendarType,
            filter);

        RangeResolution.NotableDateRangePipeline pipeline = GetOrBuildRangePipeline();
        IReadOnlyList<NotableDate> resolved = pipeline.Resolve(request);

        if (recordWindow)
        {
            lock (_resolvedWindowsGate)
            {
                _resolvedWindows.Add(new DateRange(request.StartDate, request.EndDate));
            }
        }

        List<NotableDate> localized = new(resolved.Count);
        foreach (NotableDate notable in resolved)
            localized.Add(LocaliseIfNeeded(notable));

        return [.. localized
            .GroupBy(n => n.Date.Date)
            .OrderBy(g => g.Key)
            .SelectMany(g => _collisionResolver.Resolve(g.Key, [.. g]) ?? [])];
    }

    /// <summary>
    /// Applies the pre-materialised list of override additions to the base rule set, producing the merged effective
    /// rule set.
    /// </summary>
    /// <param name="baseRules">The base set of rules to be overridden.</param>
    /// <param name="overrideAdditions">
    /// The materialised list of additions contributed by every configured override provider.
    /// </param>
    /// <returns>The rule list after all additions have been applied.</returns>
    /// <remarks>
    /// Additions are layered on top of the base rule set using a composite (name, territory) key so that regional
    /// variants of the same notable date (for example, multiple Labour Day variants across Australian states) survive
    /// instead of collapsing into a single entry. Removals are evaluated per (year, territory) downstream so they can
    /// be scoped to specific years and territories.
    /// </remarks>
    private static ImmutableArray<NotableDateRule> ApplyOverrides(
        ImmutableArray<NotableDateRule> baseRules,
        List<NotableDateRule> overrideAdditions)
    {
        if (overrideAdditions.Count == 0)
            return baseRules.IsDefault ? [] : baseRules;

        IEnumerable<NotableDateRule> source = baseRules.IsDefault ? Enumerable.Empty<NotableDateRule>() : baseRules;

        var byKey = new Dictionary<(string Name, string Territory), NotableDateRule>();
        foreach (NotableDateRule rule in source)
        {
            byKey[CompositeKey(rule)] = rule;
        }

        foreach (NotableDateRule addition in overrideAdditions)
        {
            byKey[CompositeKey(addition)] = addition;
        }

        return [.. byKey.Values];
    }

    /// <summary>
    /// Creates the composite rule identity used when merging base rules with override additions.
    /// </summary>
    /// <param name="rule">The notable-date rule whose identity should be calculated.</param>
    /// <returns>A tuple containing the normalized rule name and territory code used as the dictionary key.</returns>
    /// <remarks>
    /// Rules are keyed by both name and territory so that regional variants of the same named date remain distinct
    /// during override application. A <see langword="null" /> name or territory is normalized to
    /// <see cref="string.Empty" /> so the key can be used directly in dictionaries.
    /// </remarks>
    private static (string Name, string Territory) CompositeKey(NotableDateRule rule)
        => (rule.Name ?? string.Empty, rule.TerritoryCode ?? string.Empty);

    /// <summary>
    /// Returns <see langword="true" /> if <paramref name="notable" /> covers the calendar day of
    /// <paramref name="day" />, ignoring the time component.
    /// </summary>
    /// <param name="notable">The notable date.</param>
    /// <param name="day">The day under test.</param>
    /// <returns><see langword="true" /> if the day is covered.</returns>
    private static bool ContainsDay(NotableDate notable, DateTime day)
        => day >= notable.Date.Date && day <= notable.EndDate.Date;

    /// <summary>
    /// Constructs the prototype range-resolution pipeline using the service's effective rule set, resolver, and weekend
    /// / handler configuration.
    /// </summary>
    /// <returns>The constructed pipeline.</returns>
    private RangeResolution.NotableDateRangePipeline BuildRangePipeline()
    {
        var analysis = RangeResolution.RuleStaticAnalysis.Build(_effectiveRules);

        return new RangeResolution.NotableDateRangePipeline(
            analysis,
            _resolver,
            WorkingWeek,
            _adjustmentHandlers,
            _overrideRemovals,
            _overrideAdditions);
    }

    /// <summary>
    /// If a name-localizer is configured, replaces the name on <paramref name="notable" /> with its localized form;
    /// otherwise returns <paramref name="notable" /> unchanged.
    /// </summary>
    /// <param name="notable">The notable date to potentially localize.</param>
    /// <returns>The localized or original notable date.</returns>
    private NotableDate LocaliseIfNeeded(NotableDate notable)
    {
        if (_nameLocalizer is null)
            return notable;

        var localized = _nameLocalizer.GetDisplayName(notable, CultureInfo.CurrentCulture);

        return string.IsNullOrEmpty(localized) || string.Equals(localized, notable.Name, StringComparison.Ordinal)
            ? notable
            : notable with { Name = localized };
    }

    /// <summary>
    /// Layers two <see cref="INotableDateAlgorithmRegistry" /> instances: <c>primary</c> is consulted first; on a miss,
    /// <c>fallback</c> is consulted. Used to compose host-supplied algorithms with plugin-supplied ones so the host
    /// retains precedence on key collisions.
    /// </summary>
    private sealed class CompositeAlgorithmRegistry : INotableDateAlgorithmRegistry
    {
        /// <summary>
        /// The plugin-supplied registry consulted when <see cref="_primary" /> does not contain the requested key.
        /// </summary>
        private readonly INotableDateAlgorithmRegistry _fallback;

        /// <summary>
        /// The host-supplied registry consulted first; its registrations take precedence on key collisions.
        /// </summary>
        private readonly INotableDateAlgorithmRegistry _primary;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeAlgorithmRegistry" /> class.
        /// </summary>
        /// <param name="primary">The primary (host) registry, consulted first.</param>
        /// <param name="fallback">The fallback (plugin) registry, consulted on primary misses.</param>
        public CompositeAlgorithmRegistry(INotableDateAlgorithmRegistry primary, INotableDateAlgorithmRegistry fallback)
        {
            _primary = primary;
            _fallback = fallback;
        }

        /// <inheritdoc />
        public bool Contains(string key) => _primary.Contains(key) || _fallback.Contains(key);

        /// <inheritdoc />
        public bool TryGet(string key, out INotableDateAlgorithm algorithm) =>
             _primary.TryGet(key, out algorithm!) || _fallback.TryGet(key, out algorithm!);
    }
}

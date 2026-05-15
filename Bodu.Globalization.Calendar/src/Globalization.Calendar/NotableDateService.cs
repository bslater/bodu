// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateService.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
/// <see cref="INotableDateRuleProvider" /> sources with optional <see cref="INotableDateRuleOverrideProvider" /> layers and producing
/// resolved <see cref="NotableDate" /> instances on demand.
/// </summary>
/// <remarks>
/// <para>
/// This type is the orchestrator at the centre of the resolution pipeline. The pipeline reads
/// <c>Rule source → NotableDateRule → Resolution strategy → Nominal date → Adjustment pipeline → Resolved NotableDate → Consumer query</c>;
/// the documentation introduction (<c>docs/calendar/index.md</c>) and the concepts page
/// (<c>docs/calendar/concepts.md</c>) render this flow as a diagram and provide the surrounding vocabulary, and the
/// <c>guides/calendar/*</c> walk-throughs cover authoring rules, working-day arithmetic, observance adjustments, territories,
/// and data packs.
/// </para>
/// <para>
/// The service caches resolved notable dates per year. Years are generated lazily on first access and cleared via
/// <see cref="Invalidate()" /> or <see cref="Invalidate(int)" />. The cache is thread-safe under concurrent reads and writes.
/// </para>
/// <para>
/// Multi-day events are supported: a <see cref="NotableDateRule" /> with <see cref="NotableDateRule.DurationDays" /> greater than one
/// produces a single <see cref="NotableDate" /> whose <see cref="NotableDate.EndDate" /> is the inclusive last day of the span. Range
/// and single-day queries return the span when any day within it intersects the query.
/// </para>
/// <para>
/// The default no-argument constructor loads only the embedded minimal default rule set (currently New Year's Day) so a service
/// can be created without referencing any companion data pack. Region-specific public holidays — US federal holidays, UK bank
/// holidays, Australian state observances, and so on — ship in separate <c>Bodu.Globalization.Calendar.Data.*</c> assemblies and
/// are added by passing the pack's <c>CreateProvider()</c> result through the full constructor. The full constructor accepts
/// base rule providers, weekend definitions, override providers, algorithm registries, custom adjustment handlers, collision
/// resolvers, and name localizers, enabling complete control over the resolution pipeline.
/// </para>
/// <para>
/// Pipeline stage cross-reference:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       <b>Rule source</b> — supplied via the <c>ruleProviders</c> constructor parameter using
///       <see cref="INotableDateRuleProvider" /> implementations such as <see cref="XmlResourceNotableDateRuleProvider" /> or
///       <see cref="JsonResourceNotableDateRuleProvider" />, or via companion <c>Bodu.Globalization.Calendar.Data.*</c> packs.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>NotableDateRule → Resolution strategy</b> — each <see cref="NotableDateRule" /> declares a
///       <see cref="DateResolutionStrategy" /> (<c>Fixed</c>, <c>DayOfWeekInMonth</c>, <c>OffsetFromAnchor</c>, or
///       <c>Algorithm</c>) that the resolver dispatches to per year, producing the <i>nominal</i> date.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Adjustment pipeline</b> — <see cref="ObservanceAdjustment" /> entries on the rule fire through registered
///       <see cref="IAdjustmentHandler" /> instances (managed by <see cref="AdjustmentHandlerRegistry" />), yielding the
///       <i>observed</i> date and populating <see cref="NotableDate.WasAdjusted" /> / <see cref="NotableDate.AdjustmentReason" />.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Resolved NotableDate</b> — an immutable <see cref="NotableDate" /> is cached per year. <see cref="Invalidate()" />
///       drops all years; <see cref="Invalidate(int)" /> drops a single year.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Consumer query</b> — <c>GetNotableDates</c> and its overloads return the resolved set;
///       <see cref="NotableDateFilter" /> composes territory / category / tag predicates;
///       <c>Bodu.Extensions.NotableDateOnlyExtensions</c> and <c>NotableDateTimeExtensions</c> provide working-day arithmetic
///       (<c>IsWorkingDay</c>, <c>AddWorkingDays</c>, <c>NextWorkingDay</c>, …).
///     </description>
///   </item>
/// </list>
/// </remarks>
/// <example>
/// <para>Construct with the built-in minimal rule set, then layer Australian public holidays from a companion data pack:</para>
/// <code>
/// // Simplest construction — loads only the embedded New Year's Day rule:
/// NotableDateService service = new NotableDateService();
///
/// // All notable dates for New South Wales in 2026 (requires the Asia-Pacific data pack):
/// IReadOnlyList&lt;NotableDate&gt; dates = service.GetNotableDates(2026, territoryCode: "AU-NSW");
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
/// </code>
/// </example>
/// <seealso cref="INotableDateService" />
/// <seealso cref="NotableDateRule" />
/// <seealso cref="NotableDate" />
/// <seealso cref="NotableDateFilter" />
/// <seealso cref="ObservanceAdjustment" />
/// <seealso cref="INotableDateRuleProvider" />
/// <seealso cref="INotableDateRuleOverrideProvider" />
/// <seealso cref="NotableDateServiceOptions" />
public sealed class NotableDateService
    : INotableDateService
    , IDisposable
{
    /// <summary>The embedded resource path for the minimal default rule set used by the parameterless constructor.</summary>
    private const string DefaultResourceName = "Bodu/Globalization/Calendar/Resources/default-minimal.xml";

    /// <summary>The immutable snapshot of base rules loaded at construction from all rule providers.</summary>
    private readonly ImmutableArray<NotableDateRule> _baseRules;

    /// <summary>The ordered list of override providers applied on top of the base rule set.</summary>
    private readonly IReadOnlyList<INotableDateRuleOverrideProvider> _overrideProviders;

    /// <summary>Snapshot of all override removals, materialized once at construction to avoid re-querying providers per year.</summary>
    private readonly IReadOnlyList<RuleRemoval> _overrideRemovals;

    /// <summary>
    /// Identity-keyed set of every rule contributed by an <see cref="INotableDateRuleOverrideProvider" /> addition.
    /// Used by <see cref="IsRemovedByOverride" /> to exempt override additions from same-name <see cref="RuleRemoval" />
    /// suppression, so an addition can replace a removed base rule of the same name.
    /// </summary>
    private readonly HashSet<NotableDateRule> _overrideAdditions;

    /// <summary>The merged rule set after all overrides have been applied; drives every resolution pass.</summary>
    private readonly IReadOnlyList<NotableDateRule> _effectiveRules;

    /// <summary>The resolver that turns each rule into an anchor date for a given year.</summary>
    private readonly NotableDateRuleResolver _resolver;

    /// <summary>The optional registry of custom adjustment handlers used during generation.</summary>
    private readonly IAdjustmentHandlerRegistry? _adjustmentHandlers;

    /// <summary>The chronological windows resolved by <see cref="ResolveNotableDatesInRange" /> since the last <see cref="Invalidate()" /> call.</summary>
    private readonly RangeResolution.ResolvedWindowSet _resolvedWindows = new();

    /// <summary>The resolver that arbitrates when multiple rules produce a date on the same day.</summary>
    private readonly INotableDateCollisionResolver _collisionResolver;

    /// <summary>The optional localizer used to translate notable date names into the active culture.</summary>
    private readonly INotableDateNameLocalizer? _nameLocalizer;

    /// <summary>The resolver used to locate embedded XML resource files.</summary>
    private readonly IResourcePathResolver _resourcePathResolver;

    /// <summary>Thread-safe per-year cache of fully resolved notable dates.</summary>
    private readonly ConcurrentDictionary<int, IReadOnlyList<NotableDate>> _yearCache = new();

    /// <summary>Lock protecting write access to <see cref="_yearCache" /> during first-time year generation.</summary>
    private readonly object _gate = new();

    /// <summary>Lock protecting concurrent updates to <see cref="_resolvedWindows" />.</summary>
    private readonly object _resolvedWindowsGate = new();

    // Per-thread set of years currently being generated on this thread. Used by GetOrGenerateYear to short-circuit recursive
    // entry from within GenerateYear (for example, MoveToNextNonWorkingDay's walk calling back through IsNonWorkingDay) so that a
    // single rule cannot cause the generator to stack-overflow by consulting the very year it is in the middle of producing.
    private readonly ThreadLocal<HashSet<int>> _generatingYears = new(() => []);

    /// <summary>The lazily constructed prototype range-resolution pipeline used by <see cref="ResolveNotableDatesInRange" />.</summary>
    private RangeResolution.NotableDateRangePipeline? _rangePipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateService" /> class using the embedded minimal default rule set
    /// (currently a single rule for New Year's Day). Region-specific holidays must be supplied via the full constructor by passing
    /// providers from the <c>Bodu.Globalization.Calendar.Data.*</c> companion assemblies.
    /// </summary>
    public NotableDateService()
        : this(
        [
            new XmlResourceNotableDateRuleProvider(DefaultResourceName, new ResourcePathResolver())
        ],
        WeekPattern.MondayToFriday)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateService" /> class using the embedded minimal default rule set and
    /// the supplied <see cref="WeekPattern" /> working week.
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
    /// Initializes a new instance of the <see cref="NotableDateService" /> class using the embedded minimal default rule set and
    /// the supplied named <see cref="WorkingDaysOfWeek" /> working week.
    /// </summary>
    /// <param name="workingDaysOfWeek">The named working-week pattern used when classifying dates.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="workingDaysOfWeek" /> is not a defined member of the <see cref="WorkingDaysOfWeek" /> enumeration.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="workingDaysOfWeek" /> is <see cref="WorkingDaysOfWeek.Custom" />, which has no canonical pattern.</exception>
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ruleProviders" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// To use a custom weekend supplied by an <see cref="IWeekendDefinitionProvider" />, convert it to a
    /// <see cref="WeekPattern" /> first via
    /// <see cref="Bodu.Extensions.IWeekendDefinitionProviderExtensions.ToWeekPattern(IWeekendDefinitionProvider)" />.
    /// </para>
    /// <example>
    /// <code language="csharp">
    /// <![CDATA[
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
    /// ]]>
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

        // Snapshot every override provider's contributions at construction so that downstream lookups iterate a materialized
        // list once per rule × year × territory rather than re-invoking GetRemovals / GetAdditions on every check. This pins
        // the cost of any non-trivial override provider (database-backed, configuration-bound, lazily-enumerated) to a single
        // call per provider and removes a runaway vector for providers that return fresh, infinite, or expensive enumerables
        // on each invocation. Materialise additions once into a list so the same instances are reused by ApplyOverrides and
        // by the addition-identity set below.
        _overrideRemovals = [.. _overrideProviders.SelectMany(p => p.GetRemovals())];
        List<NotableDateRule> overrideAdditionList = [.. _overrideProviders.SelectMany(p => p.GetAdditions())];

        // Track the addition instances by reference identity. NotableDateRule is a record with value equality, so a HashSet
        // keyed on its default equality would treat two unrelated rules with identical property values as the same entry —
        // ReferenceEqualityComparer ensures we only exempt the specific instances contributed by override providers.
        _overrideAdditions = new HashSet<NotableDateRule>(overrideAdditionList, ReferenceEqualityComparer.Instance);

        WorkingWeek = workingWeek;
        _collisionResolver = opts.CollisionResolver ?? new DefaultNotableDateCollisionResolver();
        _nameLocalizer = opts.NameLocalizer;
        _resourcePathResolver = opts.ResourcePathResolver ?? new ResourcePathResolver();

        _effectiveRules = ApplyOverrides(_baseRules, overrideAdditionList);
        _resolver = new NotableDateRuleResolver(_effectiveRules, effectiveRegistry);
        _adjustmentHandlers = opts.AdjustmentHandlers;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateService" /> class using a named
    /// <see cref="WorkingDaysOfWeek" /> preset as the working week.
    /// </summary>
    /// <param name="ruleProviders">Sources of base notable date rules. Must not be <see langword="null" />.</param>
    /// <param name="workingDaysOfWeek">The named working-week pattern used when classifying dates.</param>
    /// <param name="options">Optional service configuration. When <see langword="null" />, defaults apply.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ruleProviders" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="workingDaysOfWeek" /> is not a defined member of the <see cref="WorkingDaysOfWeek" /> enumeration.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="workingDaysOfWeek" /> is <see cref="WorkingDaysOfWeek.Custom" />, which has no canonical pattern.</exception>
    public NotableDateService(
        IEnumerable<INotableDateRuleProvider> ruleProviders,
        WorkingDaysOfWeek workingDaysOfWeek,
        NotableDateServiceOptions? options = null)
        : this(ruleProviders, workingDaysOfWeek.ToWeekPattern(), options)
    { }

    // --------------------------------------------------------------------------------------
    // INotableDateService surface
    // --------------------------------------------------------------------------------------

    /// <inheritdoc />
    public WeekPattern WorkingWeek { get; }

    /// <summary>
    /// Gets the chronological windows that have been resolved by <see cref="ResolveNotableDatesInRange" /> since the service was
    /// constructed or <see cref="Invalidate()" /> was last called. The list is sorted ascending by start date and contains the
    /// minimum number of disjoint intervals describing the same coverage.
    /// </summary>
    /// <returns>
    /// A snapshot of the disjoint <see cref="DateRange" /> instances representing the union of every requested window. An empty
    /// list indicates that no range request has been served yet.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The property reflects the windows the consumer has <em>asked about</em>, not the rule-set's effective range. Adjacent or
    /// overlapping requests are merged. The returned list is a snapshot; subsequent calls to
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

    /// <inheritdoc />
    public bool IsWeekend(DateTime date) => !WorkingWeek.Contains(date.DayOfWeek);

    /// <inheritdoc />
    public bool IsNonWorkingDay(DateTime date, string? territoryCode = null, Type? calendarType = null)
    {
        if (IsWeekend(date)) return true;

        return IsHolidayNonWorkingDay(date, territoryCode, calendarType);
    }

    /// <inheritdoc />
    public bool IsHolidayNonWorkingDay(DateTime date, string? territoryCode = null, Type? calendarType = null)
    {
        IReadOnlyList<NotableDate> perYear = GetOrGenerateYear(date.Year);
        foreach (NotableDate notable in perYear)
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

        List<NotableDate> results = [];
        for (var year = startDate.Year; year <= endDate.Year; year++)
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
        return [.. results
            .GroupBy(n => n.Date.Date)
            .OrderBy(g => g.Key)
            .SelectMany(g => _collisionResolver.Resolve(g.Key, [.. g]) ?? [])];
    }

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> GetNotableDates(DateTime startDate, DateTime endDate, NotableDateFilter filter, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(filter);

        if (endDate < startDate) (startDate, endDate) = (endDate, startDate);

        List<NotableDate> results = [];
        for (var year = startDate.Year; year <= endDate.Year; year++)
        {
            IReadOnlyList<NotableDate> perYear = GenerateYearFiltered(year, filter);
            foreach (NotableDate notable in perYear)
            {
                if (notable.EndDate < startDate.Date || notable.Date > endDate.Date) continue;
                if (!MatchesContext(notable, territoryCode, calendarType)) continue;

                results.Add(LocaliseIfNeeded(notable));
            }
        }

        return [.. results
            .GroupBy(n => n.Date.Date)
            .OrderBy(g => g.Key)
            .SelectMany(g => _collisionResolver.Resolve(g.Key, [.. g]) ?? [])];
    }

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> GetNotableDates(DateTime date, string? territoryCode = null, Type? calendarType = null)
    {
        List<NotableDate> results = [];

        // Multi-day spans may have started in the previous year; check both years to cover wrap-around.
        foreach (var year in new[] { date.Year - 1, date.Year })
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

        return _collisionResolver.Resolve(date.Date, results) ?? [];
    }

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> GetNotableDates(DateTime date, NotableDateFilter filter, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(filter);

        List<NotableDate> results = [];

        // Multi-day spans may have started in the previous year; check both years to cover wrap-around.
        foreach (var year in new[] { date.Year - 1, date.Year })
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

        return _collisionResolver.Resolve(date.Date, results) ?? [];
    }

    /// <inheritdoc />
    public void Invalidate()
    {
        _yearCache.Clear();
        _rangePipeline = null;
        lock (_resolvedWindowsGate)
        {
            _resolvedWindows.Clear();
        }
    }

    /// <inheritdoc />
    public void Invalidate(int year) => _yearCache.TryRemove(year, out _);

    /// <summary>
    /// Resolves notable dates whose observed date intersects the supplied chronological window using the prototype range-resolution
    /// pipeline. Collateral dates that originate outside the requested range are admitted when an observance adjustment rolls them
    /// into the window or when an offset rule projects an out-of-window anchor date inside.
    /// </summary>
    /// <param name="startDate">The inclusive start date of the requested window.</param>
    /// <param name="endDate">The inclusive end date of the requested window. Must not be earlier than <paramref name="startDate" />.</param>
    /// <param name="territoryCode">The optional territory context.</param>
    /// <param name="calendarType">The optional calendar context.</param>
    /// <param name="filter">The optional notable-date filter.</param>
    /// <returns>The resolved notable dates ordered by observed date.</returns>
    /// <exception cref="ArgumentException"><paramref name="endDate" /> is earlier than <paramref name="startDate" />.</exception>
    /// <remarks>
    /// <para>
    /// TODO: Once the prototype is validated against the existing window-based overloads, evaluate promoting this method to
    /// <see cref="INotableDateService" /> and routing the existing
    /// <see cref="GetNotableDates(DateTime, DateTime, string?, Type?)" /> and
    /// <see cref="GetNotableDates(DateTime, DateTime, NotableDateFilter, string?, Type?)" /> overloads through the new pipeline.
    /// </para>
    /// </remarks>
    public IReadOnlyList<NotableDate> ResolveNotableDatesInRange(
        DateTime startDate,
        DateTime endDate,
        string? territoryCode = null,
        Type? calendarType = null,
        NotableDateFilter? filter = null)
    {
        RangeResolution.NotableDateRangeRequest request = new(
            startDate,
            endDate,
            territoryCode,
            calendarType,
            filter);

        RangeResolution.NotableDateRangePipeline pipeline = _rangePipeline ??= BuildRangePipeline();
        IReadOnlyList<NotableDate> resolved = pipeline.Resolve(request);

        // Record the request window so consumers can introspect what the service has been asked to resolve. The window is recorded
        // regardless of whether a filter was applied — it represents queried coverage, not result coverage. Filtered queries
        // therefore still extend the known window.
        lock (_resolvedWindowsGate)
        {
            _resolvedWindows.Add(new DateRange(request.StartDate, request.EndDate));
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
    /// Determines whether the supplied chronological range has already been resolved in its entirety by a previous call to
    /// <see cref="ResolveNotableDatesInRange" />.
    /// </summary>
    /// <param name="startDate">The inclusive start of the range to test.</param>
    /// <param name="endDate">The inclusive end of the range to test. Must not be earlier than <paramref name="startDate" />.</param>
    /// <returns><see langword="true" /> when every day in the supplied range is covered by a single resolved window; otherwise, <see langword="false" />.</returns>
    public bool IsRangeResolved(DateTime startDate, DateTime endDate)
    {
        DateRange probe = new(startDate.Date, endDate.Date);

        lock (_resolvedWindowsGate)
        {
            return _resolvedWindows.Covers(probe);
        }
    }

    /// <summary>
    /// Releases resources owned by the service.
    /// </summary>
    public void Dispose()
    {
        _generatingYears.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Constructs the prototype range-resolution pipeline using the service's effective rule set, resolver, and weekend / handler
    /// configuration.
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
    /// Orders base occurrences for deterministic adjustment evaluation.
    /// </summary>
    /// <param name="occurrences">The base occurrences to order.</param>
    /// <returns>The ordered occurrences.</returns>
    private static IEnumerable<ResolvedNotableDateOccurrence> OrderForAdjustment(IEnumerable<ResolvedNotableDateOccurrence> occurrences)
    {
        return occurrences
            .OrderBy(occurrence => occurrence.AnchorDate)
            .ThenBy(occurrence => occurrence.Rule.Priority)
            .ThenBy(occurrence => occurrence.Rule.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(occurrence => occurrence.TerritoryCode, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns the cached per-year notable-date list for <paramref name="year" />, generating
    /// and caching it on first access.
    /// </summary>
    /// <param name="year">The civil year to resolve.</param>
    /// <returns>The notable dates for the specified year.</returns>
    private IReadOnlyList<NotableDate> GetOrGenerateYear(int year)
    {
        if (_yearCache.TryGetValue(year, out IReadOnlyList<NotableDate>? cached))
            return cached;

        // Re-entry guard: if this thread is already generating any year higher up the call stack, do not start another generation
        // pass. MoveToNextNonWorkingDay's bounded walk calls back through IsNonWorkingDay → GetOrGenerateYear; without the guard
        // it recurses for the originating year (same-year re-entry) and, when the walk crosses a year boundary, would otherwise
        // open a fresh generation for the next year, leading to year-by-year recursion until DateTime overflows. Returning an
        // empty snapshot for any cache-miss query during nested generation collapses both vectors: dependent predicates see only
        // what is already cached, the bounded walk falls through its 366-iteration cap, and only the outer caller fully
        // materializes a year.
        HashSet<int> inProgress = _generatingYears.Value!;
        if (inProgress.Count > 0)
            return [];

        lock (_gate)
        {
            if (_yearCache.TryGetValue(year, out cached))
                return cached;

            inProgress.Add(year);
            try
            {
                IReadOnlyList<NotableDate> generated = GenerateYear(year);
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
    /// Materializes the notable dates for <paramref name="year" /> by resolving every configured rule, adding all base dates
    /// to a generation-local context, and then applying observance adjustments against that completed base-date view.
    /// </summary>
    /// <param name="year">The civil year to generate.</param>
    /// <returns>The notable dates for <paramref name="year" />, in unspecified order.</returns>
    private IReadOnlyList<NotableDate> GenerateYear(int year) => GenerateYearCore(year);

    /// <summary>
    /// Materializes the notable dates for <paramref name="year" /> and returns only those that satisfy
    /// <paramref name="filter" />.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Filtered generation intentionally resolves the complete year before applying the date-level filter. This ensures filtered
    /// queries compute the same adjusted dates as unfiltered queries; otherwise, a primary rule filter could exclude a blocking
    /// non-working holiday from the generation context and change substitute-day calculation.
    /// </para>
    /// </remarks>
    /// <param name="year">The civil year to generate.</param>
    /// <param name="filter">The filter to apply to the generated dates.</param>
    /// <returns>The filtered notable dates for <paramref name="year" />, in unspecified order.</returns>
    private IReadOnlyList<NotableDate> GenerateYearFiltered(int year, NotableDateFilter filter) => [.. GenerateYearCore(year).Where(filter.IsMatch)];

    /// <summary>
    /// Generates the complete notable-date set for a year using a generation-local context.
    /// </summary>
    /// <param name="year">The civil year to generate.</param>
    /// <returns>The generated notable dates.</returns>
    private IReadOnlyList<NotableDate> GenerateYearCore(int year)
    {
        List<ResolvedNotableDateOccurrence> occurrences = ResolveBaseOccurrences(year);

        NotableDateGenerationContext context = new();

        foreach (ResolvedNotableDateOccurrence occurrence in occurrences)
        {
            context.Add(occurrence.BaseDate);
        }

        NotableDateAdjuster adjuster = CreateAdjuster(context);

        foreach (ResolvedNotableDateOccurrence occurrence in OrderForAdjustment(occurrences))
        {
            foreach (ObservanceAdjustment adjustment in occurrence.Rule.Adjustments.OrderBy(adjustment => adjustment.Priority))
            {
                if (!NotableDateAdjuster.IsInScope(
                        adjustment,
                        year,
                        occurrence.TerritoryCode,
                        occurrence.Rule.CalendarType))
                {
                    continue;
                }

                AdjustmentApplyResult result = adjuster.Apply(
                    adjustment,
                    occurrence.Rule,
                    occurrence.AnchorDate,
                    occurrence.TerritoryCode,
                    occurrence.Rule.CalendarType);

                if (!result.Activated || result.AdjustedDate.Date == occurrence.AnchorDate.Date)
                    continue;

                var emittedTerritory = !string.IsNullOrEmpty(adjustment.TerritoryCode)
                    ? adjustment.TerritoryCode
                    : occurrence.TerritoryCode;

                var isNonWorking = result.IsNonWorkingOverride ?? occurrence.Rule.IsNonWorkingDay ?? false;

                AdjustmentReason reason = new(
                    occurrence.AnchorDate,
                    result.Trigger,
                    result.Action,
                    result.HandlerKey);

                NotableDate adjustedDate = BuildNotableDate(
                    occurrence.Rule,
                    result.AdjustedDate,
                    emittedTerritory,
                    reason,
                    isNonWorking);

                context.Add(adjustedDate);
            }
        }

        return context.Output;
    }

    /// <summary>
    /// Resolves all base notable-date occurrences for the specified year without applying adjustments.
    /// </summary>
    /// <param name="year">The civil year to resolve.</param>
    /// <returns>The resolved base occurrences.</returns>
    private List<ResolvedNotableDateOccurrence> ResolveBaseOccurrences(int year)
    {
        List<ResolvedNotableDateOccurrence> occurrences = [];

        foreach (NotableDateRule rule in _effectiveRules)
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

                NotableDate baseDate = BuildNotableDate(
                    rule,
                    anchor.Value,
                    territory,
                    adjustmentReason: null);

                occurrences.Add(new ResolvedNotableDateOccurrence(
                    rule,
                    anchor.Value,
                    territory,
                    baseDate));
            }
        }

        return occurrences;
    }

    /// <summary>
    /// Creates an adjustment evaluator bound to the supplied generation context.
    /// </summary>
    /// <param name="context">The generation context.</param>
    /// <returns>An adjustment evaluator that does not call back into the public year cache.</returns>
    private NotableDateAdjuster CreateAdjuster(NotableDateGenerationContext context)
    {
        return new NotableDateAdjuster(
            IsWeekend,
            (date, territoryCode, calendarType) =>
                context.IsNonWorkingDay(date, territoryCode, calendarType, IsWeekend),
            WorkingWeek,
            _adjustmentHandlers,
            (ruleName, year, territoryCode, calendarType) =>
                context.ResolveByName(ruleName, year, territoryCode, calendarType));
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
    /// Applies the pre-materialised list of override additions to the base rule set, producing the merged effective rule set.
    /// </summary>
    /// <param name="baseRules">The base set of rules to be overridden.</param>
    /// <param name="overrideAdditions">The materialised list of additions contributed by every configured override provider.</param>
    /// <returns>The rule list after all additions have been applied.</returns>
    /// <remarks>
    /// Additions are layered on top of the base rule set using a composite (name, territory) key so that regional variants of
    /// the same notable date (for example, multiple Labour Day variants across Australian states) survive instead of collapsing
    /// into a single entry. Removals are evaluated per (year, territory) downstream so they can be scoped to specific years and
    /// territories.
    /// </remarks>
    private static ImmutableArray<NotableDateRule> ApplyOverrides(
        ImmutableArray<NotableDateRule> baseRules,
        List<NotableDateRule> overrideAdditions)
    {
        if (overrideAdditions.Count == 0)
            return baseRules.IsDefault ? [] : baseRules;

        IEnumerable<NotableDateRule> source = baseRules.IsDefault
            ? Enumerable.Empty<NotableDateRule>()
            : baseRules;

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
    /// <returns>
    /// A tuple containing the normalized rule name and territory code used as the dictionary key.
    /// </returns>
    /// <remarks>
    /// Rules are keyed by both name and territory so that regional variants of the same named date
    /// remain distinct during override application. A <see langword="null" /> name or territory is
    /// normalized to <see cref="string.Empty" /> so the key can be used directly in dictionaries.
    /// </remarks>
    private static (string Name, string Territory) CompositeKey(NotableDateRule rule) =>
        (rule.Name ?? string.Empty, rule.TerritoryCode ?? string.Empty);

    /// <summary>
    /// If a name-localizer is configured, replaces the name on <paramref name="notable" /> with
    /// its localized form; otherwise returns <paramref name="notable" /> unchanged.
    /// </summary>
    /// <param name="notable">The notable date to potentially localize.</param>
    /// <returns>The localized or original notable date.</returns>
    private NotableDate LocaliseIfNeeded(NotableDate notable)
    {
        if (_nameLocalizer is null) return notable;

        var localized = _nameLocalizer.GetDisplayName(notable, CultureInfo.CurrentCulture);
        if (string.IsNullOrEmpty(localized) || string.Equals(localized, notable.Name, StringComparison.Ordinal))
            return notable;

        return notable with { Name = localized };
    }

    /// <summary>
    /// Filters the full-year notable-date list for the requested territory and calendar type,
    /// applies localization, and returns the results ordered by observed date.
    /// </summary>
    /// <param name="perYear">The unfiltered notable dates for a single year.</param>
    /// <param name="territoryCode">The territory code filter, or <see langword="null" />.</param>
    /// <param name="calendarType">The calendar type filter, or <see langword="null" />.</param>
    /// <returns>The filtered, localized, and ordered notable-date list.</returns>
    private IReadOnlyList<NotableDate> ProjectAndOrder(IReadOnlyList<NotableDate> perYear, string? territoryCode, Type? calendarType)
    {
        var matching = new List<NotableDate>();
        foreach (NotableDate notable in perYear)
        {
            if (!MatchesContext(notable, territoryCode, calendarType)) continue;
            matching.Add(LocaliseIfNeeded(notable));
        }

        return [.. matching
            .OrderBy(n => n.Date)
            .ThenBy(n => n.Name, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// Returns <see langword="true" /> if <paramref name="rule" /> has been suppressed for the
    /// specified year and territory by the configured override provider.
    /// </summary>
    /// <param name="rule">The candidate rule.</param>
    /// <param name="year">The civil year under evaluation.</param>
    /// <param name="territory">The territory code, or <see langword="null" /> for territory-neutral.</param>
    /// <returns><see langword="true" /> if the rule is removed; otherwise <see langword="false" />.</returns>
    /// <remarks>
    /// Override additions are exempt from removal suppression: a provider that emits both a removal and an addition with the same
    /// rule name is interpreted as a replacement, so the addition surfaces in place of the removed base rule.
    /// </remarks>
    private bool IsRemovedByOverride(NotableDateRule rule, int year, string? territory)
    {
        if (_overrideAdditions.Contains(rule))
            return false;

        foreach (RuleRemoval removal in _overrideRemovals)
        {
            if (!string.Equals(removal.RuleName, rule.Name, StringComparison.OrdinalIgnoreCase))
                continue;

            if (removal.FromYear is { } from && year < from) continue;
            if (removal.ToYear is { } to && year > to) continue;

            if (!string.IsNullOrEmpty(removal.TerritoryCode))
            {
                if (string.IsNullOrEmpty(territory)) continue;
                if (!TerritoryCode.TryParse(removal.TerritoryCode, out TerritoryCode removalScope)) continue;
                if (!TerritoryCode.TryParse(territory, out TerritoryCode actual)) continue;
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

        foreach (TerritoryCode territory in TerritoryCode.ParseList(value))
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

        if (!TerritoryCode.TryParse(territoryCode, out TerritoryCode requested))
            return false;
        if (!TerritoryCode.TryParse(date.TerritoryCode, out TerritoryCode owned))
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
    private static bool ContainsDay(NotableDate notable, DateTime day) => day >= notable.Date.Date && day <= notable.EndDate.Date;

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
        IReadOnlyList<NotableDate> perYear = GetOrGenerateYear(year);
        foreach (NotableDate notable in perYear)
        {
            if (!string.Equals(notable.Name, ruleName, StringComparison.OrdinalIgnoreCase)) continue;
            if (!MatchesContext(notable, territoryCode, calendarType)) continue;

            return notable.Date;
        }

        return null;
    }

    /// <summary>
    /// Layers two <see cref="INotableDateAlgorithmRegistry" /> instances: <c>primary</c> is consulted first; on a
    /// miss, <c>fallback</c> is consulted. Used to compose host-supplied algorithms with plugin-supplied ones so
    /// the host retains precedence on key collisions.
    /// </summary>
    private sealed class CompositeAlgorithmRegistry
        : INotableDateAlgorithmRegistry
    {
        /// <summary>The host-supplied registry consulted first; its registrations take precedence on key collisions.</summary>
        private readonly INotableDateAlgorithmRegistry _primary;

        /// <summary>The plugin-supplied registry consulted when <see cref="_primary" /> does not contain the requested key.</summary>
        private readonly INotableDateAlgorithmRegistry _fallback;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeAlgorithmRegistry"/> class.
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
        public bool TryGet(string key, out INotableDateAlgorithm algorithm)
        {
            if (_primary.TryGet(key, out algorithm!))
                return true;

            return _fallback.TryGet(key, out algorithm!);
        }
    }
}

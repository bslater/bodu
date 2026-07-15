// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AggregatingRateProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Bodu.Financial.Currencies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Groups several named <see cref="IDatedRateProvider" /> children behind a single entry point, combining them through
/// a configurable <see cref="IRateAggregationStrategy" /> with optional per-currency-pair routing.
/// </summary>
/// <remarks>
/// <para>
/// The aggregator is itself a provider on both the dated <see cref="IDatedRateProvider" /> and timeless
/// <see cref="IRateProvider" /> surfaces, so it composes anywhere a provider is expected — including wrapping each
/// child in its own <see cref="CachingRateProvider" /> so the grouping sits above the per-source cache. Same-currency
/// identity is handled here before any strategy is consulted.
/// </para>
/// <para>
/// Aggregation combines <em>distinct sources</em> — children that differ in who published the rate — for resilience and
/// coverage, and is orthogonal to the tiered read-through that a <see cref="CachingRateProvider" /> forms by stacking
/// caches over a <em>single</em> source. The two nest: an aggregator child can itself be a stacked cache over a source.
/// Reach for aggregation for fallback, an averaged rate, or per-pair routing across providers; reach for stacking to
/// cut latency and survive restarts on one provider.
/// </para>
/// <para>
/// To target a specific source rather than the routed result, resolve it by name through <see cref="TryGetProvider" />
/// (or, under dependency injection, a keyed service); the lookup methods always apply the configured strategy and
/// routing.
/// </para>
/// <para>
/// When <see cref="RateAggregationOptions.RespectHistoryAvailability" /> is enabled (the default), children that
/// advertise their history depth through <see cref="IHistoricalRateProvider" /> and have declared they cannot serve any
/// part of the requested date or window are dropped from the candidate set before the strategy runs, so a priority
/// fallback does not waste a call on a source that cannot answer. A non-aware child is treated as unbounded and always
/// kept, and the group's own <see cref="HistoryAvailability" /> composes the most generous declaration across the
/// children.
/// </para>
/// <para>
/// Routing is frozen at construction: the per-pair routes and default order are resolved to their candidate sets once,
/// after the constructor validates every referenced name. Mutating the supplied
/// <see cref="RateAggregationOptions.Routes" /> or <see cref="RateAggregationOptions.DefaultProviderOrder" /> after
/// construction has no effect on this instance; construct a new provider to change routing.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Wrap each source in its own cache, then group them with per-FX-pair routing.
/// var rba = new CachingRateProvider(rbaSource, new TomlFileRateCache(
///     new FileRateCacheOptions { Provider = "RBA", CacheDirectory = "/var/cache/fx" }), options);
/// var ecb = new CachingRateProvider(ecbSource, new InMemoryRateCache("ECB"), options);
///
/// var aggregation = new RateAggregationOptions();
/// aggregation.Routes[new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD)] = new CurrencyPairRoute(new[] { "RBA", "ECB" });
/// aggregation.Routes[new CurrencyPair(CurrencyCode.USD, CurrencyCode.GBP)] = new CurrencyPairRoute(new[] { "ECB", "RBA" });
/// aggregation.Routes[new CurrencyPair(CurrencyCode.EUR, CurrencyCode.USD)] = new CurrencyPairRoute(new[] { "ECB", "RBA" }, new AverageStrategy());
///
/// IDatedRateProvider provider = new AggregatingRateProvider(
///     new[]
///     {
///         new NamedDatedRateProvider("RBA", rba),
///         new NamedDatedRateProvider("ECB", ecb),
///     },
///     aggregation);
///
/// // AUD/USD is routed RBA-then-ECB; EUR/USD is averaged across both.
/// RateLookupResult aud = provider.GetRate("AUD", "USD", new DateOnly(2024, 1, 3), RateLookupOptions.Exact);
///
/// // Reach a specific source directly when the routed result is not what you want.
/// if (((AggregatingRateProvider)provider).TryGetProvider("ECB", out IDatedRateProvider ecbOnly))
/// {
///     RateLookupResult ecbRate = ecbOnly.GetRate("AUD", "USD", new DateOnly(2024, 1, 3), RateLookupOptions.Exact);
/// }
///]]>
/// </code>
/// </example>
public sealed class AggregatingRateProvider
    : IDatedRateProvider, IRateProvider, IHistoricalRateProvider
{
    /// <summary>The synthetic provider name reported for a same-currency identity rate.</summary>
    private const string IdentityProvider = "Identity";

    /// <summary>The children in the order supplied, used as the default candidate set.</summary>
    private readonly NamedDatedRateProvider[] _children;

    /// <summary>The children indexed by name for routing and direct resolution.</summary>
    private readonly Dictionary<string, IDatedRateProvider> _byName;

    /// <summary>The aggregation options carrying the default strategy, default order, and per-pair routes.</summary>
    private readonly RateAggregationOptions _options;

    /// <summary>The per-pair routes resolved to their candidate arrays and strategies once at construction, so a routed lookup probes a dictionary instead of rebuilding the candidate array per call. Routing is frozen at construction; later mutation of the options' routes has no effect.</summary>
    private readonly Dictionary<CurrencyPair, (NamedDatedRateProvider[] Candidates, IRateAggregationStrategy Strategy)> _routes;

    /// <summary>The default provider order resolved to its candidate array once at construction, or <see langword="null" /> when no default order is configured.</summary>
    private readonly NamedDatedRateProvider[]? _defaultOrder;

    /// <summary>The time source used to resolve the current date for the timeless surface.</summary>
    private readonly TimeProvider _timeProvider;

    /// <summary>The logger that records routing and aggregation outcomes.</summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregatingRateProvider" /> class.
    /// </summary>
    /// <param name="children">The named children to group, in default priority order.</param>
    /// <param name="options">
    /// The aggregation options, or <see langword="null" /> for priority-fallback defaults.
    /// </param>
    /// <param name="timeProvider">
    /// The time source used by the timeless surface. <see langword="null" /> selects <see cref="TimeProvider.System" />
    /// .
    /// </param>
    /// <param name="logger">
    /// The logger that records routing and aggregation outcomes. <see langword="null" /> selects
    /// <see cref="NullLogger.Instance" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="children" /> is <see langword="null" />, or when <paramref name="options" /> has a
    /// <see langword="null" /> <see cref="RateAggregationOptions.DefaultStrategy" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="children" /> is empty, contains a blank name, a <see langword="null" /> provider, or
    /// a duplicate name, or when <paramref name="options" /> references a child name that was not supplied.
    /// </exception>
    public AggregatingRateProvider(
        IEnumerable<NamedDatedRateProvider> children,
        RateAggregationOptions? options = null,
        TimeProvider? timeProvider = null,
        ILogger? logger = null)
    {
        ThrowHelper.ThrowIfNull(children);

        _options = options ?? new RateAggregationOptions();
        _options.Validate();
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger ?? NullLogger.Instance;

        NamedDatedRateProvider[] snapshot = [.. children];
        if (snapshot.Length == 0)
            throw new ArgumentException(CachingResourceStrings.Arg_Invalid_ProvidersEmpty, nameof(children));

        _byName = new Dictionary<string, IDatedRateProvider>(snapshot.Length, StringComparer.Ordinal);
        for (int i = 0; i < snapshot.Length; i++)
        {
            NamedDatedRateProvider child = snapshot[i];

            if (string.IsNullOrWhiteSpace(child.Name))
                throw new ArgumentException(CachingResourceStrings.Arg_Invalid_ProviderNameBlank, nameof(children));

            if (child.Provider is null)
                throw new ArgumentException(CachingResourceStrings.Arg_Invalid_ProviderNull, nameof(children));

            if (!_byName.TryAdd(child.Name, child.Provider))
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.CurrentCulture, CachingResourceStrings.Arg_Invalid_DuplicateProviderName, child.Name),
                    nameof(children));
            }
        }

        _children = snapshot;

        ValidateReferencedNames(nameof(options));

        // Resolve every route (and the default order) to its candidate array once: the name->provider map and each
        // route's provider order are immutable after construction, so per-call resolution would rebuild identical
        // arrays. This also freezes routing at construction — mutating the options' routes afterwards has no effect,
        // making explicit the contract the constructor-time validation always implied.
        _routes = new Dictionary<CurrencyPair, (NamedDatedRateProvider[] Candidates, IRateAggregationStrategy Strategy)>(_options.Routes.Count);
        foreach (KeyValuePair<CurrencyPair, CurrencyPairRoute> entry in _options.Routes)
            _routes[entry.Key] = (ResolveNames(entry.Value.ProviderOrder), entry.Value.Strategy ?? _options.DefaultStrategy);

        _defaultOrder = _options.DefaultProviderOrder is not null ? ResolveNames(_options.DefaultProviderOrder) : null;
    }

    /// <summary>
    /// Gets the names of the grouped children.
    /// </summary>
    /// <value>The child names, in no particular order.</value>
    public IReadOnlyCollection<string> ProviderNames => _byName.Keys;

    /// <summary>
    /// Gets the history depth this group advertises: the most generous availability across the grouped children,
    /// because a date any single child can serve is a date the group can serve.
    /// </summary>
    /// <value>
    /// <see cref="RateHistoryAvailability.Unbounded" /> when any child is
    /// <see cref="RateHistoryAvailability.Unbounded" /> or does not implement <see cref="IHistoricalRateProvider" /> (a
    /// non-aware child declares no floor); otherwise the child availability whose earliest available date, evaluated
    /// against the current date, reaches furthest back.
    /// </value>
    public RateHistoryAvailability HistoryAvailability
    {
        get
        {
            var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);

            RateHistoryAvailability? mostGenerous = null;
            DateOnly? mostGenerousEarliest = null;

            foreach (NamedDatedRateProvider child in _children)
            {
                if (child.Provider is not IHistoricalRateProvider aware)
                    return RateHistoryAvailability.Unbounded;

                RateHistoryAvailability availability = aware.HistoryAvailability;
                DateOnly? earliest = availability.GetEarliestAvailable(today);
                if (earliest is null)
                    return RateHistoryAvailability.Unbounded;

                if (mostGenerousEarliest is null || earliest.Value < mostGenerousEarliest.Value)
                {
                    mostGenerous = availability;
                    mostGenerousEarliest = earliest;
                }
            }

            // The constructor rejects an empty child set, so at least one child contributed a bounded availability.
            return mostGenerous!.Value;
        }
    }

    /// <summary>
    /// Attempts to resolve a grouped child by name, for callers that need a specific source rather than the routed
    /// result.
    /// </summary>
    /// <param name="name">The child name.</param>
    /// <param name="provider">When this method returns <see langword="true" />, the matching child provider.</param>
    /// <returns>
    /// <see langword="true" /> when a child with the supplied name exists; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    public bool TryGetProvider(string name, [MaybeNullWhen(false)] out IDatedRateProvider provider)
    {
        ThrowHelper.ThrowIfNull(name);

        return _byName.TryGetValue(name, out provider);
    }

    /// <inheritdoc />
    public RateLookupResult GetRate(string fromIsoCode, string toIsoCode, RateLookupOptions? options = null)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        return GetRate(fromIsoCode, toIsoCode, today, options ?? _options.DefaultLookupOptions);
    }

    /// <inheritdoc />
    public RateLookupResult GetRate(string fromIsoCode, string toIsoCode, DateOnly date, RateLookupOptions? options = null)
    {
        return TryGetRate(fromIsoCode, toIsoCode, date, options, out RateLookupResult result)
            ? result
            : throw new KeyNotFoundException(
                string.Format(CultureInfo.CurrentCulture, CachingResourceStrings.IO_KeyNotFound_ExchangeRate, fromIsoCode, toIsoCode, date));
    }

    /// <inheritdoc />
    public ValueTask<RateLookupResult> GetRateAsync(
        string fromIsoCode,
        string toIsoCode,
        RateLookupOptions? options = null,
        CancellationToken cancellationToken = default) =>
        new(GetRate(fromIsoCode, toIsoCode, options));

    /// <inheritdoc />
    public ValueTask<RateLookupResult> GetRateAsync(
        string fromIsoCode,
        string toIsoCode,
        DateOnly date,
        RateLookupOptions? options = null,
        CancellationToken cancellationToken = default) =>
        new(GetRate(fromIsoCode, toIsoCode, date, options));

    /// <inheritdoc />
    public bool TryGetRate(string fromIsoCode, string toIsoCode, DateOnly date, RateLookupOptions? options, out RateLookupResult result)
    {
        options ??= RateLookupOptions.Exact;
        CurrencyPair pair = new(CurrencyInfo.ParseCurrencyCode(fromIsoCode), CurrencyInfo.ParseCurrencyCode(toIsoCode));

        if (options.AllowSameCurrencyIdentityRate && pair.From == pair.To)
        {
            ExchangeRate identity = new(pair.From, pair.To, date, 1m, IdentityProvider);
            result = new RateLookupResult(identity, date, options.DateResolution, 0, RateProvenance.Live(identity.Provider));
            return true;
        }

        (NamedDatedRateProvider[] candidates, IRateAggregationStrategy strategy) = ResolveRoute(pair, options.AllowInverse);
        candidates = FilterCandidatesForDate(candidates, date, options);

        if (_logger.IsEnabled(_options.RouteSelectedLogLevel))
        {
            string providerOrder = FormatNames(candidates);
            Log.RouteSelected(_logger, _options.RouteSelectedLogLevel, fromIsoCode, toIsoCode, providerOrder);
        }

        if (strategy.TryAggregate(fromIsoCode, toIsoCode, date, options, candidates, out result))
        {
            if (_logger.IsEnabled(_options.ResolvedLogLevel))
                Log.Aggregated(_logger, _options.ResolvedLogLevel, strategy.GetType().Name, fromIsoCode, toIsoCode, date, candidates.Length);

            return true;
        }

        if (_logger.IsEnabled(_options.UnresolvedLogLevel))
            Log.AggregationUnresolved(_logger, _options.UnresolvedLogLevel, strategy.GetType().Name, fromIsoCode, toIsoCode, date);

        result = default;
        return false;
    }

    /// <inheritdoc />
    public RateRangeResult GetRates(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
            throw new ArgumentException(CachingResourceStrings.Arg_Invalid_RangeInverted, nameof(endDate));

        CurrencyPair pair = new(CurrencyInfo.ParseCurrencyCode(fromIsoCode), CurrencyInfo.ParseCurrencyCode(toIsoCode));
        (NamedDatedRateProvider[] candidates, IRateAggregationStrategy strategy) = ResolveRoute(pair, allowInverse: false);
        candidates = FilterCandidatesForRange(candidates, endDate);

        if (_logger.IsEnabled(_options.RouteSelectedLogLevel))
        {
            string providerOrder = FormatNames(candidates);
            Log.RouteSelected(_logger, _options.RouteSelectedLogLevel, fromIsoCode, toIsoCode, providerOrder);
        }

        IReadOnlyList<ExchangeRate> rates = strategy.AggregateRange(fromIsoCode, toIsoCode, startDate, endDate, candidates);
        return new RateRangeResult(fromIsoCode, toIsoCode, startDate, endDate, rates);
    }

    /// <inheritdoc />
    public async ValueTask<RateRangeResult> GetRatesAsync(
        string fromIsoCode,
        string toIsoCode,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        if (endDate < startDate)
            throw new ArgumentException(CachingResourceStrings.Arg_Invalid_RangeInverted, nameof(endDate));

        CurrencyPair pair = new(CurrencyInfo.ParseCurrencyCode(fromIsoCode), CurrencyInfo.ParseCurrencyCode(toIsoCode));
        (NamedDatedRateProvider[] candidates, IRateAggregationStrategy strategy) = ResolveRoute(pair, allowInverse: false);
        candidates = FilterCandidatesForRange(candidates, endDate);

        if (_logger.IsEnabled(_options.RouteSelectedLogLevel))
        {
            string providerOrder = FormatNames(candidates);
            Log.RouteSelected(_logger, _options.RouteSelectedLogLevel, fromIsoCode, toIsoCode, providerOrder);
        }

        IReadOnlyList<ExchangeRate> rates =
            await strategy.AggregateRangeAsync(fromIsoCode, toIsoCode, startDate, endDate, candidates, cancellationToken).ConfigureAwait(false);
        return new RateRangeResult(fromIsoCode, toIsoCode, startDate, endDate, rates);
    }

    /// <inheritdoc />
    decimal IRateProvider.GetRate(string fromIsoCode, string toIsoCode) =>
        GetRate(fromIsoCode, toIsoCode).Rate.Rate;

    /// <summary>
    /// Resolves the candidate set and strategy for a pair, preferring a direct route, then an inverse route (when
    /// permitted), then the default order.
    /// </summary>
    /// <param name="pair">The requested currency pair.</param>
    /// <param name="allowInverse">Whether an inverse-pair route may be used.</param>
    /// <returns>The ordered candidates and the strategy to combine them.</returns>
    private (NamedDatedRateProvider[] Candidates, IRateAggregationStrategy Strategy) ResolveRoute(CurrencyPair pair, bool allowInverse)
    {
        if (_routes.TryGetValue(pair, out (NamedDatedRateProvider[] Candidates, IRateAggregationStrategy Strategy) route))
            return route;

        if (allowInverse && _routes.TryGetValue(pair.Inverse(), out (NamedDatedRateProvider[] Candidates, IRateAggregationStrategy Strategy) inverseRoute))
            return inverseRoute;

        if (_defaultOrder is not null)
            return (_defaultOrder, _options.DefaultStrategy);

        return (_children, _options.DefaultStrategy);
    }

    /// <summary>
    /// Drops candidates whose advertised history says they cannot resolve any date the lookup could reach, so a doomed
    /// child is never consulted by the strategy.
    /// </summary>
    /// <param name="candidates">The routed candidates, in order.</param>
    /// <param name="date">The requested date.</param>
    /// <param name="options">The lookup rules to apply.</param>
    /// <returns>
    /// The candidates that could serve the lookup, in their original order; the input array unchanged when the filter
    /// is disabled or nothing was dropped.
    /// </returns>
    private NamedDatedRateProvider[] FilterCandidatesForDate(NamedDatedRateProvider[] candidates, DateOnly date, RateLookupOptions options)
    {
        if (!_options.RespectHistoryAvailability)
            return candidates;

        DateOnly latestReachable = HistoryAvailabilityGuard.LatestReachableDate(date, options);
        return FilterCandidates(candidates, latestReachable);
    }

    /// <summary>
    /// Drops candidates whose advertised history says they cannot serve any part of the requested window; a child whose
    /// history merely clips the window's start is kept, since strategies already tolerate partial data.
    /// </summary>
    /// <param name="candidates">The routed candidates, in order.</param>
    /// <param name="endDate">The inclusive last date of the requested window.</param>
    /// <returns>
    /// The candidates whose advertised history overlaps the window, in their original order; the input array unchanged
    /// when the filter is disabled or nothing was dropped.
    /// </returns>
    private NamedDatedRateProvider[] FilterCandidatesForRange(NamedDatedRateProvider[] candidates, DateOnly endDate) =>
        _options.RespectHistoryAvailability
            ? FilterCandidates(candidates, endDate)
            : candidates;

    /// <summary>
    /// Keeps the candidates whose advertised earliest available date is on or before the latest date the request can
    /// reach. A non-aware or unbounded child is always kept.
    /// </summary>
    /// <param name="candidates">The routed candidates, in order.</param>
    /// <param name="latestReachable">The latest date the request could resolve or cover.</param>
    /// <returns>The surviving candidates, in their original order.</returns>
    private NamedDatedRateProvider[] FilterCandidates(NamedDatedRateProvider[] candidates, DateOnly latestReachable)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);

        List<NamedDatedRateProvider>? kept = null;
        for (int i = 0; i < candidates.Length; i++)
        {
            DateOnly? earliest = candidates[i].Provider is IHistoricalRateProvider aware
                ? aware.HistoryAvailability.GetEarliestAvailable(today)
                : null;
            bool available = earliest is null || earliest.Value <= latestReachable;

            if (available)
            {
                kept?.Add(candidates[i]);
            }
            else if (kept is null)
            {
                // First drop: materialize the survivors seen so far and continue filtering into the list.
                kept = new List<NamedDatedRateProvider>(candidates.Length - 1);
                for (int j = 0; j < i; j++)
                    kept.Add(candidates[j]);
            }
        }

        return kept is null ? candidates : [.. kept];
    }

    /// <summary>
    /// Maps an ordered set of validated child names to their providers.
    /// </summary>
    /// <param name="names">The child names to resolve.</param>
    /// <returns>The resolved named providers, in order.</returns>
    private NamedDatedRateProvider[] ResolveNames(IReadOnlyList<string> names)
    {
        var resolved = new NamedDatedRateProvider[names.Count];
        for (int i = 0; i < names.Count; i++)
            resolved[i] = new NamedDatedRateProvider(names[i], _byName[names[i]]);

        return resolved;
    }

    /// <summary>
    /// Validates that every child name referenced by the routes and default order was supplied as a child.
    /// </summary>
    /// <param name="optionsParamName">The parameter name to attribute a validation failure to.</param>
    /// <exception cref="ArgumentException">Thrown when a referenced child name was not supplied.</exception>
    private void ValidateReferencedNames(string optionsParamName)
    {
        if (_options.DefaultProviderOrder is not null)
        {
            foreach (string name in _options.DefaultProviderOrder)
                ThrowIfUnknownChild(name, optionsParamName);
        }

        foreach (KeyValuePair<CurrencyPair, CurrencyPairRoute> entry in _options.Routes)
        {
            foreach (string name in entry.Value.ProviderOrder)
                ThrowIfUnknownChild(name, optionsParamName);
        }
    }

    /// <summary>
    /// Throws when a referenced child name is not among the supplied children.
    /// </summary>
    /// <param name="name">The referenced child name.</param>
    /// <param name="paramName">The parameter name to attribute the failure to.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name" /> is not a known child.</exception>
    private void ThrowIfUnknownChild(string name, string paramName)
    {
        if (!_byName.ContainsKey(name))
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, CachingResourceStrings.Arg_Invalid_UnknownAggregationChild, name),
                paramName);
        }
    }

    /// <summary>
    /// Formats the candidate names as a comma-separated list for diagnostics.
    /// </summary>
    /// <param name="candidates">The candidates to format.</param>
    /// <returns>The comma-separated candidate names.</returns>
    private static string FormatNames(NamedDatedRateProvider[] candidates)
    {
        string[] names = new string[candidates.Length];
        for (int i = 0; i < candidates.Length; i++)
            names[i] = candidates[i].Name;

        return string.Join(", ", names);
    }
}

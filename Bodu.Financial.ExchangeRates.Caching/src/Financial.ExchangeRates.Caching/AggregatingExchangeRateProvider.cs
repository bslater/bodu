// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AggregatingExchangeRateProvider.cs" company="Bodu Pty. Ltd.">
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
/// Groups several named <see cref="IDatedExchangeRateProvider" /> children behind a single entry point, combining them
/// through a configurable <see cref="IExchangeRateAggregationStrategy" /> with optional per-currency-pair routing.
/// </summary>
/// <remarks>
/// <para>
/// The aggregator is itself a provider on both the dated <see cref="IDatedExchangeRateProvider" /> and timeless
/// <see cref="IExchangeRateProvider" /> surfaces, so it composes anywhere a provider is expected — including wrapping
/// each child in its own <see cref="CachingExchangeRateProvider" /> so the grouping sits above the per-source cache.
/// Same-currency identity is handled here before any strategy is consulted.
/// </para>
/// <para>
/// Aggregation combines <em>distinct sources</em> — children that differ in who published the rate — for resilience and
/// coverage, and is orthogonal to the tiered read-through that a <see cref="CachingExchangeRateProvider" /> forms by
/// stacking caches over a <em>single</em> source. The two nest: an aggregator child can itself be a stacked cache over
/// a source. Reach for aggregation for fallback, an averaged rate, or per-pair routing across providers; reach for
/// stacking to cut latency and survive restarts on one provider.
/// </para>
/// <para>
/// To target a specific source rather than the routed result, resolve it by name through <see cref="TryGetProvider" />
/// (or, under dependency injection, a keyed service); the lookup methods always apply the configured strategy and
/// routing.
/// </para>
/// <para>
/// When <see cref="ExchangeRateAggregationOptions.RespectHistoryAvailability" /> is enabled (the default), children
/// that advertise their history depth through <see cref="IHistoryAwareExchangeRateProvider" /> and have declared they
/// cannot serve any part of the requested date or window are dropped from the candidate set before the strategy runs,
/// so a priority fallback does not waste a call on a source that cannot answer. A non-aware child is treated as
/// unbounded and always kept, and the group's own <see cref="HistoryAvailability" /> composes the most generous
/// declaration across the children.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Wrap each source in its own cache, then group them with per-FX-pair routing.
/// var rba = new CachingExchangeRateProvider(rbaSource, new TomlFileExchangeRateCache(
///     new FileExchangeRateCacheOptions { Provider = "RBA", CacheDirectory = "/var/cache/fx" }), options);
/// var ecb = new CachingExchangeRateProvider(ecbSource, new InMemoryExchangeRateCache("ECB"), options);
///
/// var aggregation = new ExchangeRateAggregationOptions();
/// aggregation.Routes[new ExchangeRatePair(CurrencyCode.AUD, CurrencyCode.USD)] = new ExchangeRatePairRoute(new[] { "RBA", "ECB" });
/// aggregation.Routes[new ExchangeRatePair(CurrencyCode.USD, CurrencyCode.GBP)] = new ExchangeRatePairRoute(new[] { "ECB", "RBA" });
/// aggregation.Routes[new ExchangeRatePair(CurrencyCode.EUR, CurrencyCode.USD)] = new ExchangeRatePairRoute(new[] { "ECB", "RBA" }, new AverageStrategy());
///
/// IDatedExchangeRateProvider provider = new AggregatingExchangeRateProvider(
///     new[]
///     {
///         new NamedDatedExchangeRateProvider("RBA", rba),
///         new NamedDatedExchangeRateProvider("ECB", ecb),
///     },
///     aggregation);
///
/// // AUD/USD is routed RBA-then-ECB; EUR/USD is averaged across both.
/// ExchangeRateLookupResult aud = provider.GetRate("AUD", "USD", new DateOnly(2024, 1, 3), ExchangeRateLookupOptions.Exact);
///
/// // Reach a specific source directly when the routed result is not what you want.
/// if (((AggregatingExchangeRateProvider)provider).TryGetProvider("ECB", out IDatedExchangeRateProvider ecbOnly))
/// {
///     ExchangeRateLookupResult ecbRate = ecbOnly.GetRate("AUD", "USD", new DateOnly(2024, 1, 3), ExchangeRateLookupOptions.Exact);
/// }
///]]>
/// </code>
/// </example>
public sealed class AggregatingExchangeRateProvider
    : IDatedExchangeRateProvider, IExchangeRateProvider, IHistoryAwareExchangeRateProvider
{
    /// <summary>The synthetic provider name reported for a same-currency identity rate.</summary>
    private const string IdentityProvider = "Identity";

    /// <summary>The children in the order supplied, used as the default candidate set.</summary>
    private readonly NamedDatedExchangeRateProvider[] _children;

    /// <summary>The children indexed by name for routing and direct resolution.</summary>
    private readonly Dictionary<string, IDatedExchangeRateProvider> _byName;

    /// <summary>The aggregation options carrying the default strategy, default order, and per-pair routes.</summary>
    private readonly ExchangeRateAggregationOptions _options;

    /// <summary>The time source used to resolve the current date for the timeless surface.</summary>
    private readonly TimeProvider _timeProvider;

    /// <summary>The logger that records routing and aggregation outcomes.</summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregatingExchangeRateProvider" /> class.
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
    /// <see langword="null" /> <see cref="ExchangeRateAggregationOptions.DefaultStrategy" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="children" /> is empty, contains a blank name, a <see langword="null" /> provider, or
    /// a duplicate name, or when <paramref name="options" /> references a child name that was not supplied.
    /// </exception>
    public AggregatingExchangeRateProvider(
        IEnumerable<NamedDatedExchangeRateProvider> children,
        ExchangeRateAggregationOptions? options = null,
        TimeProvider? timeProvider = null,
        ILogger? logger = null)
    {
        ThrowHelper.ThrowIfNull(children);

        _options = options ?? new ExchangeRateAggregationOptions();
        _options.Validate();
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger ?? NullLogger.Instance;

        NamedDatedExchangeRateProvider[] snapshot = [.. children];
        if (snapshot.Length == 0)
            throw new ArgumentException(CachingResourceStrings.Arg_Invalid_ProvidersEmpty, nameof(children));

        _byName = new Dictionary<string, IDatedExchangeRateProvider>(snapshot.Length, StringComparer.Ordinal);
        for (int i = 0; i < snapshot.Length; i++)
        {
            NamedDatedExchangeRateProvider child = snapshot[i];

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
    /// <see cref="ExchangeRateHistoryAvailability.Unbounded" /> when any child is
    /// <see cref="ExchangeRateHistoryAvailability.Unbounded" /> or does not implement
    /// <see cref="IHistoryAwareExchangeRateProvider" /> (a non-aware child declares no floor); otherwise the child
    /// availability whose earliest available date, evaluated against the current date, reaches furthest back.
    /// </value>
    public ExchangeRateHistoryAvailability HistoryAvailability
    {
        get
        {
            var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);

            ExchangeRateHistoryAvailability? mostGenerous = null;
            DateOnly? mostGenerousEarliest = null;

            foreach (NamedDatedExchangeRateProvider child in _children)
            {
                if (child.Provider is not IHistoryAwareExchangeRateProvider aware)
                    return ExchangeRateHistoryAvailability.Unbounded;

                ExchangeRateHistoryAvailability availability = aware.HistoryAvailability;
                DateOnly? earliest = availability.GetEarliestAvailable(today);
                if (earliest is null)
                    return ExchangeRateHistoryAvailability.Unbounded;

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
    public bool TryGetProvider(string name, [MaybeNullWhen(false)] out IDatedExchangeRateProvider provider)
    {
        ThrowHelper.ThrowIfNull(name);

        return _byName.TryGetValue(name, out provider);
    }

    /// <inheritdoc />
    public ExchangeRateLookupResult GetRate(string fromIsoCode, string toIsoCode, ExchangeRateLookupOptions? options = null)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        return GetRate(fromIsoCode, toIsoCode, today, options ?? _options.DefaultLookupOptions);
    }

    /// <inheritdoc />
    public ExchangeRateLookupResult GetRate(string fromIsoCode, string toIsoCode, DateOnly date, ExchangeRateLookupOptions? options = null)
    {
        return TryGetRate(fromIsoCode, toIsoCode, date, options, out ExchangeRateLookupResult result)
            ? result
            : throw new KeyNotFoundException(
                string.Format(CultureInfo.CurrentCulture, CachingResourceStrings.IO_KeyNotFound_ExchangeRate, fromIsoCode, toIsoCode, date));
    }

    /// <inheritdoc />
    public ValueTask<ExchangeRateLookupResult> GetRateAsync(
        string fromIsoCode,
        string toIsoCode,
        ExchangeRateLookupOptions? options = null,
        CancellationToken cancellationToken = default) =>
        new(GetRate(fromIsoCode, toIsoCode, options));

    /// <inheritdoc />
    public ValueTask<ExchangeRateLookupResult> GetRateAsync(
        string fromIsoCode,
        string toIsoCode,
        DateOnly date,
        ExchangeRateLookupOptions? options = null,
        CancellationToken cancellationToken = default) =>
        new(GetRate(fromIsoCode, toIsoCode, date, options));

    /// <inheritdoc />
    public bool TryGetRate(string fromIsoCode, string toIsoCode, DateOnly date, ExchangeRateLookupOptions? options, out ExchangeRateLookupResult result)
    {
        options ??= ExchangeRateLookupOptions.Exact;
        ExchangeRatePair pair = new(CurrencyInfo.ParseCurrencyCode(fromIsoCode), CurrencyInfo.ParseCurrencyCode(toIsoCode));

        if (options.AllowSameCurrencyIdentityRate && pair.From == pair.To)
        {
            ExchangeRate identity = new(pair.From, pair.To, date, 1m, IdentityProvider);
            result = new ExchangeRateLookupResult(identity, date, options.DateResolution, 0, ExchangeRateProvenance.Live(identity.Provider));
            return true;
        }

        (NamedDatedExchangeRateProvider[] candidates, IExchangeRateAggregationStrategy strategy) = ResolveRoute(pair, options.AllowInverse);
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
    public ExchangeRateRangeResult GetRates(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
            throw new ArgumentException(CachingResourceStrings.Arg_Invalid_RangeInverted, nameof(endDate));

        ExchangeRatePair pair = new(CurrencyInfo.ParseCurrencyCode(fromIsoCode), CurrencyInfo.ParseCurrencyCode(toIsoCode));
        (NamedDatedExchangeRateProvider[] candidates, IExchangeRateAggregationStrategy strategy) = ResolveRoute(pair, allowInverse: false);
        candidates = FilterCandidatesForRange(candidates, endDate);

        if (_logger.IsEnabled(_options.RouteSelectedLogLevel))
        {
            string providerOrder = FormatNames(candidates);
            Log.RouteSelected(_logger, _options.RouteSelectedLogLevel, fromIsoCode, toIsoCode, providerOrder);
        }

        IReadOnlyList<ExchangeRate> rates = strategy.AggregateRange(fromIsoCode, toIsoCode, startDate, endDate, candidates);
        return new ExchangeRateRangeResult(fromIsoCode, toIsoCode, startDate, endDate, rates);
    }

    /// <inheritdoc />
    public async ValueTask<ExchangeRateRangeResult> GetRatesAsync(
        string fromIsoCode,
        string toIsoCode,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        if (endDate < startDate)
            throw new ArgumentException(CachingResourceStrings.Arg_Invalid_RangeInverted, nameof(endDate));

        ExchangeRatePair pair = new(CurrencyInfo.ParseCurrencyCode(fromIsoCode), CurrencyInfo.ParseCurrencyCode(toIsoCode));
        (NamedDatedExchangeRateProvider[] candidates, IExchangeRateAggregationStrategy strategy) = ResolveRoute(pair, allowInverse: false);
        candidates = FilterCandidatesForRange(candidates, endDate);

        if (_logger.IsEnabled(_options.RouteSelectedLogLevel))
        {
            string providerOrder = FormatNames(candidates);
            Log.RouteSelected(_logger, _options.RouteSelectedLogLevel, fromIsoCode, toIsoCode, providerOrder);
        }

        IReadOnlyList<ExchangeRate> rates =
            await strategy.AggregateRangeAsync(fromIsoCode, toIsoCode, startDate, endDate, candidates, cancellationToken).ConfigureAwait(false);
        return new ExchangeRateRangeResult(fromIsoCode, toIsoCode, startDate, endDate, rates);
    }

    /// <inheritdoc />
    decimal IExchangeRateProvider.GetRate(string fromIsoCode, string toIsoCode) =>
        GetRate(fromIsoCode, toIsoCode).Rate.Rate;

    /// <summary>
    /// Resolves the candidate set and strategy for a pair, preferring a direct route, then an inverse route (when
    /// permitted), then the default order.
    /// </summary>
    /// <param name="pair">The requested currency pair.</param>
    /// <param name="allowInverse">Whether an inverse-pair route may be used.</param>
    /// <returns>The ordered candidates and the strategy to combine them.</returns>
    private (NamedDatedExchangeRateProvider[] Candidates, IExchangeRateAggregationStrategy Strategy) ResolveRoute(ExchangeRatePair pair, bool allowInverse)
    {
        if (_options.Routes.TryGetValue(pair, out ExchangeRatePairRoute? route))
            return (ResolveNames(route.ProviderOrder), route.Strategy ?? _options.DefaultStrategy);

        if (allowInverse && _options.Routes.TryGetValue(pair.Inverse(), out ExchangeRatePairRoute? inverseRoute))
            return (ResolveNames(inverseRoute.ProviderOrder), inverseRoute.Strategy ?? _options.DefaultStrategy);

        if (_options.DefaultProviderOrder is not null)
            return (ResolveNames(_options.DefaultProviderOrder), _options.DefaultStrategy);

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
    private NamedDatedExchangeRateProvider[] FilterCandidatesForDate(NamedDatedExchangeRateProvider[] candidates, DateOnly date, ExchangeRateLookupOptions options)
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
    private NamedDatedExchangeRateProvider[] FilterCandidatesForRange(NamedDatedExchangeRateProvider[] candidates, DateOnly endDate) =>
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
    private NamedDatedExchangeRateProvider[] FilterCandidates(NamedDatedExchangeRateProvider[] candidates, DateOnly latestReachable)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);

        List<NamedDatedExchangeRateProvider>? kept = null;
        for (int i = 0; i < candidates.Length; i++)
        {
            DateOnly? earliest = candidates[i].Provider is IHistoryAwareExchangeRateProvider aware
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
                kept = new List<NamedDatedExchangeRateProvider>(candidates.Length - 1);
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
    private NamedDatedExchangeRateProvider[] ResolveNames(IReadOnlyList<string> names)
    {
        var resolved = new NamedDatedExchangeRateProvider[names.Count];
        for (int i = 0; i < names.Count; i++)
            resolved[i] = new NamedDatedExchangeRateProvider(names[i], _byName[names[i]]);

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

        foreach (KeyValuePair<ExchangeRatePair, ExchangeRatePairRoute> entry in _options.Routes)
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
    private static string FormatNames(NamedDatedExchangeRateProvider[] candidates)
    {
        string[] names = new string[candidates.Length];
        for (int i = 0; i < candidates.Length; i++)
            names[i] = candidates[i].Name;

        return string.Join(", ", names);
    }
}

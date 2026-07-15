// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateCacheWarmupService.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// A hosted service that warms the registered caching rate providers at application start, fetching the configured
/// pairs over the configured window through each provider's normal read-through path so the first user request never
/// pays the origin fetch.
/// </summary>
/// <remarks>
/// <para>
/// The warm-up runs in the background after the host has started — it yields before doing any work, so application
/// start is never blocked — and warms every directly registered <see cref="CachingRateProvider" /> plus any keyed
/// aggregation children named in <see cref="RateCacheWarmupOptions.Providers" />. A named child that resolves to no
/// keyed caching provider is logged and skipped.
/// </para>
/// <para>
/// Failure never crashes the host: an individual pair's failure is already swallowed inside
/// <see cref="CachingRateProviderBase.WarmAsync" />, host shutdown cancels the run quietly, and any other fault is
/// logged and abandoned. The warmed window is recomputed from the clock each run, so a rolling look-back stays current
/// across restarts.
/// </para>
/// </remarks>
internal sealed class RateCacheWarmupService : BackgroundService
{
    /// <summary>The container the warm-up targets are resolved from when the run starts.</summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>The validated warm-up options.</summary>
    private readonly RateCacheWarmupOptions _options;

    /// <summary>The clock the rolling warm-up window is derived from.</summary>
    private readonly TimeProvider _timeProvider;

    /// <summary>The logger that receives the warm-up lifecycle diagnostics.</summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateCacheWarmupService" /> class.
    /// </summary>
    /// <param name="serviceProvider">The container the warm-up targets are resolved from.</param>
    /// <param name="options">The warm-up options.</param>
    /// <param name="timeProvider">
    /// The clock the rolling window is derived from, or <see langword="null" /> for <see cref="TimeProvider.System" />.
    /// </param>
    /// <param name="loggerFactory">
    /// The factory used to create the diagnostics logger, or <see langword="null" /> to disable logging.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="serviceProvider" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    public RateCacheWarmupService(
        IServiceProvider serviceProvider,
        IOptions<RateCacheWarmupOptions> options,
        TimeProvider? timeProvider = null,
        ILoggerFactory? loggerFactory = null)
    {
        ThrowHelper.ThrowIfNull(serviceProvider);
        ThrowHelper.ThrowIfNull(options);

        _serviceProvider = serviceProvider;
        _options = options.Value;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = loggerFactory?.CreateLogger<RateCacheWarmupService>() ?? NullLogger<RateCacheWarmupService>.Instance;
    }

    /// <summary>
    /// Runs the one-shot warm-up: resolves the window and targets, warms each target, and reports the outcome.
    /// </summary>
    /// <param name="stoppingToken">Signalled when the host shuts down, aborting the warm-up quietly.</param>
    /// <returns>A task representing the warm-up run.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield before any work so host start-up is never blocked by the warm-up's synchronous prefix.
        await Task.Yield();

        try
        {
            var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
            DateOnly startDate = _options.StartDate ?? today.AddDays(-_options.LookbackDays);
            DateOnly endDate = _options.EndDate ?? today;

            var pairs = new List<(string From, string To)>(_options.Pairs.Count);
            foreach (string pair in _options.Pairs)
            {
                if (RateCacheWarmupOptions.TryParsePair(pair, out (string From, string To) parsed))
                    pairs.Add(parsed);
            }

            Log.WarmupStarted(_logger, pairs.Count, startDate, endDate);

            int pairsWarmed = 0;
            var targets = 0;
            foreach (CachingRateProvider provider in _serviceProvider.GetServices<CachingRateProvider>())
            {
                targets++;
                pairsWarmed += await provider.WarmAsync(pairs, startDate, endDate, stoppingToken).ConfigureAwait(false);
            }

            foreach (string name in _options.Providers)
            {
                if (ResolveKeyedTarget(name) is { } keyed)
                {
                    targets++;
                    pairsWarmed += await keyed.WarmAsync(pairs, startDate, endDate, stoppingToken).ConfigureAwait(false);
                }
                else
                {
                    Log.WarmupProviderNotFound(_logger, name);
                }
            }

            Log.WarmupCompleted(_logger, pairsWarmed, targets);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host shutdown aborts the warm-up quietly; the cache simply stays as warm as it got.
        }
        catch (Exception ex)
        {
            // The warm-up is an optimization: any unexpected fault is reported and abandoned, never surfaced to the host.
            Log.WarmupFailed(_logger, ex);
        }
    }

    /// <summary>
    /// Resolves a named keyed aggregation child as a warmable caching provider, or <see langword="null" /> when the
    /// name resolves to nothing warmable.
    /// </summary>
    /// <param name="name">The keyed child name.</param>
    /// <returns>The keyed caching provider, or <see langword="null" />.</returns>
    private CachingRateProviderBase? ResolveKeyedTarget(string name) =>
        _serviceProvider is IKeyedServiceProvider keyedProvider
            ? keyedProvider.GetKeyedService<IDatedRateProvider>(name) as CachingRateProviderBase
            : null;
}

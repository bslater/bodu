// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCacheWarmupService.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// A hosted service that warms the registered caching notable-date service at application start, resolving the
/// configured territories over the configured span of civil years through the normal read-through path so the first
/// user request never pays the year computation.
/// </summary>
/// <remarks>
/// <para>
/// The warm-up runs in the background after the host has started — it yields before doing any work, so application
/// start is never blocked — and computes territories one at a time (year resolution is synchronous and CPU-bound),
/// checking for host shutdown between territories. When the registered <see cref="INotableDateService" /> is not the
/// caching decorator — the warm-up was registered without <c>AddCachedNotableDateService</c>, or another decorator was
/// layered over it — the run logs the misconfiguration and no-ops rather than failing the host.
/// </para>
/// <para>
/// Failure never crashes the host: an individual territory's failure is already swallowed inside
/// <see cref="CachingNotableDateService.Warm" />, host shutdown cancels the run quietly, and any other fault is logged
/// and abandoned. The warmed span is recomputed from the clock each run, so a rolling window stays current across
/// restarts.
/// </para>
/// </remarks>
internal sealed class NotableDateCacheWarmupService : BackgroundService
{
    /// <summary>The container the caching service is resolved from when the run starts.</summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>The validated warm-up options.</summary>
    private readonly NotableDateCacheWarmupOptions _options;

    /// <summary>The clock the rolling warm-up span is derived from.</summary>
    private readonly TimeProvider _timeProvider;

    /// <summary>The logger that receives the warm-up lifecycle diagnostics.</summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateCacheWarmupService" /> class.
    /// </summary>
    /// <param name="serviceProvider">The container the caching service is resolved from.</param>
    /// <param name="options">The warm-up options.</param>
    /// <param name="timeProvider">
    /// The clock the rolling span is derived from, or <see langword="null" /> for <see cref="TimeProvider.System" />.
    /// </param>
    /// <param name="loggerFactory">
    /// The factory used to create the diagnostics logger, or <see langword="null" /> to disable logging.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="serviceProvider" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    public NotableDateCacheWarmupService(
        IServiceProvider serviceProvider,
        IOptions<NotableDateCacheWarmupOptions> options,
        TimeProvider? timeProvider = null,
        ILoggerFactory? loggerFactory = null)
    {
        ThrowHelper.ThrowIfNull(serviceProvider);
        ThrowHelper.ThrowIfNull(options);

        _serviceProvider = serviceProvider;
        _options = options.Value;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = loggerFactory?.CreateLogger<NotableDateCacheWarmupService>() ?? NullLogger<NotableDateCacheWarmupService>.Instance;
    }

    /// <summary>
    /// Runs the one-shot warm-up: resolves the caching service and the span, warms each territory, and reports the
    /// outcome.
    /// </summary>
    /// <param name="stoppingToken">
    /// Signalled when the host shuts down, aborting the warm-up between territories.
    /// </param>
    /// <returns>A task representing the warm-up run.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield before any work so host start-up is never blocked by the CPU-bound year computations.
        await Task.Yield();

        try
        {
            if (_serviceProvider.GetService<INotableDateService>() is not CachingNotableDateService caching)
            {
                Log.WarmupServiceNotCaching(_logger);
                return;
            }

            int currentYear = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime).Year;
            int firstYear = _options.FirstYear ?? currentYear - _options.YearsBehind;
            int lastYear = _options.LastYear ?? currentYear + _options.YearsAhead;

            Log.WarmupStarted(_logger, _options.Territories.Count, firstYear, lastYear);

            var territoriesWarmed = 0;
            foreach (string territory in _options.Territories)
            {
                if (stoppingToken.IsCancellationRequested)
                    return;

                // One territory per call keeps the per-territory failure isolation and gives the shutdown check a
                // regular cadence; Warm itself swallows and logs the territory's failure.
                territoriesWarmed += caching.Warm(new[] { territory }, firstYear, lastYear);
            }

            Log.WarmupCompleted(_logger, territoriesWarmed, firstYear, lastYear);
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
}

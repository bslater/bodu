// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WebExchangeRateProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

/// <summary>
/// Provides the shared machinery for an exchange-rate provider that materializes a remote feed into an in-memory
/// <see cref="ExchangeRateBook" /> snapshot: it owns the accumulator and the immutable snapshot, implements the full
/// synchronous and asynchronous lookup matrix once, and optionally owns the <see cref="HttpClient" /> used to reach the
/// feed. Derived types supply only the feed-specific fetch.
/// </summary>
/// <remarks>
/// <para>
/// <strong>HttpClient ownership.</strong> A derived provider constructed without a caller-supplied client builds and
/// owns its own (typically through <see cref="ExchangeRateHttpClientFactory.Create(string?, TimeSpan)" />), passing it
/// to this base so it is disposed with the provider. A provider constructed with a caller-supplied client passes
/// <see langword="null" /> as the owned client, leaving its lifetime — and its HTTP contract (user agent, timeout) — to
/// the caller. This is the path a dependency-injection registration uses, supplying a client from
/// <c>IHttpClientFactory</c>.
/// </para>
/// <para>
/// <strong>Lookup matrix.</strong> All getters resolve against the current immutable snapshot. The synchronous point
/// and range getters block to fetch on a miss only when <see cref="AllowSynchronousNetworkAccess" /> is enabled; the
/// asynchronous getters always await a coverage-aware fetch. The undated getters resolve the most recent rate as of the
/// current instant supplied by the time provider. Derived types implement the feed-specific
/// <see cref="EnsureLoadedAsync(ExchangeRatePair, DateOnly, DateOnly, CancellationToken)" /> and
/// <see cref="IsLoaded(ExchangeRatePair, DateOnly, DateOnly)" /> and accumulate fetched observations through
/// <see cref="AddObservations(IEnumerable{ExchangeRate}, DateTimeOffset?)" /> followed by
/// <see cref="RebuildSnapshot" />, all under <see cref="SyncRoot" />.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Financial;
///
/// // A derived provider supplies only the feed-specific fetch and coverage check.
/// sealed class MyFeedProvider : WebExchangeRateProvider
/// {
///     public MyFeedProvider(HttpClient client) : base(client, timeProvider: null) { }
///
///     protected override string ProviderId => "MyFeed";
///     protected override bool AllowSynchronousNetworkAccess => false;
///     protected override TimeSpan DefaultLookback => TimeSpan.FromDays(7);
///
///     protected override bool IsLoaded(ExchangeRatePair pair, DateOnly startDate, DateOnly endDate) =>
///         false;   // real implementations track covered windows
///
///     protected override async ValueTask EnsureLoadedAsync(
///         ExchangeRatePair pair, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken)
///     {
///         IEnumerable<ExchangeRate> fetched = await FetchFromFeedAsync(pair, startDate, endDate, cancellationToken);
///         lock (SyncRoot)
///         {
///             AddObservations(fetched, DateTimeOffset.UtcNow);
///             RebuildSnapshot();
///         }
///     }
/// }
///]]>
/// </code>
/// </example>
/// </remarks>
public abstract class WebExchangeRateProvider
    : IDatedExchangeRateProvider, IExchangeRateProvider, IDisposable
{
    /// <summary>The tolerance, in days, used to resolve the most recent rate for the undated surfaces; large enough to reach any rate fetched into the store from the current date.</summary>
    private const int LatestRateToleranceDays = 100_000;

    /// <summary>Guards mutation of the accumulator and the snapshot fields, and is shared with derived types for their own coverage and series indexes so a fetch publishes atomically.</summary>
    private readonly object _gate = new();

    /// <summary>The accumulator into which each fetched observation is upserted.</summary>
    private readonly ExchangeRateTableBuilder _builder = new();

    /// <summary>The time source used to resolve the current instant for the undated surfaces.</summary>
    private readonly TimeProvider _timeProvider;

    /// <summary>The HTTP client owned by this provider, disposed with it; <see langword="null" /> when the client was supplied by the caller and its lifetime is the caller's responsibility.</summary>
    private readonly HttpClient? _ownedHttpClient;

    /// <summary>Coalesces concurrent loads keyed by a string so callers requesting the same endpoint window share a single in-flight fetch rather than each issuing a duplicate request. Used by derived types through <see cref="LoadCoalescedAsync(string, Func{CancellationToken, Task}, CancellationToken)" />.</summary>
    private readonly SingleFlightCoordinator<string> _loadCoordinator = new();

    /// <summary>The current immutable book backing range queries; replaced under <see cref="_gate" /> after each fetch.</summary>
    private volatile ExchangeRateBook _book;

    /// <summary>The current immutable lookup snapshot; replaced under <see cref="_gate" /> after each fetch.</summary>
    private volatile FixedDatedExchangeRateProvider _snapshot;

    /// <summary>Tracks whether this instance has been disposed so disposal is idempotent.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebExchangeRateProvider" /> class.
    /// </summary>
    /// <param name="ownedHttpClient">
    /// The HTTP client this provider should own and dispose, or <see langword="null" /> when the client is
    /// caller-supplied and its lifetime is the caller's responsibility.
    /// </param>
    /// <param name="timeProvider">
    /// The time source used to resolve the current instant for the undated surfaces. <see langword="null" /> selects
    /// <see cref="TimeProvider.System" />.
    /// </param>
    protected WebExchangeRateProvider(HttpClient? ownedHttpClient, TimeProvider? timeProvider)
    {
        _ownedHttpClient = ownedHttpClient;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _book = _builder.ToBook();
        _snapshot = new FixedDatedExchangeRateProvider(_book);
    }

    /// <summary>
    /// Gets the provider identifier stamped on every rate this provider produces.
    /// </summary>
    /// <value>The provider identifier.</value>
    protected abstract string ProviderId { get; }

    /// <summary>
    /// Gets a value indicating whether a synchronous lookup may block to fetch a missing window on demand.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when synchronous getters may block on the network; otherwise <see langword="false" />.
    /// </value>
    protected abstract bool AllowSynchronousNetworkAccess { get; }

    /// <summary>
    /// Gets the look-back window used when a single-rate lookup must fetch on demand; the provider fetches the window
    /// ending on the requested date and spanning this duration.
    /// </summary>
    /// <value>The look-back window.</value>
    protected abstract TimeSpan DefaultLookback { get; }

    /// <summary>
    /// Gets the synchronization object guarding the accumulator and snapshot; derived types lock on it while checking
    /// coverage and accumulating a fetch so the fetch publishes atomically.
    /// </summary>
    /// <value>The synchronization object.</value>
    protected object SyncRoot => _gate;

    /// <summary>
    /// Gets the time source used to resolve the current instant.
    /// </summary>
    /// <value>The time provider.</value>
    protected TimeProvider TimeProvider => _timeProvider;

    /// <inheritdoc />
    public ExchangeRateLookupResult GetRate(string fromIsoCode, string toIsoCode, ExchangeRateLookupOptions? options = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        return GetRate(fromIsoCode, toIsoCode, today, options ?? ExchangeRateLookupOptions.PreviousWithin(LatestRateToleranceDays));
    }

    /// <inheritdoc />
    public ExchangeRateLookupResult GetRate(string fromIsoCode, string toIsoCode, DateOnly date, ExchangeRateLookupOptions? options = null) =>
        TryGetRate(fromIsoCode, toIsoCode, date, options, out ExchangeRateLookupResult result)
            ? result
            : throw new KeyNotFoundException(FormatRateNotFound(fromIsoCode, toIsoCode, date));

    /// <inheritdoc />
    public bool TryGetRate(string fromIsoCode, string toIsoCode, DateOnly date, ExchangeRateLookupOptions? options, out ExchangeRateLookupResult result)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_snapshot.TryGetRate(fromIsoCode, toIsoCode, date, options, out result))
            return true;

        if (AllowSynchronousNetworkAccess && TryLoadForDate(fromIsoCode, toIsoCode, date))
        {
            OnSynchronousNetworkFetch(date);
            return _snapshot.TryGetRate(fromIsoCode, toIsoCode, date, options, out result);
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public ExchangeRateRangeResult GetRates(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        FinancialThrowHelper.ThrowIfNotValidIsoCode(fromIsoCode);
        FinancialThrowHelper.ThrowIfNotValidIsoCode(toIsoCode);
        if (endDate < startDate)
            throw CreateRangeInvertedException(startDate, endDate);
        ValidateRangeRequest(fromIsoCode, toIsoCode, startDate, endDate);

        ExchangeRatePair pair = new(CurrencyInfo.ParseCurrencyCode(fromIsoCode), CurrencyInfo.ParseCurrencyCode(toIsoCode));

        if (AllowSynchronousNetworkAccess && !IsLoaded(pair, startDate, endDate))
            BlockingLoad(pair, startDate, endDate);

        return _snapshot.GetRates(fromIsoCode, toIsoCode, startDate, endDate);
    }

    /// <inheritdoc />
    public ValueTask<ExchangeRateLookupResult> GetRateAsync(
        string fromIsoCode,
        string toIsoCode,
        ExchangeRateLookupOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        return GetRateAsync(
            fromIsoCode,
            toIsoCode,
            today,
            options ?? ExchangeRateLookupOptions.PreviousWithin(LatestRateToleranceDays),
            cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<ExchangeRateLookupResult> GetRateAsync(
        string fromIsoCode,
        string toIsoCode,
        DateOnly date,
        ExchangeRateLookupOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!string.Equals(fromIsoCode, toIsoCode, StringComparison.Ordinal))
        {
            FinancialThrowHelper.ThrowIfNotValidIsoCode(fromIsoCode);
            FinancialThrowHelper.ThrowIfNotValidIsoCode(toIsoCode);

            ExchangeRatePair pair = new(CurrencyInfo.ParseCurrencyCode(fromIsoCode), CurrencyInfo.ParseCurrencyCode(toIsoCode));
            DateOnly startDate = date.AddDays(-(int)DefaultLookback.TotalDays);
            await EnsureLoadedAsync(pair, startDate, date, cancellationToken).ConfigureAwait(false);
        }

        return _snapshot.TryGetRate(fromIsoCode, toIsoCode, date, options, out ExchangeRateLookupResult result)
            ? result
            : throw new KeyNotFoundException(FormatRateNotFound(fromIsoCode, toIsoCode, date));
    }

    /// <inheritdoc />
    public async ValueTask<ExchangeRateRangeResult> GetRatesAsync(
        string fromIsoCode,
        string toIsoCode,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        FinancialThrowHelper.ThrowIfNotValidIsoCode(fromIsoCode);
        FinancialThrowHelper.ThrowIfNotValidIsoCode(toIsoCode);
        if (endDate < startDate)
            throw CreateRangeInvertedException(startDate, endDate);
        ValidateRangeRequest(fromIsoCode, toIsoCode, startDate, endDate);

        ExchangeRatePair pair = new(CurrencyInfo.ParseCurrencyCode(fromIsoCode), CurrencyInfo.ParseCurrencyCode(toIsoCode));
        await EnsureLoadedAsync(pair, startDate, endDate, cancellationToken).ConfigureAwait(false);

        return _snapshot.GetRates(fromIsoCode, toIsoCode, startDate, endDate);
    }

    /// <inheritdoc />
    decimal IExchangeRateProvider.GetRate(string fromIsoCode, string toIsoCode) =>
        GetRate(fromIsoCode, toIsoCode).Rate.Rate;

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Ensures the inclusive window for a pair has been fetched and accumulated, idempotently. Implementations perform
    /// their own coverage check, request coalescing, fetch, and accumulation (via
    /// <see cref="AddObservations(IEnumerable{ExchangeRate}, DateTimeOffset?)" /> and <see cref="RebuildSnapshot" />
    /// under <see cref="SyncRoot" />).
    /// </summary>
    /// <param name="pair">
    /// The currency pair to ensure data for. Feeds that fetch by range, feed, or file may ignore it.
    /// </param>
    /// <param name="startDate">The inclusive start of the window.</param>
    /// <param name="endDate">The inclusive end of the window.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the fetch.</param>
    /// <returns>A task that completes when the window has been loaded.</returns>
    protected abstract ValueTask EnsureLoadedAsync(ExchangeRatePair pair, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken);

    /// <summary>
    /// Reports whether the inclusive window for a pair has already been fetched, so the synchronous lookup path can
    /// skip a redundant blocking fetch.
    /// </summary>
    /// <param name="pair">The currency pair to test.</param>
    /// <param name="startDate">The inclusive start of the window.</param>
    /// <param name="endDate">The inclusive end of the window.</param>
    /// <returns>
    /// <see langword="true" /> when the window is already covered; otherwise <see langword="false" />.
    /// </returns>
    protected abstract bool IsLoaded(ExchangeRatePair pair, DateOnly startDate, DateOnly endDate);

    /// <summary>
    /// Runs <paramref name="load" /> for <paramref name="key" />, or joins the load already in flight for that key, so
    /// concurrent callers requesting the same endpoint window share a single fetch rather than each issuing a duplicate
    /// request. Derived types call this from
    /// <see cref="EnsureLoadedAsync(ExchangeRatePair, DateOnly, DateOnly, CancellationToken)" /> with a key identifying
    /// the unit they download — an era, a feed, a date range, a pair-and-window.
    /// </summary>
    /// <param name="key">The key identifying the load; equal keys share one in-flight fetch.</param>
    /// <param name="load">The fetch to run on a miss, invoked with a token decoupled from any single caller.</param>
    /// <param name="cancellationToken">A token that abandons this caller's wait on the shared fetch.</param>
    /// <returns>A task that completes when the load for <paramref name="key" /> completes.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> or <paramref name="load" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="cancellationToken" /> is signalled before the shared fetch completes; the shared
    /// fetch continues to completion for any other joiner.
    /// </exception>
    /// <remarks>
    /// The shared fetch runs under <see cref="CancellationToken.None" />, so one caller's cancellation abandons only
    /// its own wait and never faults the fetch for the other joiners — appropriate for the idempotent cache-warming
    /// loads whose result populates the shared snapshot. The in-flight entry is released as soon as the fetch
    /// completes, including on failure, so a fault never poisons the key and the next caller starts a fresh attempt.
    /// </remarks>
    protected Task LoadCoalescedAsync(string key, Func<CancellationToken, Task> load, CancellationToken cancellationToken)
    {
        ThrowHelper.ThrowIfNull(key);

        return _loadCoordinator.RunAsync(key, load, cancellationToken);
    }

    /// <summary>
    /// Upserts a batch of fetched observations into the accumulator under the provider's identifier, invoking
    /// <see cref="OnObservationIngested(ExchangeRate)" /> for each. The caller must hold <see cref="SyncRoot" /> and
    /// follow the batch with <see cref="RebuildSnapshot" />.
    /// </summary>
    /// <param name="rates">The observations to upsert.</param>
    /// <param name="fetchedAtUtc">
    /// The UTC instant at which the batch was downloaded, recorded at the series grain as load provenance, or
    /// <see langword="null" /> when not tracked.
    /// </param>
    /// <returns>The number of observations upserted.</returns>
    protected int AddObservations(IEnumerable<ExchangeRate> rates, DateTimeOffset? fetchedAtUtc = null)
    {
        ThrowHelper.ThrowIfNull(rates);

        int count = 0;
        foreach (ExchangeRate rate in rates)
        {
            _builder.Upsert(rate.Pair, ProviderId, rate.Date, rate.Rate, fetchedAtUtc);
            OnObservationIngested(rate);
            count++;
        }

        return count;
    }

    /// <summary>
    /// Rebuilds the immutable book and lookup snapshot from the accumulator. The caller must hold
    /// <see cref="SyncRoot" />.
    /// </summary>
    protected void RebuildSnapshot()
    {
        _book = _builder.ToBook();
        _snapshot = new FixedDatedExchangeRateProvider(_book);
    }

    /// <summary>
    /// Called once per observation as it is ingested, for derived-type diagnostics. The default does nothing.
    /// </summary>
    /// <param name="rate">The observation being ingested.</param>
    protected virtual void OnObservationIngested(ExchangeRate rate)
    {
    }

    /// <summary>
    /// Called after a synchronous lookup blocks to fetch on demand, for derived-type diagnostics. The default does
    /// nothing.
    /// </summary>
    /// <param name="date">The date around which the fetch was performed.</param>
    protected virtual void OnSynchronousNetworkFetch(DateOnly date)
    {
    }

    /// <summary>
    /// Validates a range request against feed-specific preconditions before any fetch is attempted. The default does
    /// nothing; derived types may override to reject unsupported pairs (for example, a single-issuer feed that quotes
    /// only against one base currency).
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    protected virtual void ValidateRangeRequest(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate)
    {
    }

    /// <summary>
    /// Creates the exception thrown when an inclusive date range is inverted. Derived types may override to throw a
    /// feed-specific exception type.
    /// </summary>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <returns>The exception to throw.</returns>
    protected virtual Exception CreateRangeInvertedException(DateOnly startDate, DateOnly endDate) =>
        new ArgumentException(FinancialResourceStrings.Arg_Invalid_ExchangeRateRangeInverted, nameof(endDate));

    /// <summary>
    /// Formats the message for the <see cref="KeyNotFoundException" /> thrown when a single-rate lookup fails. Derived
    /// types may override to use a feed-specific resource string.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="date">The requested date.</param>
    /// <returns>The exception message.</returns>
    protected virtual string FormatRateNotFound(string fromIsoCode, string toIsoCode, DateOnly date) =>
        string.Format(
            CultureInfo.CurrentCulture,
            FinancialResourceStrings.IO_KeyNotFound_DatedExchangeRate,
            fromIsoCode,
            toIsoCode,
            date,
            ExchangeRateDateResolution.Exact,
            0);

    /// <summary>
    /// Releases the resources used by this provider; disposes the owned <see cref="HttpClient" /> when one was created.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true" /> when called from <see cref="Dispose()" />; <see langword="false" /> when called from a
    /// finalizer.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
            _ownedHttpClient?.Dispose();

        _disposed = true;
    }

    /// <summary>
    /// Synchronously fetches a window around a date for the on-demand single-rate path, unless the pair is a
    /// same-currency request or the window is already covered.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="date">The date around which to fetch.</param>
    /// <returns><see langword="true" /> when a fetch was attempted; otherwise <see langword="false" />.</returns>
    private bool TryLoadForDate(string fromIsoCode, string toIsoCode, DateOnly date)
    {
        if (string.Equals(fromIsoCode, toIsoCode, StringComparison.Ordinal))
            return false;

        ExchangeRatePair pair = new(CurrencyInfo.ParseCurrencyCode(fromIsoCode), CurrencyInfo.ParseCurrencyCode(toIsoCode));
        DateOnly startDate = date.AddDays(-(int)DefaultLookback.TotalDays);

        if (IsLoaded(pair, startDate, date))
            return false;

        BlockingLoad(pair, startDate, date);
        return true;
    }

    /// <summary>
    /// Blocks on the asynchronous load for the supplied window. Used only on the opt-in synchronous network path.
    /// </summary>
    /// <param name="pair">The currency pair to fetch.</param>
    /// <param name="startDate">The inclusive start of the window.</param>
    /// <param name="endDate">The inclusive end of the window.</param>
    private void BlockingLoad(ExchangeRatePair pair, DateOnly startDate, DateOnly endDate)
    {
#pragma warning disable VSTHRD002 // Intentional opt-in synchronous fetch, gated by AllowSynchronousNetworkAccess.
        EnsureLoadedAsync(pair, startDate, endDate, CancellationToken.None).AsTask().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
    }
}

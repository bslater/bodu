// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WebRateProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Provides the shared machinery for an exchange-rate provider that materializes a remote feed into an in-memory
/// <see cref="RateBook" /> snapshot: it owns the accumulator and the immutable snapshot, implements the full
/// synchronous and asynchronous lookup matrix once, and optionally owns the <see cref="HttpClient" /> used to reach the
/// feed. Derived types supply only the feed-specific fetch.
/// </summary>
/// <remarks>
/// <para>
/// <strong>HttpClient ownership.</strong> A derived provider constructed without a caller-supplied client builds and
/// owns its own (typically through <see cref="RateProviderHttpClientFactory.Create(string?, TimeSpan, long)" />),
/// passing it to this base so it is disposed with the provider. A provider constructed with a caller-supplied client
/// passes <see langword="null" /> as the owned client, leaving its lifetime — and its HTTP contract (user agent,
/// timeout) — to the caller. This is the path a dependency-injection registration uses, supplying a client from
/// <c>IHttpClientFactory</c>.
/// </para>
/// <para>
/// <strong>Lookup matrix.</strong> All getters resolve against the current immutable snapshot. The synchronous point
/// and range getters block to fetch on a miss only when <see cref="AllowSynchronousNetworkAccess" /> is enabled; the
/// asynchronous getters always await a coverage-aware fetch. The undated getters resolve the most recent rate as of the
/// current instant supplied by the time provider. Derived types implement the feed-specific
/// <see cref="EnsureLoadedAsync(CurrencyPair, DateOnly, DateOnly, CancellationToken)" /> and
/// <see cref="IsLoaded(CurrencyPair, DateOnly, DateOnly)" /> and accumulate fetched observations through
/// <see cref="AddObservations(IEnumerable{ExchangeRate}, DateTimeOffset?)" /> followed by
/// <see cref="RebuildSnapshot" />, all under <see cref="SyncRoot" />.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Financial;
///
/// // A derived provider supplies only the feed-specific fetch and coverage check.
/// sealed class MyFeedProvider : WebRateProvider
/// {
///     public MyFeedProvider(HttpClient client) : base(client, timeProvider: null) { }
///
///     protected override string ProviderId => "MyFeed";
///     protected override bool AllowSynchronousNetworkAccess => false;
///     protected override TimeSpan DefaultLookback => TimeSpan.FromDays(7);
///
///     protected override bool IsLoaded(CurrencyPair pair, DateOnly startDate, DateOnly endDate) =>
///         false;   // real implementations track covered windows
///
///     protected override async ValueTask EnsureLoadedAsync(
///         CurrencyPair pair, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken)
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
public abstract class WebRateProvider
    : IDatedRateProvider, IRateProvider, IPairRateLoader, IHistoricalRateProvider, IDisposable
{
    /// <summary>The tolerance, in days, used to resolve the most recent rate for the undated surfaces; large enough to reach any rate fetched into the store from the current date.</summary>
    private const int LatestRateToleranceDays = 100_000;

    /// <summary>Guards mutation of the accumulator and the snapshot fields, and is shared with derived types for their own coverage and series indexes so a fetch publishes atomically.</summary>
    private readonly object _gate = new();

    /// <summary>The accumulator into which each fetched observation is upserted.</summary>
    private readonly RateTableBuilder _builder = new();

    /// <summary>The time source used to resolve the current instant for the undated surfaces.</summary>
    private readonly TimeProvider _timeProvider;

    /// <summary>The HTTP client owned by this provider, disposed with it; <see langword="null" /> when the client was supplied by the caller and its lifetime is the caller's responsibility.</summary>
    private readonly HttpClient? _ownedHttpClient;

    /// <summary>Coalesces concurrent loads keyed by a string so callers requesting the same endpoint window share a single in-flight fetch rather than each issuing a duplicate request. Used by derived types through <see cref="LoadCoalescedAsync(string, Func{CancellationToken, Task}, CancellationToken)" />.</summary>
    private readonly SingleFlightCoordinator<string> _loadCoordinator = new();

    /// <summary>The current immutable book backing range queries; replaced under <see cref="_gate" /> after each fetch.</summary>
    private volatile RateBook _book;

    /// <summary>The current immutable lookup snapshot; replaced under <see cref="_gate" /> after each fetch.</summary>
    private volatile FixedDatedRateProvider _snapshot;

    /// <summary>Tracks whether this instance has been disposed so disposal is idempotent.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebRateProvider" /> class.
    /// </summary>
    /// <param name="ownedHttpClient">
    /// The HTTP client this provider should own and dispose, or <see langword="null" /> when the client is
    /// caller-supplied and its lifetime is the caller's responsibility.
    /// </param>
    /// <param name="timeProvider">
    /// The time source used to resolve the current instant for the undated surfaces. <see langword="null" /> selects
    /// <see cref="TimeProvider.System" />.
    /// </param>
    protected WebRateProvider(HttpClient? ownedHttpClient, TimeProvider? timeProvider)
    {
        _ownedHttpClient = ownedHttpClient;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _book = _builder.ToBook();
        _snapshot = new FixedDatedRateProvider(_book);
    }

    /// <summary>
    /// Gets the provider identifier stamped on every rate this provider produces.
    /// </summary>
    /// <value>The provider identifier.</value>
    protected abstract string ProviderId { get; }

    /// <summary>
    /// Gets the history depth this provider advertises: how far back it can serve rates.
    /// </summary>
    /// <value>
    /// The advertised availability; the base reports <see cref="RateHistoryAvailability.Unbounded" />. A derived type
    /// whose feed publishes only a bounded window overrides this to declare it.
    /// </value>
    public virtual RateHistoryAvailability HistoryAvailability => RateHistoryAvailability.Unbounded;

    /// <summary>
    /// Gets a value indicating whether a synchronous lookup may block to fetch a missing window on demand.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when synchronous getters may block on the network; otherwise <see langword="false" />.
    /// </value>
    /// <remarks>
    /// When enabled, the synchronous getters block on the async fetch, which can deadlock if invoked on a thread
    /// carrying a captured <see cref="SynchronizationContext" /> (classic ASP.NET, a WPF/WinForms UI thread). The
    /// synchronous path guards against this by throwing <see cref="InvalidOperationException" /> when
    /// <see cref="SynchronizationContext.Current" /> is non-null; enable this only for code that calls the getters from
    /// a thread-pool thread (or use the asynchronous API).
    /// </remarks>
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
    public RateLookupResult GetRate(string fromIsoCode, string toIsoCode, RateLookupOptions? options = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        return GetRate(fromIsoCode, toIsoCode, today, options ?? RateLookupOptions.PreviousWithin(LatestRateToleranceDays));
    }

    /// <inheritdoc />
    public RateLookupResult GetRate(string fromIsoCode, string toIsoCode, DateOnly date, RateLookupOptions? options = null) =>
        TryGetRate(fromIsoCode, toIsoCode, date, options, out RateLookupResult result)
            ? result
            : throw new KeyNotFoundException(FormatRateNotFound(fromIsoCode, toIsoCode, date));

    /// <inheritdoc />
    public bool TryGetRate(string fromIsoCode, string toIsoCode, DateOnly date, RateLookupOptions? options, out RateLookupResult result)
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
    public RateRangeResult GetRates(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ExchangeRatesThrowHelper.ThrowIfNotValidIsoCode(fromIsoCode);
        ExchangeRatesThrowHelper.ThrowIfNotValidIsoCode(toIsoCode);
        if (endDate < startDate)
            throw CreateRangeInvertedException(startDate, endDate);
        ValidateRangeRequest(fromIsoCode, toIsoCode, startDate, endDate);

        CurrencyPair pair = new(CurrencyInfo.ParseCurrencyCode(fromIsoCode), CurrencyInfo.ParseCurrencyCode(toIsoCode));

        if (AllowSynchronousNetworkAccess && !IsLoaded(pair, startDate, endDate))
            BlockingLoad(pair, startDate, endDate);

        return _snapshot.GetRates(fromIsoCode, toIsoCode, startDate, endDate);
    }

    /// <inheritdoc />
    public ValueTask<RateLookupResult> GetRateAsync(
        string fromIsoCode,
        string toIsoCode,
        RateLookupOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        return GetRateAsync(
            fromIsoCode,
            toIsoCode,
            today,
            options ?? RateLookupOptions.PreviousWithin(LatestRateToleranceDays),
            cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<RateLookupResult> GetRateAsync(
        string fromIsoCode,
        string toIsoCode,
        DateOnly date,
        RateLookupOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!string.Equals(fromIsoCode, toIsoCode, StringComparison.Ordinal))
        {
            ExchangeRatesThrowHelper.ThrowIfNotValidIsoCode(fromIsoCode);
            ExchangeRatesThrowHelper.ThrowIfNotValidIsoCode(toIsoCode);

            CurrencyPair pair = new(CurrencyInfo.ParseCurrencyCode(fromIsoCode), CurrencyInfo.ParseCurrencyCode(toIsoCode));
            DateOnly startDate = date.AddDays(-(int)DefaultLookback.TotalDays);
            await EnsureLoadedAsync(pair, startDate, date, cancellationToken).ConfigureAwait(false);
        }

        return _snapshot.TryGetRate(fromIsoCode, toIsoCode, date, options, out RateLookupResult result)
            ? result
            : throw new KeyNotFoundException(FormatRateNotFound(fromIsoCode, toIsoCode, date));
    }

    /// <inheritdoc />
    public async ValueTask<RateRangeResult> GetRatesAsync(
        string fromIsoCode,
        string toIsoCode,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ExchangeRatesThrowHelper.ThrowIfNotValidIsoCode(fromIsoCode);
        ExchangeRatesThrowHelper.ThrowIfNotValidIsoCode(toIsoCode);
        if (endDate < startDate)
            throw CreateRangeInvertedException(startDate, endDate);
        ValidateRangeRequest(fromIsoCode, toIsoCode, startDate, endDate);

        CurrencyPair pair = new(CurrencyInfo.ParseCurrencyCode(fromIsoCode), CurrencyInfo.ParseCurrencyCode(toIsoCode));
        await EnsureLoadedAsync(pair, startDate, endDate, cancellationToken).ConfigureAwait(false);

        return _snapshot.GetRates(fromIsoCode, toIsoCode, startDate, endDate);
    }

    /// <inheritdoc />
    /// <remarks>
    /// The load is delegated to the feed-specific
    /// <see cref="EnsureLoadedAsync(CurrencyPair, DateOnly, DateOnly, CancellationToken)" />, so it warms whatever unit
    /// the provider downloads — a single pair, an era, a feed, or a date range — to cover the requested window.
    /// </remarks>
    public Task LoadPairAsync(
        string fromIsoCode,
        string toIsoCode,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ExchangeRatesThrowHelper.ThrowIfNotValidIsoCode(fromIsoCode);
        ExchangeRatesThrowHelper.ThrowIfNotValidIsoCode(toIsoCode);
        if (endDate < startDate)
            throw CreateRangeInvertedException(startDate, endDate);
        ValidateRangeRequest(fromIsoCode, toIsoCode, startDate, endDate);

        CurrencyPair pair = new(CurrencyInfo.ParseCurrencyCode(fromIsoCode), CurrencyInfo.ParseCurrencyCode(toIsoCode));
        return EnsureLoadedAsync(pair, startDate, endDate, cancellationToken).AsTask();
    }

    /// <inheritdoc />
    public IReadOnlyCollection<CurrencyPair> GetLoadedPairs()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        RateBook book = _book;
        HashSet<CurrencyPair> pairs = new();
        foreach (RateSeries series in book.EnumerateSeries())
            pairs.Add(series.Pair);

        return pairs;
    }

    /// <summary>
    /// Returns the immutable book of every observation this provider has fetched and accumulated so far.
    /// </summary>
    /// <returns>The current immutable <see cref="RateBook" />; empty until the first fetch completes.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the provider has been disposed.</exception>
    /// <remarks>
    /// <para>
    /// The returned book is immutable and pinned at call time: later fetches replace the provider's internal book
    /// wholesale and never mutate an instance already handed out, so the result is safe to share across threads, to
    /// query after the provider is disposed, and to use as a deterministic offline snapshot. Call again after further
    /// loads to observe newly accumulated data; no reference identity is promised across calls.
    /// </para>
    /// <para>
    /// The book is the composable export primitive: rewrap it with
    /// <see cref="FixedDatedRateProvider(RateBook, IEnumerable{string})" /> to apply a custom provider-priority policy,
    /// or use <see cref="RateBook.ToBuilder" /> to edit a copy. For the ready-to-query equivalent see
    /// <see cref="GetLoadedSnapshot" />.
    /// </para>
    /// </remarks>
    public RateBook GetLoadedBook()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _book;
    }

    /// <summary>
    /// Returns an immutable, ready-to-query provider over every observation this provider has fetched and accumulated
    /// so far.
    /// </summary>
    /// <returns>
    /// The current immutable <see cref="FixedDatedRateProvider" /> snapshot; it resolves no rates until the first fetch
    /// completes.
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown when the provider has been disposed.</exception>
    /// <remarks>
    /// The snapshot is the instance this provider itself reads from — it is rebuilt once per fetch, so handing it out
    /// costs nothing — and it is pinned at call time: later fetches replace it wholesale and never mutate an instance
    /// already handed out. Use it for deterministic, offline, disposal-independent lookups over what has been loaded.
    /// Its <see cref="FixedDatedRateProvider.Book" /> is the same instance <see cref="GetLoadedBook" /> returns at the
    /// same moment.
    /// </remarks>
    public FixedDatedRateProvider GetLoadedSnapshot()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _snapshot;
    }

    /// <inheritdoc />
    decimal IRateProvider.GetRate(string fromIsoCode, string toIsoCode) =>
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
    protected abstract ValueTask EnsureLoadedAsync(CurrencyPair pair, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken);

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
    protected abstract bool IsLoaded(CurrencyPair pair, DateOnly startDate, DateOnly endDate);

    /// <summary>
    /// Runs <paramref name="load" /> for <paramref name="key" />, or joins the load already in flight for that key, so
    /// concurrent callers requesting the same endpoint window share a single fetch rather than each issuing a duplicate
    /// request. Derived types call this from
    /// <see cref="EnsureLoadedAsync(CurrencyPair, DateOnly, DateOnly, CancellationToken)" /> with a key identifying the
    /// unit they download — an era, a feed, a date range, a pair-and-window.
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
        _snapshot = new FixedDatedRateProvider(_book);
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
        new ArgumentException(ExchangeRatesResourceStrings.Arg_Invalid_ExchangeRateRangeInverted, nameof(endDate));

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
            ExchangeRatesResourceStrings.IO_KeyNotFound_DatedExchangeRate,
            fromIsoCode,
            toIsoCode,
            date,
            RateDateResolution.Exact,
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

        CurrencyPair pair = new(CurrencyInfo.ParseCurrencyCode(fromIsoCode), CurrencyInfo.ParseCurrencyCode(toIsoCode));
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
    /// <exception cref="InvalidOperationException">
    /// The current thread has a captured <see cref="SynchronizationContext" />, on which blocking the async load can
    /// deadlock.
    /// </exception>
    private void BlockingLoad(CurrencyPair pair, DateOnly startDate, DateOnly endDate)
    {
        // Blocking on async I/O from a thread with a captured SynchronizationContext (classic ASP.NET, a WPF/WinForms
        // UI thread) can deadlock if any awaited continuation posts back to that context. Convert that hang into an
        // immediate, diagnosable failure rather than letting the caller wedge.
        if (SynchronizationContext.Current is not null)
            throw new InvalidOperationException(ExchangeRatesResourceStrings.Op_Invalid_SynchronousNetworkAccessOnCapturedContext);

#pragma warning disable VSTHRD002 // Intentional opt-in synchronous fetch, gated by AllowSynchronousNetworkAccess.
        EnsureLoadedAsync(pair, startDate, endDate, CancellationToken.None).AsTask().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
    }
}

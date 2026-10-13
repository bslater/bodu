// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CoalescingTestWebRateProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// A minimal <see cref="WebRateProvider" /> that exposes the protected coalesced-load helpers, so their contract can be
/// exercised directly.
/// </summary>
internal sealed class CoalescingTestWebRateProvider
    : WebRateProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CoalescingTestWebRateProvider" /> class.
    /// </summary>
    public CoalescingTestWebRateProvider()
        : base(ownedHttpClient: null, timeProvider: null)
    {
    }

    /// <summary>
    /// Gets the provider's synchronization object.
    /// </summary>
    /// <value>The object the provider locks while it checks coverage and publishes.</value>
    public object Gate => SyncRoot;

    /// <inheritdoc />
    protected override string ProviderId => "TESTCOALESCE";

    /// <inheritdoc />
    protected override bool AllowSynchronousNetworkAccess => false;

    /// <inheritdoc />
    protected override TimeSpan DefaultLookback => TimeSpan.Zero;

    /// <summary>
    /// Calls the protected <see cref="WebRateProvider.LoadCoalescedAsync(string, Func{bool}, Func{CancellationToken, Task}, CancellationToken)" />.
    /// </summary>
    /// <param name="key">The key identifying the load.</param>
    /// <param name="isLoaded">The coverage check.</param>
    /// <param name="load">The load to run on a miss.</param>
    /// <param name="cancellationToken">A token that abandons this caller's wait.</param>
    /// <returns>The helper's task.</returns>
    public Task RunAsync(string key, Func<bool> isLoaded, Func<CancellationToken, Task> load, CancellationToken cancellationToken = default) =>
        LoadCoalescedAsync(key, isLoaded, load, cancellationToken);

    /// <inheritdoc />
    protected override ValueTask EnsureLoadedAsync(CurrencyPair pair, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken) =>
        ValueTask.CompletedTask;

    /// <inheritdoc />
    protected override bool IsLoaded(CurrencyPair pair, DateOnly startDate, DateOnly endDate) =>
        true;
}

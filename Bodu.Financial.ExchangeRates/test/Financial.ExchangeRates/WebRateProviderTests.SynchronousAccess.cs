// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WebRateProviderTests.SynchronousAccess.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class WebRateProviderTests
{
    /// <summary>
    /// Verifies that a synchronous range getter (with opt-in synchronous network access enabled) throws
    /// <see cref="InvalidOperationException" /> when invoked on a thread carrying a captured
    /// <see cref="SynchronizationContext" />, converting a potential deadlock into an immediate, diagnosable failure.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenSynchronousAccessOnCapturedContext_ShouldThrowInvalidOperationException()
    {
        SynchronizationContext? original = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

            using var provider = new TestSyncEnabledWebRateProvider();
            var date = new DateOnly(2024, 1, 2);

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                _ = provider.GetRates("AUD", "USD", date, date);
            });
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(original);
        }
    }

    /// <summary>
    /// Verifies that the same synchronous range getter proceeds normally when no
    /// <see cref="SynchronizationContext" /> is captured on the current thread (the thread-pool case), confirming the
    /// guard fires only on the deadlock-prone path.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenSynchronousAccessWithoutCapturedContext_ShouldNotThrowFromGuard()
    {
        SynchronizationContext? original = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(null);

            using var provider = new TestSyncEnabledWebRateProvider();
            var date = new DateOnly(2024, 1, 2);

            // Loads synchronously (the test double's EnsureLoadedAsync completes inline) and returns without the
            // guard tripping.
            _ = provider.GetRates("AUD", "USD", date, date);
            Assert.AreEqual(1, provider.LoadCount, "The synchronous path should have loaded exactly once.");
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(original);
        }
    }

    /// <summary>
    /// A <see cref="WebRateProvider" /> test double with opt-in synchronous network access enabled and an
    /// inline (already-completed) load, used to exercise the synchronous getter's <see cref="SynchronizationContext" />
    /// guard without real network I/O.
    /// </summary>
    private sealed class TestSyncEnabledWebRateProvider
        : WebRateProvider
    {
        private const string BaseIsoCode = "AUD";

        private bool _loaded;

        public TestSyncEnabledWebRateProvider()
            : base(ownedHttpClient: null, timeProvider: null)
        {
        }

        public int LoadCount { get; private set; }

        protected override string ProviderId => "TESTSYNC";

        protected override bool AllowSynchronousNetworkAccess => true;

        protected override TimeSpan DefaultLookback => TimeSpan.Zero;

        protected override ValueTask EnsureLoadedAsync(CurrencyPair pair, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken)
        {
            lock (SyncRoot)
            {
                if (!_loaded)
                {
                    LoadCount++;
                    _loaded = true;
                    RebuildSnapshot();
                }
            }

            return ValueTask.CompletedTask;
        }

        protected override bool IsLoaded(CurrencyPair pair, DateOnly startDate, DateOnly endDate)
        {
            lock (SyncRoot)
            {
                return _loaded;
            }
        }

        protected override void ValidateRangeRequest(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate)
        {
            if (!string.Equals(fromIsoCode, BaseIsoCode, StringComparison.Ordinal) &&
                !string.Equals(toIsoCode, BaseIsoCode, StringComparison.Ordinal))
            {
                throw new RateSeriesNotFoundException($"neither {fromIsoCode} nor {toIsoCode} is {BaseIsoCode}");
            }
        }
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedNotableDateCacheExpirationTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Distributed;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Verifies the server-side absolute expiration <see cref="DistributedNotableDateCache" /> stamps onto each written
/// territory blob: on by default with a one-hour margin, honouring a custom margin, and absent when the margin is
/// disabled.
/// </summary>
[TestClass]
public sealed class DistributedNotableDateCacheExpirationTests
{
    /// <summary>The fixed evaluation instant the tests write at.</summary>
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>The time-to-live the tests store under.</summary>
    private static readonly TimeSpan Ttl = TimeSpan.FromDays(30);

    /// <summary>
    /// Verifies that with the default margin a stored year's blob carries a server-side lifetime of the
    /// time-to-live plus one hour.
    /// </summary>
    [TestMethod]
    public void StoreYear_WhenDefaultMargin_ShouldStampAbsoluteExpiration()
    {
        var backing = new RecordingCalendarDistributedCache();
        var cache = new DistributedNotableDateCache(backing, new DistributedNotableDateCacheOptions());

        cache.StoreYear(NotableDateCacheTestFactory.Entry("US", 2026, "v1", Now), Ttl, Now);

        Assert.HasCount(1, backing.Sets);
        Assert.AreEqual(Ttl + TimeSpan.FromHours(1), backing.Sets[0].Options?.AbsoluteExpirationRelativeToNow);
    }

    /// <summary>
    /// Verifies that a custom margin is honoured, and that a version-superseding rewrite re-stamps the lifetime.
    /// </summary>
    [TestMethod]
    public void StoreYear_WhenVersionSupersedes_ShouldRestampExpirationOnTheRewrite()
    {
        var backing = new RecordingCalendarDistributedCache();
        var cache = new DistributedNotableDateCache(backing, new DistributedNotableDateCacheOptions
        {
            EntryExpirationMargin = TimeSpan.FromMinutes(10),
        });

        cache.StoreYear(NotableDateCacheTestFactory.Entry("US", 2026, "v1", Now), Ttl, Now);
        DateTimeOffset later = Now.AddHours(2);
        cache.StoreYear(NotableDateCacheTestFactory.Entry("US", 2026, "v2", later), Ttl, later);

        Assert.HasCount(2, backing.Sets);
        Assert.AreEqual(Ttl + TimeSpan.FromMinutes(10), backing.Sets[0].Options?.AbsoluteExpirationRelativeToNow);
        Assert.AreEqual(Ttl + TimeSpan.FromMinutes(10), backing.Sets[1].Options?.AbsoluteExpirationRelativeToNow);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> margin writes the blob without any server-side expiration.
    /// </summary>
    [TestMethod]
    public void StoreYear_WhenMarginDisabled_ShouldWriteWithoutServerSideExpiration()
    {
        var backing = new RecordingCalendarDistributedCache();
        var cache = new DistributedNotableDateCache(backing, new DistributedNotableDateCacheOptions
        {
            EntryExpirationMargin = null,
        });

        cache.StoreYear(NotableDateCacheTestFactory.Entry("US", 2026, "v1", Now), Ttl, Now);

        Assert.HasCount(1, backing.Sets);
        Assert.IsNull(backing.Sets[0].Options?.AbsoluteExpiration);
        Assert.IsNull(backing.Sets[0].Options?.AbsoluteExpirationRelativeToNow);
        Assert.IsNull(backing.Sets[0].Options?.SlidingExpiration);
    }

    /// <summary>
    /// Verifies that a negative margin is rejected with the offending parameter name.
    /// </summary>
    [TestMethod]
    public void Validate_WhenMarginNegative_ShouldThrowArgumentException()
    {
        var options = new DistributedNotableDateCacheOptions { EntryExpirationMargin = TimeSpan.FromSeconds(-1) };

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            options.Validate();
        });

        Assert.AreEqual(nameof(DistributedNotableDateCacheOptions.EntryExpirationMargin), ex.ParamName);
        Assert.IsFalse(options.TryValidate(out string? error));
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// An in-memory <see cref="IDistributedCache" /> that records the entry options supplied with every set.
    /// </summary>
    private sealed class RecordingCalendarDistributedCache : IDistributedCache
    {
        /// <summary>The stored payloads, keyed by cache key.</summary>
        private readonly ConcurrentDictionary<string, byte[]> _store = new();

        /// <summary>The recorded set calls, in order.</summary>
        private readonly List<(string Key, DistributedCacheEntryOptions? Options)> _sets = new();

        /// <summary>
        /// Gets the recorded set calls: the key and the entry options supplied.
        /// </summary>
        /// <value>The recorded sets, in call order.</value>
        public IReadOnlyList<(string Key, DistributedCacheEntryOptions? Options)> Sets
        {
            get
            {
                lock (_sets)
                    return _sets.ToArray();
            }
        }

        /// <inheritdoc />
        public byte[]? Get(string key) => _store.TryGetValue(key, out byte[]? value) ? value : null;

        /// <inheritdoc />
        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult(Get(key));

        /// <inheritdoc />
        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            _store[key] = value;
            lock (_sets)
                _sets.Add((key, options));
        }

        /// <inheritdoc />
        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Refresh(string key)
        {
        }

        /// <inheritdoc />
        public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

        /// <inheritdoc />
        public void Remove(string key) => _store.TryRemove(key, out _);

        /// <inheritdoc />
        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }
    }
}

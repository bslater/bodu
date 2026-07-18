// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryTests.Expiration.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentEvictingDictionaryTests
{
    /// <summary>
    /// Verifies that an expired entry is invisible to lookups even before it is physically purged.
    /// </summary>
    [TestMethod]
    public void Expiration_WhenEntryExpires_ShouldBeInvisibleToLookups()
    {
        var time = new ManualTimeProvider();
        var expiration = new EvictingDictionaryExpiration(TimeSpan.FromMinutes(1), EvictingDictionaryExpirationKind.Absolute, time);
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128, expiration);
        dictionary.Add("a", 1);

        time.Advance(TimeSpan.FromMinutes(2));

        Assert.IsFalse(dictionary.ContainsKey("a"));
        Assert.IsFalse(dictionary.TryGetValue("a", out _));
    }

    /// <summary>
    /// Verifies that a live entry within its time-to-live remains fully visible.
    /// </summary>
    [TestMethod]
    public void Expiration_WhenEntryStillLive_ShouldRemainVisible()
    {
        var time = new ManualTimeProvider();
        var expiration = new EvictingDictionaryExpiration(TimeSpan.FromMinutes(5), EvictingDictionaryExpirationKind.Absolute, time);
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128, expiration);
        dictionary.Add("a", 1);

        time.Advance(TimeSpan.FromMinutes(4));

        Assert.IsTrue(dictionary.TryGetValue("a", out int value));
        Assert.AreEqual(1, value);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentEvictingDictionary{TKey, TValue}.Count" /> reports the raw stored count,
    /// including expired-but-unpurged entries.
    /// </summary>
    [TestMethod]
    public void Expiration_WhenEntriesExpireUnpurged_ShouldStillCountRawEntries()
    {
        var time = new ManualTimeProvider();
        var expiration = new EvictingDictionaryExpiration(TimeSpan.FromMinutes(1), EvictingDictionaryExpirationKind.Absolute, time);
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128, expiration);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        time.Advance(TimeSpan.FromMinutes(2));

        Assert.AreEqual(2, dictionary.Count, "Count must include expired-but-unpurged entries.");
        Assert.IsEmpty(dictionary.ToArray(), "The snapshot must filter expired entries.");
    }

    /// <summary>
    /// Verifies that a sliding expiration is refreshed by a read access, keeping the entry alive past its original
    /// deadline.
    /// </summary>
    [TestMethod]
    public void Expiration_WhenSlidingAndRead_ShouldRefreshDeadline()
    {
        var time = new ManualTimeProvider();
        var expiration = new EvictingDictionaryExpiration(TimeSpan.FromMinutes(2), EvictingDictionaryExpirationKind.Sliding, time);
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128, expiration);
        dictionary.Add("a", 1);

        time.Advance(TimeSpan.FromMinutes(1));
        Assert.IsTrue(dictionary.TryGetValue("a", out _));
        time.Advance(TimeSpan.FromMinutes(1.5));

        Assert.IsTrue(dictionary.TryGetValue("a", out _), "The read at minute 1 must have restarted the countdown.");
    }

    /// <summary>
    /// Verifies that an absolute expiration is not refreshed by reads: the entry expires at its original deadline
    /// regardless of accesses.
    /// </summary>
    [TestMethod]
    public void Expiration_WhenAbsoluteAndRead_ShouldNotRefreshDeadline()
    {
        var time = new ManualTimeProvider();
        var expiration = new EvictingDictionaryExpiration(TimeSpan.FromMinutes(2), EvictingDictionaryExpirationKind.Absolute, time);
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128, expiration);
        dictionary.Add("a", 1);

        time.Advance(TimeSpan.FromMinutes(1));
        Assert.IsTrue(dictionary.TryGetValue("a", out _));
        time.Advance(TimeSpan.FromMinutes(1.5));

        Assert.IsFalse(dictionary.TryGetValue("a", out _), "Absolute expiration must ignore the earlier read.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentEvictingDictionary{TKey, TValue}.Touch" /> does not slide a sliding
    /// expiration deadline.
    /// </summary>
    [TestMethod]
    public void Expiration_WhenSlidingAndTouched_ShouldNotRefreshDeadline()
    {
        var time = new ManualTimeProvider();
        var expiration = new EvictingDictionaryExpiration(TimeSpan.FromMinutes(2), EvictingDictionaryExpirationKind.Sliding, time);
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128, expiration);
        dictionary.Add("a", 1);

        time.Advance(TimeSpan.FromMinutes(1));
        Assert.IsTrue(dictionary.Touch("a"));
        time.Advance(TimeSpan.FromMinutes(1.5));

        Assert.IsFalse(dictionary.TryGetValue("a", out _), "Touch must not slide the deadline.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentEvictingDictionary{TKey, TValue}.ContainsKey" /> is a pure read that does not
    /// slide a sliding expiration deadline, symmetric with <see cref="ConcurrentEvictingDictionary{TKey, TValue}.Touch" />.
    /// </summary>
    [TestMethod]
    public void Expiration_WhenSlidingAndContainsKey_ShouldNotRefreshDeadline()
    {
        var time = new ManualTimeProvider();
        var expiration = new EvictingDictionaryExpiration(TimeSpan.FromMinutes(2), EvictingDictionaryExpirationKind.Sliding, time);
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128, expiration);
        dictionary.Add("a", 1);

        time.Advance(TimeSpan.FromMinutes(1));
        Assert.IsTrue(dictionary.ContainsKey("a"));
        time.Advance(TimeSpan.FromMinutes(1.5));

        Assert.IsFalse(dictionary.TryGetValue("a", out _), "ContainsKey must not slide the deadline.");
    }

    /// <summary>
    /// Verifies that under sliding expiry a <see cref="ConcurrentEvictingDictionary{TKey, TValue}.ContainsKey" /> probe
    /// leaves the deadline intact while a later <see cref="ConcurrentEvictingDictionary{TKey, TValue}.TryGetValue" /> read
    /// slides it — the containment probe and the value read are asymmetric with respect to the deadline.
    /// </summary>
    [TestMethod]
    public void Expiration_WhenSlidingAndContainsKeyThenRead_ShouldSlideOnlyOnRead()
    {
        var time = new ManualTimeProvider();
        var expiration = new EvictingDictionaryExpiration(TimeSpan.FromMinutes(2), EvictingDictionaryExpirationKind.Sliding, time);
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128, expiration);
        dictionary.Add("a", 1);

        // A containment probe at minute 1 must not restart the countdown...
        time.Advance(TimeSpan.FromMinutes(1));
        Assert.IsTrue(dictionary.ContainsKey("a"));

        // ...but a value read at minute 1.5 must, keeping the entry alive past its original 2-minute deadline.
        time.Advance(TimeSpan.FromMinutes(0.5));
        Assert.IsTrue(dictionary.TryGetValue("a", out _));

        time.Advance(TimeSpan.FromMinutes(1.5));
        Assert.IsTrue(dictionary.TryGetValue("a", out _), "The read at minute 1.5 must have restarted the countdown.");
    }

    /// <summary>
    /// Verifies that a per-entry time-to-live override takes precedence over the dictionary default.
    /// </summary>
    [TestMethod]
    public void Expiration_WhenPerEntryTtlSupplied_ShouldOverrideDefault()
    {
        var time = new ManualTimeProvider();
        var expiration = new EvictingDictionaryExpiration(TimeSpan.FromMinutes(10), EvictingDictionaryExpirationKind.Absolute, time);
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128, expiration);
        dictionary.Add("short", 1, TimeSpan.FromMinutes(1));
        dictionary.Add("long", 2);

        time.Advance(TimeSpan.FromMinutes(2));

        Assert.IsFalse(dictionary.ContainsKey("short"));
        Assert.IsTrue(dictionary.ContainsKey("long"));
    }

    /// <summary>
    /// Verifies that the time-to-live overloads throw <see cref="InvalidOperationException" /> when the dictionary was
    /// constructed without an expiration configuration.
    /// </summary>
    [TestMethod]
    public void Expiration_WhenNotConfigured_ShouldThrowInvalidOperationExceptionOnTtlOverloads()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            dictionary.Add("a", 1, TimeSpan.FromMinutes(1));
        });

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = dictionary.TryAdd("a", 1, TimeSpan.FromMinutes(1));
        });

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = dictionary.GetOrAdd("a", 1, TimeSpan.FromMinutes(1));
        });

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = dictionary.GetOrAdd("a", _ => 1, TimeSpan.FromMinutes(1));
        });
    }

    /// <summary>
    /// Verifies that a non-positive time-to-live throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Expiration_WhenTtlIsZeroOrNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var expiration = new EvictingDictionaryExpiration(TimeSpan.FromMinutes(1));
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128, expiration);

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            dictionary.Add("a", 1, TimeSpan.Zero);
        });

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = dictionary.TryAdd("a", 1, TimeSpan.FromSeconds(-1));
        });
    }

    /// <summary>
    /// Verifies that capacity pressure purges expired entries before consulting the policy, so live entries are not
    /// evicted while expired entries occupy slots.
    /// </summary>
    [TestMethod]
    public void Expiration_WhenFullWithExpiredEntries_ShouldPurgeExpiredBeforePolicyEviction()
    {
        var time = new ManualTimeProvider();
        var expiration = new EvictingDictionaryExpiration(null, EvictingDictionaryExpirationKind.Absolute, time);
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 3, EvictingDictionaryPolicy.FirstInFirstOut, expiration);
        dictionary.Add("mortal", 1, TimeSpan.FromMinutes(1));
        dictionary.Add("keep1", 2);
        dictionary.Add("keep2", 3);

        time.Advance(TimeSpan.FromMinutes(2));
        dictionary.Add("new", 4);

        Assert.IsTrue(dictionary.ContainsKey("keep1"), "The expired entry must be the victim, not the oldest live entry.");
        Assert.IsTrue(dictionary.ContainsKey("keep2"));
        Assert.IsTrue(dictionary.ContainsKey("new"));
    }

    /// <summary>
    /// Verifies that a <see cref="ConcurrentEvictingDictionary{TKey, TValue}.TryAdd(TKey, TValue)" /> over an
    /// expired-but-unpurged entry succeeds, lazily removing the expired entry.
    /// </summary>
    [TestMethod]
    public void Expiration_WhenTryAddOverExpiredEntry_ShouldReplaceAndReturnTrue()
    {
        var time = new ManualTimeProvider();
        var expiration = new EvictingDictionaryExpiration(TimeSpan.FromMinutes(1), EvictingDictionaryExpirationKind.Absolute, time);
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128, expiration);
        dictionary.Add("a", 1);

        time.Advance(TimeSpan.FromMinutes(2));

        Assert.IsTrue(dictionary.TryAdd("a", 2));
        Assert.IsTrue(dictionary.TryGetValue("a", out int value));
        Assert.AreEqual(2, value);
    }

    /// <summary>
    /// Verifies that replacing a live entry restarts its lifetime as a fresh lease using the dictionary default.
    /// </summary>
    [TestMethod]
    public void Expiration_WhenValueReplaced_ShouldRestartLifetime()
    {
        var time = new ManualTimeProvider();
        var expiration = new EvictingDictionaryExpiration(TimeSpan.FromMinutes(2), EvictingDictionaryExpirationKind.Absolute, time);
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128, expiration);
        dictionary.Add("a", 1);

        time.Advance(TimeSpan.FromMinutes(1));
        dictionary.Add("a", 2);
        time.Advance(TimeSpan.FromMinutes(1.5));

        Assert.IsTrue(dictionary.TryGetValue("a", out int value), "The replacement at minute 1 must have restarted the lease.");
        Assert.AreEqual(2, value);
    }

    /// <summary>
    /// Verifies that without an expiration configuration entries never expire.
    /// </summary>
    [TestMethod]
    public void Expiration_WhenNotConfigured_ShouldNeverExpireEntries()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary.Add("a", 1);

        Assert.IsNull(dictionary.Expiration);
        Assert.IsTrue(dictionary.TryGetValue("a", out _));
    }
}

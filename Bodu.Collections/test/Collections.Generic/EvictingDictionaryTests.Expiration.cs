// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.Expiration.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

/// <summary>
/// Tests for the time-based expiration surface of <see cref="EvictingDictionary{TKey, TValue}" />: absolute and
/// sliding lifetimes, per-entry time-to-live overrides, lazy purge on access, <c>RemoveExpired</c>, the raw
/// <c>Count</c> contract, and the interaction between expiry and every capacity policy. All time is driven through a
/// manually advanced <see cref="TimeProvider" /> — no real sleeps.
/// </summary>
public partial class EvictingDictionaryTests
{
    // --------------------------------------------------------
    // Expiration property
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the Expiration property is null when the dictionary is constructed without an expiration
    /// configuration.
    /// </summary>
    [TestMethod]
    public void Expiration_WhenNotConfigured_ShouldBeNull()
    {
        var dictionary = new EvictingDictionary<string, int>(4);

        Assert.IsNull(dictionary.Expiration);
    }

    /// <summary>
    /// Verifies that the Expiration property returns the configuration instance supplied at construction, for each
    /// expiration-accepting constructor overload.
    /// </summary>
    [TestMethod]
    public void Expiration_WhenConfigured_ShouldReturnSuppliedInstance()
    {
        var expiration = new EvictingDictionaryExpiration(TimeSpan.FromMinutes(5));

        Assert.AreSame(expiration, new EvictingDictionary<string, int>(4, expiration).Expiration);
        Assert.AreSame(expiration, new EvictingDictionary<string, int>(4, EvictingDictionaryPolicy.FirstInFirstOut, expiration).Expiration);
        Assert.AreSame(expiration, new EvictingDictionary<string, int>(4, EvictingDictionaryPolicy.FirstInFirstOut, null, expiration).Expiration);
    }

    /// <summary>
    /// Verifies that the source-copying constructor applies the expiration configuration to the copied entries.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceAndExpirationProvided_ShouldApplyDefaultTimeToLiveToCopiedEntries()
    {
        var time = new ManualTimeProvider();
        var expiration = new EvictingDictionaryExpiration(TimeSpan.FromMinutes(5), EvictingDictionaryExpirationKind.Absolute, time);
        var source = new[] { new KeyValuePair<string, int>("A", 1), new KeyValuePair<string, int>("B", 2) };
        var dictionary = new EvictingDictionary<string, int>(4, source, EvictingDictionaryPolicy.LeastRecentlyUsed, null, expiration);

        Assert.AreSame(expiration, dictionary.Expiration);
        Assert.IsTrue(dictionary.ContainsKey("A"));

        time.Advance(TimeSpan.FromMinutes(6));

        Assert.IsFalse(dictionary.ContainsKey("A"));
        Assert.IsFalse(dictionary.ContainsKey("B"));
    }

    // --------------------------------------------------------
    // Absolute expiry
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that an entry with an absolute default time-to-live remains visible strictly before its deadline.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenAbsoluteTtlNotYetElapsed_ShouldReturnEntry()
    {
        var time = new ManualTimeProvider();
        var dictionary = new EvictingDictionary<string, int>(
            4, new EvictingDictionaryExpiration(TimeSpan.FromMinutes(10), EvictingDictionaryExpirationKind.Absolute, time));
        dictionary.Add("A", 1);

        time.Advance(TimeSpan.FromMinutes(10) - TimeSpan.FromTicks(1));

        Assert.IsTrue(dictionary.TryGetValue("A", out int value));
        Assert.AreEqual(1, value);
    }

    /// <summary>
    /// Verifies that an entry with an absolute default time-to-live becomes invisible exactly at its deadline.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenAbsoluteTtlExactlyElapsed_ShouldReportMiss()
    {
        var time = new ManualTimeProvider();
        var dictionary = new EvictingDictionary<string, int>(
            4, new EvictingDictionaryExpiration(TimeSpan.FromMinutes(10), EvictingDictionaryExpirationKind.Absolute, time));
        dictionary.Add("A", 1);

        time.Advance(TimeSpan.FromMinutes(10));

        Assert.IsFalse(dictionary.TryGetValue("A", out _));
    }

    /// <summary>
    /// Verifies that under absolute expiration a read access does not extend the entry's lifetime.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenAbsoluteKindAndEntryRead_ShouldNotExtendLifetime()
    {
        var time = new ManualTimeProvider();
        var dictionary = new EvictingDictionary<string, int>(
            4, new EvictingDictionaryExpiration(TimeSpan.FromMinutes(10), EvictingDictionaryExpirationKind.Absolute, time));
        dictionary.Add("A", 1);

        time.Advance(TimeSpan.FromMinutes(6));
        Assert.IsTrue(dictionary.TryGetValue("A", out _));

        time.Advance(TimeSpan.FromMinutes(6));

        Assert.IsFalse(dictionary.TryGetValue("A", out _));
    }

    /// <summary>
    /// Verifies that updating an existing entry through <see cref="EvictingDictionary{TKey, TValue}.Add(TKey, TValue)" />
    /// restarts its absolute lifetime from the time of the update.
    /// </summary>
    [TestMethod]
    public void Add_WhenExistingEntryUpdated_ShouldRestartAbsoluteLifetime()
    {
        var time = new ManualTimeProvider();
        var dictionary = new EvictingDictionary<string, int>(
            4, new EvictingDictionaryExpiration(TimeSpan.FromMinutes(10), EvictingDictionaryExpirationKind.Absolute, time));
        dictionary.Add("A", 1);

        time.Advance(TimeSpan.FromMinutes(6));
        dictionary.Add("A", 2);

        time.Advance(TimeSpan.FromMinutes(6));

        Assert.IsTrue(dictionary.TryGetValue("A", out int value));
        Assert.AreEqual(2, value);

        time.Advance(TimeSpan.FromMinutes(5));

        Assert.IsFalse(dictionary.TryGetValue("A", out _));
    }

    /// <summary>
    /// Verifies that updating an entry through plain <see cref="EvictingDictionary{TKey, TValue}.Add(TKey, TValue)" />
    /// discards a previous per-entry time-to-live override and re-applies the dictionary default.
    /// </summary>
    [TestMethod]
    public void Add_WhenEntryHadPerEntryTtlAndIsUpdatedWithoutOne_ShouldReapplyDefaultTimeToLive()
    {
        var time = new ManualTimeProvider();
        var dictionary = new EvictingDictionary<string, int>(
            4, new EvictingDictionaryExpiration(TimeSpan.FromMinutes(10), EvictingDictionaryExpirationKind.Absolute, time));
        dictionary.Add("A", 1, TimeSpan.FromMinutes(2));

        dictionary.Add("A", 2);
        time.Advance(TimeSpan.FromMinutes(5));

        Assert.IsTrue(dictionary.TryGetValue("A", out _), "The default lifetime should apply after the plain update.");

        time.Advance(TimeSpan.FromMinutes(6));

        Assert.IsFalse(dictionary.TryGetValue("A", out _));
    }

    /// <summary>
    /// Verifies that when the default time-to-live is null, entries added without a per-entry override never expire.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenDefaultTtlIsNullAndNoOverride_ShouldNeverExpire()
    {
        var time = new ManualTimeProvider();
        var dictionary = new EvictingDictionary<string, int>(
            4, new EvictingDictionaryExpiration(null, EvictingDictionaryExpirationKind.Absolute, time));
        dictionary.Add("A", 1);

        time.Advance(TimeSpan.FromDays(365));

        Assert.IsTrue(dictionary.TryGetValue("A", out _));
    }

    // --------------------------------------------------------
    // Sliding expiry
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that under sliding expiration a successful <see cref="EvictingDictionary{TKey, TValue}.TryGetValue" />
    /// refreshes the entry's deadline.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenSlidingKindAndEntryRead_ShouldExtendLifetime()
    {
        var time = new ManualTimeProvider();
        var dictionary = new EvictingDictionary<string, int>(
            4, new EvictingDictionaryExpiration(TimeSpan.FromMinutes(10), EvictingDictionaryExpirationKind.Sliding, time));
        dictionary.Add("A", 1);

        time.Advance(TimeSpan.FromMinutes(6));
        Assert.IsTrue(dictionary.TryGetValue("A", out _));

        time.Advance(TimeSpan.FromMinutes(6));

        Assert.IsTrue(dictionary.TryGetValue("A", out _), "The read at 6 minutes should have slid the deadline to 16 minutes.");

        time.Advance(TimeSpan.FromMinutes(11));

        Assert.IsFalse(dictionary.TryGetValue("A", out _));
    }

    /// <summary>
    /// Verifies that under sliding expiration the indexer getter refreshes the entry's deadline.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSlidingKindAndEntryRead_ShouldExtendLifetime()
    {
        var time = new ManualTimeProvider();
        var dictionary = new EvictingDictionary<string, int>(
            4, new EvictingDictionaryExpiration(TimeSpan.FromMinutes(10), EvictingDictionaryExpirationKind.Sliding, time));
        dictionary.Add("A", 1);

        time.Advance(TimeSpan.FromMinutes(6));
        Assert.AreEqual(1, dictionary["A"]);

        time.Advance(TimeSpan.FromMinutes(6));

        Assert.AreEqual(1, dictionary["A"]);
    }

    /// <summary>
    /// Verifies that under sliding expiration <see cref="EvictingDictionary{TKey, TValue}.ContainsKey" /> is a pure
    /// read that does not refresh the entry's deadline, so the entry still expires on its original schedule — whereas a
    /// <see cref="EvictingDictionary{TKey, TValue}.TryGetValue" /> read at the same point does slide it.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenSlidingKind_ShouldNotExtendLifetime()
    {
        var time = new ManualTimeProvider();
        var dictionary = new EvictingDictionary<string, int>(
            4, new EvictingDictionaryExpiration(TimeSpan.FromMinutes(10), EvictingDictionaryExpirationKind.Sliding, time));
        dictionary.Add("A", 1);

        time.Advance(TimeSpan.FromMinutes(6));
        Assert.IsTrue(dictionary.ContainsKey("A"), "The entry is still live at 6 minutes.");

        time.Advance(TimeSpan.FromMinutes(6));

        Assert.IsFalse(dictionary.ContainsKey("A"), "ContainsKey must not slide the deadline; the entry expires at 10 minutes.");

        // Contrast: a TryGetValue read at 6 minutes does slide the deadline, keeping the entry alive past 10 minutes.
        var slidingTime = new ManualTimeProvider();
        var sliding = new EvictingDictionary<string, int>(
            4, new EvictingDictionaryExpiration(TimeSpan.FromMinutes(10), EvictingDictionaryExpirationKind.Sliding, slidingTime));
        sliding.Add("A", 1);

        slidingTime.Advance(TimeSpan.FromMinutes(6));
        Assert.IsTrue(sliding.TryGetValue("A", out _), "The entry is live at 6 minutes.");

        slidingTime.Advance(TimeSpan.FromMinutes(6));
        Assert.IsTrue(sliding.ContainsKey("A"), "The TryGetValue hit at 6 minutes should have slid the deadline to 16 minutes.");
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.ContainsKey" /> does not count as a policy access
    /// (TotalTouches is unchanged).
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenSlidingKind_ShouldNotIncrementTotalTouches()
    {
        var time = new ManualTimeProvider();
        var dictionary = new EvictingDictionary<string, int>(
            4, new EvictingDictionaryExpiration(TimeSpan.FromMinutes(10), EvictingDictionaryExpirationKind.Sliding, time));
        dictionary.Add("A", 1);

        Assert.IsTrue(dictionary.ContainsKey("A"));

        Assert.AreEqual(0, dictionary.TotalTouches);
    }

    /// <summary>
    /// Verifies that under sliding expiration enumeration does not refresh entry deadlines.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenSlidingKind_ShouldNotExtendLifetime()
    {
        var time = new ManualTimeProvider();
        var dictionary = new EvictingDictionary<string, int>(
            4, new EvictingDictionaryExpiration(TimeSpan.FromMinutes(10), EvictingDictionaryExpirationKind.Sliding, time));
        dictionary.Add("A", 1);

        time.Advance(TimeSpan.FromMinutes(6));

        int seen = 0;
        foreach (KeyValuePair<string, int> kvp in dictionary)
            seen++;

        Assert.AreEqual(1, seen);

        time.Advance(TimeSpan.FromMinutes(6));

        Assert.IsFalse(dictionary.TryGetValue("A", out _), "Enumeration at 6 minutes must not slide the deadline past 10 minutes.");
    }

    /// <summary>
    /// Verifies that under sliding expiration <see cref="EvictingDictionary{TKey, TValue}.Touch" /> does not refresh
    /// the entry's expiration deadline (it affects capacity-policy metadata only).
    /// </summary>
    [TestMethod]
    public void Touch_WhenSlidingKind_ShouldNotExtendLifetime()
    {
        var time = new ManualTimeProvider();
        var dictionary = new EvictingDictionary<string, int>(
            4, new EvictingDictionaryExpiration(TimeSpan.FromMinutes(10), EvictingDictionaryExpirationKind.Sliding, time));
        dictionary.Add("A", 1);

        time.Advance(TimeSpan.FromMinutes(6));
        Assert.IsTrue(dictionary.Touch("A"));

        time.Advance(TimeSpan.FromMinutes(6));

        Assert.IsFalse(dictionary.TryGetValue("A", out _), "Touch at 6 minutes must not slide the deadline past 10 minutes.");
    }

    /// <summary>
    /// Verifies that a sliding refresh uses the entry's own per-entry time-to-live rather than the dictionary default.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenSlidingKindAndPerEntryTtl_ShouldSlideByPerEntryTtl()
    {
        var time = new ManualTimeProvider();
        var dictionary = new EvictingDictionary<string, int>(
            4, new EvictingDictionaryExpiration(TimeSpan.FromMinutes(10), EvictingDictionaryExpirationKind.Sliding, time));
        dictionary.Add("A", 1, TimeSpan.FromMinutes(2));

        time.Advance(TimeSpan.FromMinutes(1));
        Assert.IsTrue(dictionary.TryGetValue("A", out _));

        time.Advance(TimeSpan.FromMinutes(1) + TimeSpan.FromSeconds(30));

        Assert.IsTrue(dictionary.TryGetValue("A", out _), "The read at 1 minute should have slid the deadline to 3 minutes.");

        time.Advance(TimeSpan.FromMinutes(2));

        Assert.IsFalse(dictionary.TryGetValue("A", out _));
    }

    // --------------------------------------------------------
    // Per-entry TTL overrides
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that a per-entry time-to-live shorter than the dictionary default expires the entry ahead of
    /// default-lifetime entries.
    /// </summary>
    [TestMethod]
    public void Add_WhenPerEntryTtlShorterThanDefault_ShouldExpireEntryFirst()
    {
        var time = new ManualTimeProvider();
        var dictionary = new EvictingDictionary<string, int>(
            4, new EvictingDictionaryExpiration(TimeSpan.FromMinutes(10), EvictingDictionaryExpirationKind.Absolute, time));
        dictionary.Add("short", 1, TimeSpan.FromMinutes(2));
        dictionary.Add("default", 2);

        time.Advance(TimeSpan.FromMinutes(5));

        Assert.IsFalse(dictionary.TryGetValue("short", out _));
        Assert.IsTrue(dictionary.TryGetValue("default", out _));
    }

    /// <summary>
    /// Verifies that a per-entry time-to-live longer than the dictionary default keeps the entry alive after
    /// default-lifetime entries have expired.
    /// </summary>
    [TestMethod]
    public void Add_WhenPerEntryTtlLongerThanDefault_ShouldOutliveDefaultEntries()
    {
        var time = new ManualTimeProvider();
        var dictionary = new EvictingDictionary<string, int>(
            4, new EvictingDictionaryExpiration(TimeSpan.FromMinutes(10), EvictingDictionaryExpirationKind.Absolute, time));
        dictionary.Add("long", 1, TimeSpan.FromMinutes(30));
        dictionary.Add("default", 2);

        time.Advance(TimeSpan.FromMinutes(15));

        Assert.IsTrue(dictionary.TryGetValue("long", out _));
        Assert.IsFalse(dictionary.TryGetValue("default", out _));

        time.Advance(TimeSpan.FromMinutes(20));

        Assert.IsFalse(dictionary.TryGetValue("long", out _));
    }

    /// <summary>
    /// Verifies that adding with a per-entry time-to-live when no expiration configuration exists throws
    /// <see cref="InvalidOperationException" /> with the resource message.
    /// </summary>
    [TestMethod]
    public void Add_WhenExpirationNotConfiguredAndTtlSupplied_ShouldThrowInvalidOperationException()
    {
        var dictionary = new EvictingDictionary<string, int>(4);

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            dictionary.Add("A", 1, TimeSpan.FromMinutes(1));
        });

        Assert.AreEqual(CollectionsResourceStrings.Op_Invalid_ExpirationNotConfigured, ex.Message);
    }

    /// <summary>
    /// Verifies that attempting <see cref="EvictingDictionary{TKey, TValue}.TryAdd(TKey, TValue, TimeSpan)" /> when no
    /// expiration configuration exists throws <see cref="InvalidOperationException" /> with the resource message.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenExpirationNotConfiguredAndTtlSupplied_ShouldThrowInvalidOperationException()
    {
        var dictionary = new EvictingDictionary<string, int>(4);

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = dictionary.TryAdd("A", 1, TimeSpan.FromMinutes(1));
        });

        Assert.AreEqual(CollectionsResourceStrings.Op_Invalid_ExpirationNotConfigured, ex.Message);
    }

    /// <summary>
    /// Verifies that adding with a zero or negative per-entry time-to-live throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenPerEntryTtlIsZeroOrNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var dictionary = new EvictingDictionary<string, int>(
            4, new EvictingDictionaryExpiration(TimeSpan.FromMinutes(10)));

        var zeroEx = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            dictionary.Add("A", 1, TimeSpan.Zero);
        });

        Assert.AreEqual("timeToLive", zeroEx.ParamName);

        var negativeEx = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = dictionary.TryAdd("A", 1, TimeSpan.FromSeconds(-1));
        });

        Assert.AreEqual("timeToLive", negativeEx.ParamName);
    }

    // --------------------------------------------------------
    // TryAdd semantics
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.TryAdd(TKey, TValue, TimeSpan)" /> adds an absent key
    /// and returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenKeyAbsent_ShouldAddAndReturnTrue()
    {
        var time = new ManualTimeProvider();
        var dictionary = new EvictingDictionary<string, int>(
            4, new EvictingDictionaryExpiration(TimeSpan.FromMinutes(10), EvictingDictionaryExpirationKind.Absolute, time));

        Assert.IsTrue(dictionary.TryAdd("A", 1, TimeSpan.FromMinutes(5)));
        Assert.AreEqual(1, dictionary["A"]);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.TryAdd(TKey, TValue, TimeSpan)" /> returns
    /// <see langword="false" /> and leaves the value unchanged when a live entry already exists.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenLiveEntryExists_ShouldReturnFalseAndPreserveValue()
    {
        var time = new ManualTimeProvider();
        var dictionary = new EvictingDictionary<string, int>(
            4, new EvictingDictionaryExpiration(TimeSpan.FromMinutes(10), EvictingDictionaryExpirationKind.Absolute, time));
        dictionary.Add("A", 1);

        Assert.IsFalse(dictionary.TryAdd("A", 99, TimeSpan.FromMinutes(5)));
        Assert.AreEqual(1, dictionary["A"]);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.TryAdd(TKey, TValue, TimeSpan)" /> treats an
    /// expired-but-unpurged entry as absent, replacing it and returning <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenExistingEntryExpired_ShouldReplaceAndReturnTrue()
    {
        var time = new ManualTimeProvider();
        var dictionary = new EvictingDictionary<string, int>(
            4, new EvictingDictionaryExpiration(TimeSpan.FromMinutes(10), EvictingDictionaryExpirationKind.Absolute, time));
        dictionary.Add("A", 1);

        time.Advance(TimeSpan.FromMinutes(11));

        Assert.IsTrue(dictionary.TryAdd("A", 2, TimeSpan.FromMinutes(5)));
        Assert.AreEqual(2, dictionary["A"]);
    }

    // --------------------------------------------------------
    // RemoveExpired and the Count contract
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Count" /> includes expired-but-unpurged entries and
    /// drops only after <see cref="EvictingDictionary{TKey, TValue}.RemoveExpired" /> reconciles it.
    /// </summary>
    [TestMethod]
    public void Count_WhenEntriesExpireUnpurged_ShouldIncludeThemUntilRemoveExpired()
    {
        var time = new ManualTimeProvider();
        var dictionary = new EvictingDictionary<string, int>(
            4, new EvictingDictionaryExpiration(TimeSpan.FromMinutes(10), EvictingDictionaryExpirationKind.Absolute, time));
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);

        time.Advance(TimeSpan.FromMinutes(11));

        Assert.AreEqual(2, dictionary.Count, "Count reports the raw stored count including expired entries.");

        int seen = 0;
        foreach (KeyValuePair<string, int> kvp in dictionary)
            seen++;

        Assert.AreEqual(0, seen, "Enumeration must not observe expired entries.");
        Assert.AreEqual(2, dictionary.Count, "Enumeration must not purge expired entries.");

        Assert.AreEqual(2, dictionary.RemoveExpired());
        Assert.AreEqual(0, dictionary.Count);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.RemoveExpired" /> removes only expired entries and
    /// returns the number removed.
    /// </summary>
    [TestMethod]
    public void RemoveExpired_WhenSomeEntriesExpired_ShouldRemoveOnlyThoseAndReturnCount()
    {
        var time = new ManualTimeProvider();
        var dictionary = new EvictingDictionary<string, int>(
            4, new EvictingDictionaryExpiration(TimeSpan.FromMinutes(10), EvictingDictionaryExpirationKind.Absolute, time));
        dictionary.Add("A", 1, TimeSpan.FromMinutes(2));
        dictionary.Add("B", 2, TimeSpan.FromMinutes(2));
        dictionary.Add("C", 3, TimeSpan.FromMinutes(30));

        time.Advance(TimeSpan.FromMinutes(5));

        Assert.AreEqual(2, dictionary.RemoveExpired());
        Assert.AreEqual(1, dictionary.Count);
        Assert.IsTrue(dictionary.ContainsKey("C"));
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.RemoveExpired" /> returns zero when nothing has
    /// expired.
    /// </summary>
    [TestMethod]
    public void RemoveExpired_WhenNothingExpired_ShouldReturnZero()
    {
        var time = new ManualTimeProvider();
        var dictionary = new EvictingDictionary<string, int>(
            4, new EvictingDictionaryExpiration(TimeSpan.FromMinutes(10), EvictingDictionaryExpirationKind.Absolute, time));
        dictionary.Add("A", 1);

        Assert.AreEqual(0, dictionary.RemoveExpired());
        Assert.AreEqual(1, dictionary.Count);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.RemoveExpired" /> returns zero when no expiration
    /// configuration exists.
    /// </summary>
    [TestMethod]
    public void RemoveExpired_WhenExpirationNotConfigured_ShouldReturnZero()
    {
        var dictionary = new EvictingDictionary<string, int>(4);
        dictionary.Add("A", 1);

        Assert.AreEqual(0, dictionary.RemoveExpired());
        Assert.AreEqual(1, dictionary.Count);
    }

    // --------------------------------------------------------
    // Invisibility of expired entries before purge
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that an expired-but-unpurged entry is invisible to ContainsKey, TryGetValue, the indexer getter,
    /// value containment, and enumeration.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenEntryExpiredButUnpurged_ShouldBeInvisibleAcrossReadSurfaces()
    {
        var time = new ManualTimeProvider();
        var dictionary = new EvictingDictionary<string, int>(
            4, new EvictingDictionaryExpiration(TimeSpan.FromMinutes(10), EvictingDictionaryExpirationKind.Absolute, time));
        dictionary.Add("A", 42);

        time.Advance(TimeSpan.FromMinutes(11));

        // Non-mutating surfaces first: enumeration and value containment filter without purging.
        int seen = 0;
        foreach (KeyValuePair<string, int> kvp in dictionary)
            seen++;

        Assert.AreEqual(0, seen);
        Assert.IsFalse(dictionary.Values.Contains(42));
        Assert.IsFalse(dictionary.Contains(new KeyValuePair<string, int>("A", 42)));
        Assert.IsFalse(dictionary.Keys.Any(k => k == "A"));

        // Key-directed accesses report miss (and lazily purge).
        Assert.IsFalse(dictionary.ContainsKey("A"));
        Assert.IsFalse(dictionary.TryGetValue("A", out _));
        Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = dictionary["A"];
        });
    }

    /// <summary>
    /// Verifies that a lookup that hits an expired key lazily removes the entry from storage.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyExpired_ShouldLazilyRemoveEntry()
    {
        var time = new ManualTimeProvider();
        var dictionary = new EvictingDictionary<string, int>(
            4, new EvictingDictionaryExpiration(TimeSpan.FromMinutes(10), EvictingDictionaryExpirationKind.Absolute, time));
        dictionary.Add("A", 1);
        dictionary.Add("B", 2, TimeSpan.FromMinutes(30));

        time.Advance(TimeSpan.FromMinutes(11));

        Assert.AreEqual(2, dictionary.Count);
        Assert.IsFalse(dictionary.TryGetValue("A", out _));
        Assert.AreEqual(1, dictionary.Count, "The expired entry must be physically removed by the miss.");
        Assert.IsTrue(dictionary.ContainsKey("B"));
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Touch" /> reports an expired key as absent and
    /// lazily removes it, and that <see cref="EvictingDictionary{TKey, TValue}.TouchOrThrow" /> throws
    /// <see cref="KeyNotFoundException" /> for it.
    /// </summary>
    [TestMethod]
    public void Touch_WhenKeyExpired_ShouldReturnFalseAndRemoveEntry()
    {
        var time = new ManualTimeProvider();
        var dictionary = new EvictingDictionary<string, int>(
            4, new EvictingDictionaryExpiration(TimeSpan.FromMinutes(10), EvictingDictionaryExpirationKind.Absolute, time));
        dictionary.Add("A", 1);

        time.Advance(TimeSpan.FromMinutes(11));

        Assert.IsFalse(dictionary.Touch("A"));
        Assert.AreEqual(0, dictionary.Count);

        dictionary.Add("B", 2);
        time.Advance(TimeSpan.FromMinutes(11));

        Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            dictionary.TouchOrThrow("B");
        });
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.CopyTo" /> purges expired entries before copying, so
    /// only live entries are written and Count is reconciled by the call.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenEntriesExpired_ShouldPurgeAndWriteOnlyLiveEntries()
    {
        var time = new ManualTimeProvider();
        var dictionary = new EvictingDictionary<string, int>(
            4, new EvictingDictionaryExpiration(TimeSpan.FromMinutes(10), EvictingDictionaryExpirationKind.Absolute, time));
        dictionary.Add("A", 1, TimeSpan.FromMinutes(2));
        dictionary.Add("B", 2, TimeSpan.FromMinutes(30));

        time.Advance(TimeSpan.FromMinutes(5));

        var pairs = new KeyValuePair<string, int>[4];
        dictionary.CopyTo(pairs, 0);

        Assert.AreEqual(new KeyValuePair<string, int>("B", 2), pairs[0]);
        Assert.AreEqual(default, pairs[1], "Only the live entry may be written.");
        Assert.AreEqual(1, dictionary.Count, "CopyTo must reconcile Count by purging expired entries.");
    }

    // --------------------------------------------------------
    // Capacity pressure prefers expired victims
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that when capacity forces an eviction, expired entries are purged first so live entries are retained
    /// and the capacity policy is not consulted.
    /// </summary>
    [TestMethod]
    public void Add_WhenAtCapacityWithExpiredEntries_ShouldEvictExpiredBeforePolicyVictims()
    {
        var time = new ManualTimeProvider();
        var dictionary = new EvictingDictionary<string, int>(
            3,
            EvictingDictionaryPolicy.LeastRecentlyUsed,
            new EvictingDictionaryExpiration(null, EvictingDictionaryExpirationKind.Absolute, time));
        dictionary.Add("expiring", 1, TimeSpan.FromMinutes(1));
        dictionary.Add("B", 2);
        dictionary.Add("C", 3);

        time.Advance(TimeSpan.FromMinutes(2));
        dictionary.Add("D", 4);

        Assert.IsFalse(dictionary.ContainsKey("expiring"), "The expired entry must be the preferred victim.");
        Assert.IsTrue(dictionary.ContainsKey("B"), "Live entries must be retained while an expired victim exists.");
        Assert.IsTrue(dictionary.ContainsKey("C"));
        Assert.IsTrue(dictionary.ContainsKey("D"));
        Assert.AreEqual(1, dictionary.EvictionCount);

        // With no expired entries remaining, the policy applies as usual: "B" is now the least recently used.
        dictionary.Add("E", 5);

        Assert.IsFalse(dictionary.ContainsKey("B"));
        Assert.IsTrue(dictionary.ContainsKey("C"));
        Assert.IsTrue(dictionary.ContainsKey("D"));
        Assert.IsTrue(dictionary.ContainsKey("E"));
    }

    /// <summary>
    /// Verifies that expiry behaves identically under every capacity policy: expired entries become invisible, are
    /// purged by RemoveExpired, and live entries survive capacity pressure while expired entries are preferred victims.
    /// </summary>
    [TestMethod]
    [DataRow(EvictingDictionaryPolicy.FirstInFirstOut)]
    [DataRow(EvictingDictionaryPolicy.LeastRecentlyUsed)]
    [DataRow(EvictingDictionaryPolicy.LeastFrequentlyUsed)]
    [DataRow(EvictingDictionaryPolicy.MostRecentlyUsed)]
    [DataRow(EvictingDictionaryPolicy.RandomReplacement)]
    [DataRow(EvictingDictionaryPolicy.SecondChance)]
    public void RemoveExpired_ForEveryPolicy_ShouldBehaveIdentically(EvictingDictionaryPolicy policy)
    {
        var time = new ManualTimeProvider();
        var dictionary = new EvictingDictionary<int, int>(
            4, policy, new EvictingDictionaryExpiration(TimeSpan.FromMinutes(5), EvictingDictionaryExpirationKind.Absolute, time));

        for (int i = 0; i < 4; i++)
            dictionary.Add(i, i);

        time.Advance(TimeSpan.FromMinutes(6));

        int seen = 0;
        foreach (KeyValuePair<int, int> kvp in dictionary)
            seen++;

        Assert.AreEqual(0, seen, $"Expired entries must be invisible to enumeration under {policy}.");
        Assert.AreEqual(4, dictionary.Count, $"Count must include unpurged expired entries under {policy}.");
        Assert.AreEqual(4, dictionary.RemoveExpired(), $"RemoveExpired must purge all expired entries under {policy}.");
        Assert.AreEqual(0, dictionary.Count);

        // The dictionary remains fully usable after the purge.
        dictionary.Add(100, 100);

        Assert.IsTrue(dictionary.TryGetValue(100, out int value));
        Assert.AreEqual(100, value);
    }

    /// <summary>
    /// Verifies that capacity pressure prefers expired victims over policy victims under every capacity policy.
    /// </summary>
    [TestMethod]
    [DataRow(EvictingDictionaryPolicy.FirstInFirstOut)]
    [DataRow(EvictingDictionaryPolicy.LeastRecentlyUsed)]
    [DataRow(EvictingDictionaryPolicy.LeastFrequentlyUsed)]
    [DataRow(EvictingDictionaryPolicy.MostRecentlyUsed)]
    [DataRow(EvictingDictionaryPolicy.RandomReplacement)]
    [DataRow(EvictingDictionaryPolicy.SecondChance)]
    public void Add_WhenAtCapacityWithExpiredEntry_ForEveryPolicy_ShouldRetainLiveEntries(EvictingDictionaryPolicy policy)
    {
        var time = new ManualTimeProvider();
        var dictionary = new EvictingDictionary<string, int>(
            3, policy, new EvictingDictionaryExpiration(null, EvictingDictionaryExpirationKind.Absolute, time));
        dictionary.Add("expiring", 1, TimeSpan.FromMinutes(1));
        dictionary.Add("live1", 2);
        dictionary.Add("live2", 3);

        time.Advance(TimeSpan.FromMinutes(2));
        dictionary.Add("new", 4);

        Assert.IsFalse(dictionary.ContainsKey("expiring"), $"The expired entry must be the eviction victim under {policy}.");
        Assert.IsTrue(dictionary.ContainsKey("live1"), $"Live entries must survive under {policy}.");
        Assert.IsTrue(dictionary.ContainsKey("live2"), $"Live entries must survive under {policy}.");
        Assert.IsTrue(dictionary.ContainsKey("new"));
    }

    // --------------------------------------------------------
    // Eviction events on expiry removal
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.RemoveExpired" /> raises ItemEvicting and ItemEvicted
    /// for each purged entry and increments EvictionCount, matching the capacity-eviction path.
    /// </summary>
    [TestMethod]
    public void RemoveExpired_WhenEntriesPurged_ShouldRaiseEvictionEventsAndIncrementEvictionCount()
    {
        var time = new ManualTimeProvider();
        var dictionary = new EvictingDictionary<string, int>(
            4, new EvictingDictionaryExpiration(TimeSpan.FromMinutes(5), EvictingDictionaryExpirationKind.Absolute, time));

        var evicting = new List<(string Key, int Value)>();
        var evicted = new List<(string Key, int Value)>();
        dictionary.ItemEvicting += (key, value) => evicting.Add((key, value));
        dictionary.ItemEvicted += (key, value) => evicted.Add((key, value));

        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        time.Advance(TimeSpan.FromMinutes(6));

        Assert.AreEqual(2, dictionary.RemoveExpired());

        Assert.HasCount(2, evicting);
        Assert.HasCount(2, evicted);
        Assert.Contains(("A", 1), evicted);
        Assert.Contains(("B", 2), evicted);
        Assert.AreEqual(2, dictionary.EvictionCount);
    }

    /// <summary>
    /// Verifies that the lazy removal performed when a lookup hits an expired key raises the eviction events and
    /// increments EvictionCount.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyExpired_ShouldRaiseEvictionEvents()
    {
        var time = new ManualTimeProvider();
        var dictionary = new EvictingDictionary<string, int>(
            4, new EvictingDictionaryExpiration(TimeSpan.FromMinutes(5), EvictingDictionaryExpirationKind.Absolute, time));

        var evicting = new List<(string Key, int Value)>();
        var evicted = new List<(string Key, int Value)>();
        dictionary.ItemEvicting += (key, value) => evicting.Add((key, value));
        dictionary.ItemEvicted += (key, value) => evicted.Add((key, value));

        dictionary.Add("A", 1);
        time.Advance(TimeSpan.FromMinutes(6));

        Assert.IsFalse(dictionary.TryGetValue("A", out _));

        Assert.HasCount(1, evicting);
        Assert.AreEqual(("A", 1), evicting[0]);
        Assert.HasCount(1, evicted);
        Assert.AreEqual(("A", 1), evicted[0]);
        Assert.AreEqual(1, dictionary.EvictionCount);
    }

    // --------------------------------------------------------
    // No-expiration regression pins
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that without an expiration configuration the read and write surfaces behave exactly as a
    /// capacity-only cache: entries never expire and counters track only policy activity.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenExpirationNotConfigured_ShouldBehaveAsCapacityOnlyCache()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.LeastRecentlyUsed);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);

        Assert.IsNull(dictionary.Expiration);
        Assert.IsTrue(dictionary.TryGetValue("A", out int value));
        Assert.AreEqual(1, value);
        Assert.AreEqual(1, dictionary.TotalTouches);
        Assert.AreEqual(2, dictionary.Count);

        dictionary.Add("C", 3);

        Assert.IsFalse(dictionary.ContainsKey("B"), "LRU eviction must be unaffected by the expiration feature.");
        Assert.AreEqual(1, dictionary.EvictionCount);
    }

    /// <summary>
    /// Verifies that without an expiration configuration enumeration observes every stored entry (no expiry
    /// filtering) and Count matches the enumerated count.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenExpirationNotConfigured_ShouldObserveAllEntries()
    {
        var dictionary = new EvictingDictionary<int, int>(8, EvictingDictionaryPolicy.FirstInFirstOut);
        for (int i = 0; i < 5; i++)
            dictionary.Add(i, i);

        int seen = 0;
        foreach (KeyValuePair<int, int> kvp in dictionary)
            seen++;

        Assert.AreEqual(5, seen);
        Assert.AreEqual(dictionary.Count, seen);
    }

    /// <summary>
    /// Provides a deterministic, manually advanced <see cref="TimeProvider" /> whose wall clock only progresses when
    /// <see cref="Advance" /> is called.
    /// </summary>
    private sealed class ManualTimeProvider
        : TimeProvider
    {
        /// <summary>The current simulated UTC time.</summary>
        private DateTimeOffset _utcNow = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow() => _utcNow;

        /// <summary>
        /// Advances the simulated clock by the specified amount.
        /// </summary>
        /// <param name="delta">The amount of simulated time to advance.</param>
        public void Advance(TimeSpan delta) => _utcNow += delta;
    }
}

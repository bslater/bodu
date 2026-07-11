// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

[TestClass]
public partial class ConcurrentEvictingDictionaryTests
{
    public TestContext TestContext { get; set; }

    /// <summary>
    /// Creates a dictionary with a single segment so the eviction sequence is exact and matches the non-concurrent
    /// <see cref="EvictingDictionary{TKey, TValue}" />.
    /// </summary>
    /// <param name="capacity">The total capacity.</param>
    /// <param name="policy">The eviction policy.</param>
    /// <param name="expiration">The optional expiration configuration.</param>
    /// <returns>A dictionary whose capacity is owned by one segment.</returns>
    private static ConcurrentEvictingDictionary<string, int> CreateSingleSegment(
        int capacity,
        EvictingDictionaryPolicy policy = EvictingDictionaryPolicy.LeastRecentlyUsed,
        EvictingDictionaryExpiration? expiration = null) =>
        new(concurrencyLevel: 1, capacity, policy, null, expiration);

    /// <summary>
    /// Asserts that <paramref name="dictionary" /> contains exactly the entries in <paramref name="expected" />,
    /// ignoring order.
    /// </summary>
    /// <param name="dictionary">The dictionary under test.</param>
    /// <param name="expected">The expected key/value pairs.</param>
    private static void AssertContainsExactly(
        ConcurrentEvictingDictionary<string, int> dictionary,
        params (string Key, int Value)[] expected)
    {
        KeyValuePair<string, int>[] actual = dictionary.ToArray();

        Assert.HasCount(expected.Length, actual, "Unexpected entry count in the snapshot.");

        foreach ((string key, int value) in expected)
        {
            Assert.IsTrue(dictionary.TryGetValue(key, out int actualValue), $"Dictionary was missing an expected key: {key}.");
            Assert.AreEqual(value, actualValue, $"Dictionary held an unexpected value for key: {key}.");
        }
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

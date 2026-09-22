// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentLruCacheTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

[TestClass]
public partial class ConcurrentLruCacheTests
{
    public TestContext TestContext { get; set; }

    /// <summary>
    /// Asserts that <paramref name="cache" /> contains exactly the entries in <paramref name="expected" />, ignoring
    /// order, using pure containment probes so the assertion does not disturb recency state.
    /// </summary>
    /// <param name="cache">The cache under test.</param>
    /// <param name="expected">The expected key/value pairs.</param>
    private static void AssertContainsExactly(
        ConcurrentLruCache<string, int> cache,
        params (string Key, int Value)[] expected)
    {
        KeyValuePair<string, int>[] actual = cache.ToArray();

        Assert.HasCount(expected.Length, actual, "Unexpected entry count in the snapshot.");

        var actualMap = actual.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
        foreach ((string key, int value) in expected)
        {
            Assert.IsTrue(actualMap.TryGetValue(key, out int actualValue), $"Cache was missing an expected key: {key}.");
            Assert.AreEqual(value, actualValue, $"Cache held an unexpected value for key: {key}.");
        }
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionaryTests.HashCollisions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class SequencedDictionaryTests
{
    /// <summary>
    /// An equality comparer that forces every key into the same hash bucket while preserving real equality, used to
    /// exercise hash-collision handling.
    /// </summary>
    private sealed class ConstantHashComparer : IEqualityComparer<string>
    {
        /// <inheritdoc />
        public bool Equals(string? x, string? y) => string.Equals(x, y, StringComparison.Ordinal);

        /// <inheritdoc />
        public int GetHashCode(string obj) => 0;
    }

    /// <summary>
    /// Verifies that entries whose keys all hash to the same bucket are stored, found, and enumerated in insertion order.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void HashCollisions_WhenAllKeysCollide_ShouldPreserveOrderAndResolveLookups()
    {
        var dictionary = new SequencedDictionary<string, int>(new ConstantHashComparer());
        string[] keys = Enumerable.Range(0, 64).Select(i => $"key-{i}").ToArray();

        for (int i = 0; i < keys.Length; i++)
            dictionary.Add(keys[i], i);

        CollectionAssert.AreEqual(keys, dictionary.Keys.ToArray());

        for (int i = 0; i < keys.Length; i++)
        {
            Assert.IsTrue(dictionary.TryGetValue(keys[i], out int value));
            Assert.AreEqual(i, value);
        }
    }

    /// <summary>
    /// Verifies that removing colliding keys leaves the remaining colliding entries in insertion order.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void HashCollisions_WhenCollidingKeysRemoved_ShouldPreserveOrderOfRemainder()
    {
        var dictionary = new SequencedDictionary<string, int>(new ConstantHashComparer());
        string[] keys = Enumerable.Range(0, 32).Select(i => $"key-{i}").ToArray();

        foreach (string key in keys)
            dictionary.Add(key, 0);

        // Remove every key whose index is even.
        for (int i = 0; i < keys.Length; i += 2)
            Assert.IsTrue(dictionary.Remove(keys[i]));

        string[] expected = keys.Where((_, i) => i % 2 == 1).ToArray();
        CollectionAssert.AreEqual(expected, dictionary.Keys.ToArray());
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LayeredDictionaryTests.Keys.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class LayeredDictionaryTests
{
    /// <summary>
    /// Verifies that the key collection contains the distinct keys across all layers, listing a shadowed key once.
    /// </summary>
    [TestMethod]
    public void Keys_WhenKeysShadowed_ShouldContainDistinctKeys()
    {
        var view = CreatePopulated(out _, out _);

        CollectionAssert.AreEquivalent(new[] { "a", "b", "c" }, view.Keys.ToList());
    }

    /// <summary>
    /// Verifies that the key collection is a snapshot: later mutations of the layers do not change a previously
    /// returned collection.
    /// </summary>
    [TestMethod]
    public void Keys_WhenLayerMutatedAfterCall_ShouldNotChangeSnapshot()
    {
        var view = CreatePopulated(out Dictionary<string, int> first, out _);

        ICollection<string> keys = view.Keys;
        first["d"] = 4;

        Assert.AreEqual(3, keys.Count);
        Assert.IsFalse(keys.Contains("d"));
    }

    /// <summary>
    /// Verifies that the returned key collection is read-only and is not write-through: mutating members throw
    /// <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void Keys_WhenMutated_ShouldThrowNotSupportedException()
    {
        var view = CreatePopulated(out _, out _);

        ICollection<string> keys = view.Keys;

        Assert.IsTrue(keys.IsReadOnly);
        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            keys.Add("z");
        });
    }
}

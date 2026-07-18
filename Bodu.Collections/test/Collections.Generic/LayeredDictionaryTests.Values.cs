// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LayeredDictionaryTests.Values.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class LayeredDictionaryTests
{
    /// <summary>
    /// Verifies that the value collection contains the first-wins values — the shadowed deeper value is not included.
    /// </summary>
    [TestMethod]
    public void Values_WhenKeysShadowed_ShouldContainFirstWinsValues()
    {
        var view = CreatePopulated(out _, out _);

        CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, view.Values.ToList());
    }

    /// <summary>
    /// Verifies that the value collection is a snapshot: later mutations of the layers do not change a previously
    /// returned collection.
    /// </summary>
    [TestMethod]
    public void Values_WhenLayerMutatedAfterCall_ShouldNotChangeSnapshot()
    {
        var view = CreatePopulated(out Dictionary<string, int> first, out _);

        ICollection<int> values = view.Values;
        first["a"] = 10;

        CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, values.ToList());
    }

    /// <summary>
    /// Verifies that the returned value collection is read-only and is not write-through: mutating members throw
    /// <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void Values_WhenMutated_ShouldThrowNotSupportedException()
    {
        var view = CreatePopulated(out _, out _);

        ICollection<int> values = view.Values;

        Assert.IsTrue(values.IsReadOnly);
        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            values.Add(99);
        });
    }
}

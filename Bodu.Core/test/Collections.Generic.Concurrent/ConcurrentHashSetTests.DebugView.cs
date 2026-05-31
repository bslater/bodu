// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.DebugView.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSetDebugView{T}.Items" /> exposes every element of the set.
    /// </summary>
    [TestMethod]
    public void DebugView_Items_ShouldExposeEveryElement()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3]);

        var view = new ConcurrentHashSetDebugView<int>(set);

        CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, view.Items);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSetDebugView{T}.Items" /> is empty for an empty set.
    /// </summary>
    [TestMethod]
    public void DebugView_Items_WhenSetIsEmpty_ShouldBeEmpty()
    {
        var view = new ConcurrentHashSetDebugView<int>(new ConcurrentHashSet<int>());

        Assert.HasCount(0, view.Items);
    }

    /// <summary>
    /// Verifies that the debug view constructor throws <see cref="ArgumentNullException" /> for a
    /// <see langword="null" /> set.
    /// </summary>
    [TestMethod]
    public void DebugView_Ctor_WhenSetIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new ConcurrentHashSetDebugView<int>(null!);
        });

        Assert.AreEqual("set", ex.ParamName);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayDequeDebugViewTests.Ctor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class ArrayDequeDebugViewTests
{
    /// <summary>
    /// Verifies that constructing a DebugView with a valid <see cref="ArrayDeque{T}"/> instance succeeds.
    /// </summary>
    [TestMethod]
    public void Ctor_WithValidDeque_ShouldInitialize()
    {
        var deque = new ArrayDeque<int>(3);
        var view = new ArrayDequeDebugView<int>(deque);

        Assert.IsNotNull(view);
    }

    /// <summary>
    /// Verifies that constructing a DebugView with a <see langword="null"/> deque throws <see cref="ArgumentNullException"/>.
    /// </summary>
    [TestMethod]
    public void Ctor_WithNullDeque_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new ArrayDequeDebugView<int>(null!);
        });
    }
}

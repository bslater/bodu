// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeDebugViewTests.Ctor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DequeDebugViewTests
{

    /// <summary>
    /// Verifies that constructing a DebugView with a <see langword="null"/> deque throws <see cref="ArgumentNullException"/>.
    /// </summary>
    [TestMethod]
    public void Ctor_WithNullDeque_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DequeDebugView<int>(null!);
        });
    }
    /// <summary>
    /// Verifies that constructing a DebugView with a valid <see cref="Deque{T}"/> instance succeeds.
    /// </summary>
    [TestMethod]
    public void Ctor_WithValidDeque_ShouldInitialize()
    {
        var deque = new Deque<int>(3);
        var view = new DequeDebugView<int>(deque);

        Assert.IsNotNull(view);
    }

}

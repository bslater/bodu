// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MovingMinMaxTests.Ctors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class MovingMinMaxTests
{
    /// <summary>
    /// Verifies that a non-positive capacity throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void Ctor_WhenCapacityIsNotPositive_ShouldThrowArgumentOutOfRangeException(int capacity)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new MovingMinMax<int>(capacity);
        });

        Assert.AreEqual("capacity", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a new window is empty and reports its capacity.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCapacityIsValid_ShouldStartEmpty()
    {
        var window = new MovingMinMax<int>(4);

        Assert.AreEqual(4, window.Capacity);
        Assert.AreEqual(0, window.Count);
        Assert.IsTrue(window.IsEmpty);
        Assert.IsFalse(window.IsFull);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeTests.AllowGrow.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DequeTests
{

    /// <summary>
    /// Verifies that <see cref="Deque{T}.AllowGrow"/> defaults to <see langword="true"/> for the
    /// parameterless and capacity-only constructors.
    /// </summary>
    [TestMethod]
    public void AllowGrow_WhenDefaultUsed_ShouldBeTrue()
    {
        Assert.IsTrue(new Deque<int>().AllowGrow);
        Assert.IsTrue(new Deque<int>(8).AllowGrow);
        Assert.IsTrue(new Deque<int>([1, 2, 3]).AllowGrow);
        Assert.IsTrue(new Deque<int>([1, 2, 3], capacity: 8).AllowGrow);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.AllowGrow"/> reflects the explicit value passed to the constructor.
    /// </summary>
    /// <param name="allowGrow">The value to pass to the constructor.</param>
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AllowGrow_WhenExplicitValueProvided_ShouldReflectInProperty(bool allowGrow)
    {
        var deque = new Deque<int>(8, allowGrow: allowGrow);
        Assert.AreEqual(allowGrow, deque.AllowGrow);
    }

    /// <summary>
    /// Verifies that switching <see cref="Deque{T}.AllowGrow"/> off does not shrink the existing capacity —
    /// any room already allocated remains available.
    /// </summary>
    [TestMethod]
    public void AllowGrow_WhenToggledOff_ShouldNotShrinkCapacity()
    {
        var deque = new Deque<int>(4, allowGrow: true);
        for (var i = 0; i < 100; i++)
            deque.AddLast(i);

        var capacityBefore = deque.Capacity;
        deque.AllowGrow = false;

        Assert.AreEqual(capacityBefore, deque.Capacity);
        Assert.AreEqual(100, deque.Count);
    }

    /// <summary>
    /// Verifies that toggling <see cref="Deque{T}.AllowGrow"/> from <see langword="true"/> to
    /// <see langword="false"/> at runtime makes a previously-growable deque reject further inserts once full.
    /// </summary>
    [TestMethod]
    public void AllowGrow_WhenToggledOffAtCapacity_ShouldStartRejectingAdds()
    {
        var deque = new Deque<int>(2, allowGrow: true);
        deque.AddLast(1);
        deque.AddLast(2);

        deque.AllowGrow = false;

        Assert.ThrowsExactly<InvalidOperationException>(() => deque.AddLast(3));
        Assert.IsFalse(deque.TryAddLast(3));
        Assert.AreEqual(2, deque.Count);
    }

    /// <summary>
    /// Verifies that toggling <see cref="Deque{T}.AllowGrow"/> from <see langword="false"/> to
    /// <see langword="true"/> at runtime allows a previously-fixed deque to grow on the next add.
    /// </summary>
    [TestMethod]
    public void AllowGrow_WhenToggledOnAtCapacity_ShouldStartGrowing()
    {
        var deque = new Deque<int>(2, allowGrow: false);
        deque.AddLast(1);
        deque.AddLast(2);

        deque.AllowGrow = true;
        deque.AddLast(3);

        Assert.AreEqual(3, deque.Count);
        Assert.IsGreaterThanOrEqualTo(3, deque.Capacity);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.EnsureCapacity(int)"/> works regardless of <see cref="Deque{T}.AllowGrow"/>.
    /// It is the explicit pre-grow hatch even on fixed-capacity deques.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenAllowGrowFalse_ShouldStillExpand()
    {
        var deque = new Deque<int>(4, allowGrow: false);
        var newCap = deque.EnsureCapacity(50);

        Assert.IsGreaterThanOrEqualTo(50, newCap);
        Assert.IsGreaterThanOrEqualTo(50, deque.Capacity);
        Assert.IsFalse(deque.AllowGrow);
    }

}

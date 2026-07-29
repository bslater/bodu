// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiscreteIntervalTests.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class DiscreteIntervalTests
{
    /// <summary>
    /// Verifies that <see cref="DiscreteInterval{T}.First" /> and <see cref="DiscreteInterval{T}.Last" /> report the
    /// canonical inclusive bounds of a bounded interval.
    /// </summary>
    [TestMethod]
    public void FirstAndLast_WhenBounded_ShouldReturnCanonicalInclusiveBounds()
    {
        DiscreteInterval<int> value = DiscreteInterval<int>.Open(0, 6);

        Assert.AreEqual(1, value.First);
        Assert.AreEqual(5, value.Last);
        Assert.IsTrue(value.IsBounded);
    }

    /// <summary>
    /// Verifies that the boundedness flags reflect the finite or unbounded shape of each side.
    /// </summary>
    [TestMethod]
    public void BoundednessFlags_WhenInspected_ShouldReflectEachSide()
    {
        Assert.IsTrue(DiscreteInterval<int>.Closed(1, 10).IsBounded);
        Assert.IsFalse(DiscreteInterval<int>.Closed(1, 10).LowerUnbounded);
        Assert.IsFalse(DiscreteInterval<int>.Closed(1, 10).UpperUnbounded);

        Assert.IsTrue(DiscreteInterval<int>.AtLeast(5).UpperUnbounded);
        Assert.IsFalse(DiscreteInterval<int>.AtLeast(5).LowerUnbounded);
        Assert.IsFalse(DiscreteInterval<int>.AtLeast(5).IsBounded);

        Assert.IsTrue(DiscreteInterval<int>.AtMost(5).LowerUnbounded);
        Assert.IsFalse(DiscreteInterval<int>.AtMost(5).IsBounded);
    }

    /// <summary>
    /// Verifies that <see cref="DiscreteInterval{T}.IsEmpty" /> is <see langword="true" /> only for the empty interval.
    /// </summary>
    [TestMethod]
    public void IsEmpty_WhenInspected_ShouldReflectMembership()
    {
        Assert.IsTrue(DiscreteInterval<int>.Empty.IsEmpty);
        Assert.IsTrue(DiscreteInterval<int>.Closed(5, 1).IsEmpty);
        Assert.IsFalse(DiscreteInterval<int>.Singleton(0).IsEmpty);
        Assert.IsFalse(DiscreteInterval<int>.All.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="DiscreteInterval{T}.Count" /> returns the inclusive integer count for a bounded
    /// interval and zero for the empty interval.
    /// </summary>
    [TestMethod]
    public void Count_WhenBoundedOrEmpty_ShouldReturnIntegerCount()
    {
        Assert.AreEqual(10, DiscreteInterval<int>.Closed(1, 10).Count);
        Assert.AreEqual(1, DiscreteInterval<int>.Singleton(7).Count);
        Assert.AreEqual(0, DiscreteInterval<int>.Empty.Count);
    }

    /// <summary>
    /// Verifies that reading <see cref="DiscreteInterval{T}.Count" /> on an unbounded interval throws
    /// <see cref="InvalidOperationException" /> because the count is infinite.
    /// </summary>
    [TestMethod]
    public void Count_WhenUnbounded_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = DiscreteInterval<int>.AtLeast(5).Count;
        });
    }

    /// <summary>
    /// Verifies that reading <see cref="DiscreteInterval{T}.Count" /> throws <see cref="OverflowException" /> when the
    /// integer count does not fit in <typeparamref name="T" /> — the full-domain interval has 2^N members, one more
    /// than the type can represent — instead of silently wrapping to zero or a negative value.
    /// </summary>
    [TestMethod]
    public void Count_WhenCountExceedsTypeRange_ShouldThrowOverflowException()
    {
        Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = DiscreteInterval<byte>.Closed(byte.MinValue, byte.MaxValue).Count;
        });

        Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = DiscreteInterval<int>.Closed(int.MinValue, int.MaxValue).Count;
        });

        Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = DiscreteInterval<int>.Closed(0, int.MaxValue).Count;
        });
    }

    /// <summary>
    /// Verifies that <see cref="DiscreteInterval{T}.Count" /> still returns the exact count for the widest
    /// representable window — one short of the full domain — and for arbitrary-precision integers, where the count can
    /// never overflow.
    /// </summary>
    [TestMethod]
    public void Count_WhenWidestRepresentableWindow_ShouldReturnExactCount()
    {
        Assert.AreEqual(byte.MaxValue, DiscreteInterval<byte>.Closed(0, (byte)(byte.MaxValue - 1)).Count);
        Assert.AreEqual(int.MaxValue, DiscreteInterval<int>.Closed(int.MinValue, -2).Count);

        var big = DiscreteInterval<System.Numerics.BigInteger>.Closed(System.Numerics.BigInteger.Zero, System.Numerics.BigInteger.Pow(10, 40));
        Assert.AreEqual(System.Numerics.BigInteger.Pow(10, 40) + System.Numerics.BigInteger.One, big.Count);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiscreteIntervalTests.Factories.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class DiscreteIntervalTests
{
    /// <summary>
    /// Verifies that every endpoint shape canonicalizes to the same inclusive integer set.
    /// </summary>
    [TestMethod]
    public void Factories_WhenGivenEquivalentShapes_ShouldCanonicalizeToSameSet()
    {
        DiscreteInterval<int> expected = DiscreteInterval<int>.Closed(2, 4);

        Assert.AreEqual(expected, DiscreteInterval<int>.Open(1, 5));
        Assert.AreEqual(expected, DiscreteInterval<int>.ClosedOpen(2, 5));
        Assert.AreEqual(expected, DiscreteInterval<int>.OpenClosed(1, 4));
        Assert.AreEqual(expected, DiscreteInterval<int>.Closed(2, 4));
    }

    /// <summary>
    /// Verifies that inverted or degenerate open bounds collapse to the empty interval.
    /// </summary>
    [TestMethod]
    public void Factories_WhenNoIntegerAdmitted_ShouldReturnEmpty()
    {
        Assert.IsTrue(DiscreteInterval<int>.Closed(5, 1).IsEmpty);
        Assert.IsTrue(DiscreteInterval<int>.Open(3, 3).IsEmpty);
        Assert.IsTrue(DiscreteInterval<int>.Open(3, 4).IsEmpty);
    }

    /// <summary>
    /// Verifies that open-bound factories collapse to the empty interval when the excluded bound sits at the domain
    /// extreme, instead of letting the successor/predecessor computation wrap around the integer domain and produce a
    /// large incorrect interval.
    /// </summary>
    [TestMethod]
    public void Factories_WhenOpenBoundAtDomainExtreme_ShouldReturnEmpty()
    {
        Assert.IsTrue(DiscreteInterval<byte>.Open(byte.MaxValue, 10).IsEmpty);
        Assert.IsTrue(DiscreteInterval<byte>.Open(5, byte.MinValue).IsEmpty);
        Assert.IsTrue(DiscreteInterval<int>.Open(int.MaxValue, 0).IsEmpty);
        Assert.IsTrue(DiscreteInterval<int>.OpenClosed(int.MaxValue, 10).IsEmpty);
        Assert.IsTrue(DiscreteInterval<int>.ClosedOpen(5, int.MinValue).IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="DiscreteInterval{T}.GreaterThan(T)" /> of the domain maximum and
    /// <see cref="DiscreteInterval{T}.LessThan(T)" /> of the domain minimum are empty — no integer lies beyond the
    /// domain extremes — rather than wrapping to an interval that contains almost every integer.
    /// </summary>
    [TestMethod]
    public void GreaterThanAndLessThan_WhenBoundAtDomainExtreme_ShouldReturnEmpty()
    {
        Assert.IsTrue(DiscreteInterval<int>.GreaterThan(int.MaxValue).IsEmpty);
        Assert.IsTrue(DiscreteInterval<int>.LessThan(int.MinValue).IsEmpty);
        Assert.IsTrue(DiscreteInterval<byte>.GreaterThan(byte.MaxValue).IsEmpty);
        Assert.IsTrue(DiscreteInterval<byte>.LessThan(byte.MinValue).IsEmpty);
    }

    /// <summary>
    /// Verifies that the non-generic helper infers the endpoint type from its arguments.
    /// </summary>
    [TestMethod]
    public void NonGenericHelper_WhenGivenLongLiterals_ShouldInferType()
    {
        DiscreteInterval<long> value = DiscreteInterval.Closed(1L, 5L);

        Assert.AreEqual(DiscreteInterval<long>.Closed(1L, 5L), value);
    }

    /// <summary>
    /// Verifies that the unbounded factories report the expected boundedness and never read as empty.
    /// </summary>
    [TestMethod]
    public void UnboundedFactories_WhenConstructed_ShouldBeNonEmptyAndUnbounded()
    {
        Assert.IsFalse(DiscreteInterval<int>.All.IsBounded);
        Assert.IsFalse(DiscreteInterval<int>.All.IsEmpty);
        Assert.IsFalse(DiscreteInterval<int>.AtLeast(0).IsEmpty);
        Assert.IsTrue(DiscreteInterval<int>.AtLeast(0).UpperUnbounded);
        Assert.IsTrue(DiscreteInterval<int>.AtMost(0).LowerUnbounded);
    }

    /// <summary>
    /// Verifies that the strict unbounded factories snap the exclusive bound to the next integer, matching the
    /// inclusive form one integer inward.
    /// </summary>
    [TestMethod]
    public void GreaterThanAndLessThan_WhenConstructed_ShouldSnapToNextInteger()
    {
        Assert.AreEqual(DiscreteInterval<int>.AtLeast(2), DiscreteInterval<int>.GreaterThan(1));
        Assert.AreEqual(2, DiscreteInterval<int>.GreaterThan(1).First);
        Assert.IsFalse(DiscreteInterval<int>.GreaterThan(1).Contains(1));

        Assert.AreEqual(DiscreteInterval<int>.AtMost(4), DiscreteInterval<int>.LessThan(5));
        Assert.AreEqual(4, DiscreteInterval<int>.LessThan(5).Last);
        Assert.IsFalse(DiscreteInterval<int>.LessThan(5).Contains(5));
    }
}

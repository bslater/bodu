// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Cache.Enumerator.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using Bodu.Infrastructure;

namespace Bodu.Collections.Generic.Extensions;

public sealed partial class IEnumerableExtensionsTests_Cache
{
    /// <summary>
    /// Verifies that <c>Current</c> returns the most recently advanced element while enumeration is in progress.
    /// </summary>
    [TestMethod]
    public void Enumerator_Current_WhenEnumerating_ShouldReturnLatestElement()
    {
        IEnumerable<int> actual = Yielding().Cache();
        using IEnumerator<int> enumerator = actual.GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(1, enumerator.Current);

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(2, enumerator.Current);

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(3, enumerator.Current);

        static IEnumerable<int> Yielding()
        {
            yield return 1;
            yield return 2;
            yield return 3;
        }
    }

    /// <summary>
    /// Verifies that two independent enumerators share the cached buffer once it has been populated, and observe the same values.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenObtainedTwice_ShouldYieldIdenticalValuesFromCache()
    {
        var tracker = new TrackingEnumerable<int>(YieldOneTwoThree());
        IEnumerable<int> cached = tracker.Cache();

        var first = cached.ToList();
        var second = cached.ToList();

        CollectionAssert.AreEqual(first, second);
        Assert.AreEqual(3, tracker.ItemsEnumerated);

        static IEnumerable<int> YieldOneTwoThree()
        {
            yield return 1;
            yield return 2;
            yield return 3;
        }
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="IEnumerable.GetEnumerator" /> on a cached sequence returns an enumerator that walks all
    /// emitted values.
    /// </summary>
    [TestMethod]
    public void Enumerator_NonGenericGetEnumerator_ShouldEnumerateAllValues()
    {
        IEnumerable cached = Yielding().Cache();

        var values = new List<int>();
        foreach (var item in cached)
            values.Add((int)item);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, values);

        static IEnumerable<int> Yielding()
        {
            yield return 1;
            yield return 2;
            yield return 3;
        }
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="IEnumerator.Current" /> property returns the same value as the generic
    /// <c>Current</c> after a successful <c>MoveNext</c>.
    /// </summary>
    [TestMethod]
    public void Enumerator_NonGenericCurrent_AfterMoveNext_ShouldReturnLatestElement()
    {
        IEnumerable<int> actual = Yielding().Cache();
        using IEnumerator<int> typed = actual.GetEnumerator();
        IEnumerator legacy = typed;

        Assert.IsTrue(typed.MoveNext());
        Assert.AreEqual(42, legacy.Current);

        static IEnumerable<int> Yielding()
        {
            yield return 42;
        }
    }

    /// <summary>
    /// Verifies that calling <c>Dispose</c> on the enumerator does not throw and does not invalidate subsequent enumerator instances
    /// obtained from the same cached source.
    /// </summary>
    [TestMethod]
    public void Enumerator_Dispose_ShouldBeIdempotentAndNotAffectOtherEnumerators()
    {
        IEnumerable<int> actual = Yielding().Cache();
        IEnumerator<int> first = actual.GetEnumerator();

        Assert.IsTrue(first.MoveNext());
        first.Dispose();
        first.Dispose(); // idempotent

        IEnumerator<int> second = actual.GetEnumerator();
        Assert.IsTrue(second.MoveNext());
        Assert.AreEqual(1, second.Current);

        static IEnumerable<int> Yielding()
        {
            yield return 1;
            yield return 2;
        }
    }

    /// <summary>
    /// Verifies that <c>MoveNext</c> returns <see langword="false" /> once the cached sequence has been fully consumed.
    /// </summary>
    [TestMethod]
    public void Enumerator_MoveNext_WhenSequenceExhausted_ShouldReturnFalse()
    {
        IEnumerable<int> actual = new[] { 1, 2 }.Cache();
        using IEnumerator<int> enumerator = actual.GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsFalse(enumerator.MoveNext());
        Assert.IsFalse(enumerator.MoveNext()); // should remain false after exhaustion
    }

    /// <summary>
    /// Verifies that an enumerator obtained from a cached sequence after the underlying source produced no elements yields no items
    /// and immediately reports end-of-sequence.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenSourceIsEmpty_ShouldYieldNothing()
    {
        IEnumerable<int> actual = EmptySource().Cache();
        using IEnumerator<int> enumerator = actual.GetEnumerator();

        Assert.IsFalse(enumerator.MoveNext());

        static IEnumerable<int> EmptySource()
        {
            yield break;
        }
    }
}

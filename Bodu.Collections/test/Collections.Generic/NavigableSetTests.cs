// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableSetTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

/// <summary>
/// Unit tests for <see cref="NavigableSet{T}" />.
/// </summary>
[TestClass]
public partial class NavigableSetTests
{
    /// <summary>
    /// Verifies the happy-path smoke check: adding unordered items yields sorted enumeration and answers a floor
    /// query.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void NavigableSet_AddAndQuery_ShouldSortAndAnswerFloor()
    {
        var sut = new NavigableSet<int>();

        Assert.IsTrue(sut.Add(30));
        Assert.IsTrue(sut.Add(10));
        Assert.IsTrue(sut.Add(20));

        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, sut.ToList());
        Assert.IsTrue(sut.TryGetFloor(25, out int floor));
        Assert.AreEqual(20, floor);
    }

    /// <summary>
    /// Creates a <see cref="NavigableSet{T}" /> of <see cref="int" /> seeded with the specified items via
    /// <see cref="NavigableSet{T}.Add" />.
    /// </summary>
    /// <param name="items">The items to add, in the given order.</param>
    /// <returns>The populated set.</returns>
    private static NavigableSet<int> CreateSet(params int[] items)
    {
        var set = new NavigableSet<int>();
        foreach (int item in items)
            set.Add(item);

        return set;
    }

    /// <summary>
    /// Materialises the elements of <paramref name="set" /> using the public struct enumerator.
    /// </summary>
    /// <typeparam name="T">The element type of the set.</typeparam>
    /// <param name="set">The set to snapshot.</param>
    /// <returns>A list containing the elements in enumeration order.</returns>
    private static List<T> SnapshotByEnumerator<T>(NavigableSet<T> set)
        where T : notnull
    {
        var result = new List<T>(set.Count);
        foreach (T item in set)
            result.Add(item);

        return result;
    }
}

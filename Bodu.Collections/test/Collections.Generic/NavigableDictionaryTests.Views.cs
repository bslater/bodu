// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionaryTests.Views.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.Ascending" /> yields every entry in ascending key
    /// order.
    /// </summary>
    [TestMethod]
    public void Ascending_WhenDictionaryHasEntries_ShouldYieldEntriesAscending()
    {
        var sut = CreateDictionary(30, 10, 20);

        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, sut.Ascending().Select(e => e.Key).ToList());
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.Descending" /> yields every entry in descending
    /// key order.
    /// </summary>
    [TestMethod]
    public void Descending_WhenDictionaryHasEntries_ShouldYieldEntriesDescending()
    {
        var sut = CreateDictionary(30, 10, 20);

        CollectionAssert.AreEqual(new[] { 30, 20, 10 }, sut.Descending().Select(e => e.Key).ToList());
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.Ascending" /> and
    /// <see cref="NavigableDictionary{TKey, TValue}.Descending" /> yield nothing on an empty dictionary.
    /// </summary>
    [TestMethod]
    public void Views_WhenDictionaryIsEmpty_ShouldYieldNothing()
    {
        var sut = new NavigableDictionary<int, int>();

        Assert.AreEqual(0, sut.Ascending().Count());
        Assert.AreEqual(0, sut.Descending().Count());
        Assert.AreEqual(0, sut.Range(0, 100).Count());
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.Range" /> yields only the entries whose keys fall
    /// within the inclusive bounds, in ascending key order with paired values.
    /// </summary>
    [TestMethod]
    public void Range_WhenBoundsEncloseKeys_ShouldYieldInRangeEntriesAscending()
    {
        var sut = CreateDictionary(50, 10, 40, 20, 30);

        CollectionAssert.AreEqual(new[] { 20, 30, 40 }, sut.Range(20, 40).Select(e => e.Key).ToList());
        CollectionAssert.AreEqual(new[] { 2000, 3000, 4000 }, sut.Range(15, 45).Select(e => e.Value).ToList());
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.Range" /> yields nothing when no stored key falls
    /// between the bounds.
    /// </summary>
    [TestMethod]
    public void Range_WhenNoKeyBetweenBounds_ShouldYieldNothing()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.AreEqual(0, sut.Range(21, 29).Count());
        Assert.AreEqual(0, sut.Range(0, 5).Count());
        Assert.AreEqual(0, sut.Range(35, 99).Count());
    }

    /// <summary>
    /// Verifies that a degenerate single-key range yields the exact hit only.
    /// </summary>
    [TestMethod]
    public void Range_WhenBoundsAreEqual_ShouldYieldExactHitOnly()
    {
        var sut = CreateDictionary(10, 20, 30);

        CollectionAssert.AreEqual(new[] { 20 }, sut.Range(20, 20).Select(e => e.Key).ToList());
        Assert.AreEqual(0, sut.Range(25, 25).Count());
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.Range" /> rejects an inverted bound pair eagerly
    /// with <see cref="ArgumentException" />, before any iteration.
    /// </summary>
    [TestMethod]
    public void Range_WhenLowerBoundExceedsUpperBound_ShouldThrowArgumentExceptionEagerly()
    {
        var sut = CreateDictionary(10, 20, 30);

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = sut.Range(30, 10);
        });

        Assert.AreEqual("lowInclusive", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the views are live: a view obtained before a mutation reflects that mutation when iterated
    /// afresh.
    /// </summary>
    [TestMethod]
    public void Views_WhenIteratedAfterMutation_ShouldReflectCurrentState()
    {
        var sut = CreateDictionary(10, 30);
        IEnumerable<KeyValuePair<int, int>> ascending = sut.Ascending();
        IEnumerable<KeyValuePair<int, int>> descending = sut.Descending();
        IEnumerable<KeyValuePair<int, int>> range = sut.Range(0, 100);

        sut.Add(20, 2000);

        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, ascending.Select(e => e.Key).ToList());
        CollectionAssert.AreEqual(new[] { 30, 20, 10 }, descending.Select(e => e.Key).ToList());
        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, range.Select(e => e.Key).ToList());
    }

    /// <summary>
    /// Verifies that mutating the dictionary mid-iteration causes each view to throw
    /// <see cref="InvalidOperationException" /> on the next advance.
    /// </summary>
    [TestMethod]
    public void Views_WhenMutatedMidIteration_ShouldThrowInvalidOperationException()
    {
        foreach (string view in new[] { "ascending", "descending", "range" })
        {
            var sut = CreateDictionary(1, 2, 3, 4, 5);
            IEnumerable<KeyValuePair<int, int>> sequence = view switch
            {
                "ascending" => sut.Ascending(),
                "descending" => sut.Descending(),
                _ => sut.Range(0, 100),
            };

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                foreach (KeyValuePair<int, int> entry in sequence)
                {
                    if (entry.Key == 3 || entry.Key == 2)
                        sut.Add(99, 9900);
                }
            }, $"The {view} view should fail fast.");
        }
    }
}

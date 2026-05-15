// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultisetTests.Remove.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class MultisetTests
{
    // --------------------------------------------------------
    // Remove(T item)
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Remove"/> returns <see langword="false"/> when the element is absent.
    /// </summary>
    [TestMethod]
    public void Remove_WhenElementAbsent_ShouldReturnFalse()
    {
        var mvd = new Multiset<int>();

        var result = mvd.Remove(99);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Remove"/> returns <see langword="true"/> and decrements the count for an existing element.
    /// </summary>
    [TestMethod]
    public void Remove_WhenElementPresent_ShouldReturnTrueAndDecrementCount()
    {
        var mvd = new Multiset<int>();
        mvd.Add(1, 3);

        var result = mvd.Remove(1);

        Assert.IsTrue(result);
        Assert.AreEqual(2, mvd.CountOf(1));
        Assert.AreEqual(2, mvd.Count);
        Assert.AreEqual(1, mvd.DistinctCount);
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Remove"/> removes the element from the multiset when its last occurrence is removed.
    /// </summary>
    [TestMethod]
    public void Remove_WhenLastOccurrenceRemoved_ShouldEliminateElement()
    {
        var mvd = new Multiset<string>();
        mvd.Add("only");

        mvd.Remove("only");

        Assert.AreEqual(0, mvd.Count);
        Assert.AreEqual(0, mvd.DistinctCount);
        Assert.IsFalse(mvd.Contains("only"));
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Remove"/> does not affect other elements when called for one element.
    /// </summary>
    [TestMethod]
    public void Remove_WhenOneElementRemoved_ShouldNotAffectOtherElements()
    {
        var mvd = new Multiset<string>(["a", "a", "b", "b", "b"]);

        mvd.Remove("a");

        Assert.AreEqual(1, mvd.CountOf("a"));
        Assert.AreEqual(3, mvd.CountOf("b"));
        Assert.AreEqual(4, mvd.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.RemoveAll"/> does not affect other elements.
    /// </summary>
    [TestMethod]
    public void RemoveAll_WhenCalled_ShouldNotAffectOtherElements()
    {
        var mvd = new Multiset<int>();
        mvd.Add(1, 5);
        mvd.Add(2, 3);

        mvd.RemoveAll(1);

        Assert.AreEqual(3, mvd.CountOf(2));
        Assert.AreEqual(3, mvd.Count);
    }

    // --------------------------------------------------------
    // RemoveAll(T item)
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.RemoveAll"/> returns <see langword="false"/> when the element is absent.
    /// </summary>
    [TestMethod]
    public void RemoveAll_WhenElementAbsent_ShouldReturnFalse()
    {
        var mvd = new Multiset<int>();

        var result = mvd.RemoveAll(42);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.RemoveAll"/> returns <see langword="true"/> and removes all occurrences.
    /// </summary>
    [TestMethod]
    public void RemoveAll_WhenElementPresent_ShouldReturnTrueAndRemoveAllOccurrences()
    {
        var mvd = new Multiset<string>(["x", "x", "x", "y"]);

        var result = mvd.RemoveAll("x");

        Assert.IsTrue(result);
        Assert.AreEqual(0, mvd.CountOf("x"));
        Assert.IsFalse(mvd.Contains("x"));
        Assert.AreEqual(1, mvd.Count);
        Assert.AreEqual(1, mvd.DistinctCount);
    }

}

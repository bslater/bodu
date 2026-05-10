// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultisetTests.Remove.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;

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
        Multiset<int> sut = new Multiset<int>();

        var result = sut.Remove(99);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Remove"/> returns <see langword="true"/> and decrements the count for an existing element.
    /// </summary>
    [TestMethod]
    public void Remove_WhenElementPresent_ShouldReturnTrueAndDecrementCount()
    {
        Multiset<int> sut = new Multiset<int>();
        sut.Add(1, 3);

        var result = sut.Remove(1);

        Assert.IsTrue(result);
        Assert.AreEqual(2, sut.CountOf(1));
        Assert.AreEqual(2, sut.Count);
        Assert.AreEqual(1, sut.DistinctCount);
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Remove"/> removes the element from the multiset when its last occurrence is removed.
    /// </summary>
    [TestMethod]
    public void Remove_WhenLastOccurrenceRemoved_ShouldEliminateElement()
    {
        Multiset<string> sut = new Multiset<string>();
        sut.Add("only");

        sut.Remove("only");

        Assert.AreEqual(0, sut.Count);
        Assert.AreEqual(0, sut.DistinctCount);
        Assert.IsFalse(sut.Contains("only"));
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Remove"/> does not affect other elements when called for one element.
    /// </summary>
    [TestMethod]
    public void Remove_WhenOneElementRemoved_ShouldNotAffectOtherElements()
    {
        Multiset<string> sut = new Multiset<string>(["a", "a", "b", "b", "b"]);

        sut.Remove("a");

        Assert.AreEqual(1, sut.CountOf("a"));
        Assert.AreEqual(3, sut.CountOf("b"));
        Assert.AreEqual(4, sut.Count);
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
        Multiset<int> sut = new Multiset<int>();

        var result = sut.RemoveAll(42);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.RemoveAll"/> returns <see langword="true"/> and removes all occurrences.
    /// </summary>
    [TestMethod]
    public void RemoveAll_WhenElementPresent_ShouldReturnTrueAndRemoveAllOccurrences()
    {
        Multiset<string> sut = new Multiset<string>(["x", "x", "x", "y"]);

        var result = sut.RemoveAll("x");

        Assert.IsTrue(result);
        Assert.AreEqual(0, sut.CountOf("x"));
        Assert.IsFalse(sut.Contains("x"));
        Assert.AreEqual(1, sut.Count);
        Assert.AreEqual(1, sut.DistinctCount);
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.RemoveAll"/> does not affect other elements.
    /// </summary>
    [TestMethod]
    public void RemoveAll_WhenCalled_ShouldNotAffectOtherElements()
    {
        Multiset<int> sut = new Multiset<int>();
        sut.Add(1, 5);
        sut.Add(2, 3);

        sut.RemoveAll(1);

        Assert.AreEqual(3, sut.CountOf(2));
        Assert.AreEqual(3, sut.Count);
    }
}

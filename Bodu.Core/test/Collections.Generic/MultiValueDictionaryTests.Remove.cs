// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.Remove.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class MultiValueDictionaryTests
{
    // --------------------------------------------------------
    // Remove(TKey, TValue)
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Remove(TKey,TValue)"/> throws <see cref="ArgumentNullException"/> for a null key.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.Remove(null!, 1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Remove(TKey,TValue)"/> returns <see langword="false"/> when the key is absent.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyAbsent_ShouldReturnFalse()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();

        bool result = sut.Remove("missing", 1);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Remove(TKey,TValue)"/> returns <see langword="false"/> when the value is absent for an existing key.
    /// </summary>
    [TestMethod]
    public void Remove_WhenValueAbsentForExistingKey_ShouldReturnFalse()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("k", 1);

        bool result = sut.Remove("k", 99);

        Assert.IsFalse(result);
        Assert.AreEqual(1, sut.Count);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Remove(TKey,TValue)"/> removes one value and returns <see langword="true"/>.
    /// </summary>
    [TestMethod]
    public void Remove_WhenValuePresent_ShouldReturnTrueAndDecrementCount()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("k", 1);
        sut.Add("k", 2);
        sut.Add("k", 3);

        bool result = sut.Remove("k", 2);

        Assert.IsTrue(result);
        Assert.AreEqual(2, sut.Count);
        Assert.AreEqual(1, sut.KeyCount);
        CollectionAssert.AreEqual(new[] { 1, 3 }, sut["k"].ToList());
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Remove(TKey,TValue)"/> removes the key entry when its last value is removed.
    /// </summary>
    [TestMethod]
    public void Remove_WhenLastValueRemoved_ShouldRemoveKeyEntry()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("k", 42);

        sut.Remove("k", 42);

        Assert.AreEqual(0, sut.Count);
        Assert.AreEqual(0, sut.KeyCount);
        Assert.IsFalse(sut.ContainsKey("k"));
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Remove(TKey,TValue)"/> removes the first occurrence and preserves remaining values when the value appears at the first position.
    /// </summary>
    [TestMethod]
    public void Remove_WhenValueIsFirstInList_ShouldPreserveRemaining()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("k", 10);
        sut.Add("k", 20);
        sut.Add("k", 30);

        bool result = sut.Remove("k", 10);

        Assert.IsTrue(result);
        Assert.AreEqual(2, sut.Count);
        CollectionAssert.AreEqual(new[] { 20, 30 }, sut["k"].ToList());
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Remove(TKey,TValue)"/> removes the value and preserves remaining when the value appears at the last position.
    /// </summary>
    [TestMethod]
    public void Remove_WhenValueIsLastInList_ShouldPreserveRemaining()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("k", 10);
        sut.Add("k", 20);
        sut.Add("k", 30);

        bool result = sut.Remove("k", 30);

        Assert.IsTrue(result);
        Assert.AreEqual(2, sut.Count);
        CollectionAssert.AreEqual(new[] { 10, 20 }, sut["k"].ToList());
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Remove(TKey,TValue)"/> removes only one occurrence when the same value appears multiple times under a key.
    /// </summary>
    [TestMethod]
    public void Remove_WhenValueAppearsMultipleTimes_ShouldRemoveOnlyOneOccurrence()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("k", 5);
        sut.Add("k", 5);
        sut.Add("k", 5);

        bool result = sut.Remove("k", 5);

        Assert.IsTrue(result);
        Assert.AreEqual(2, sut.Count);
        Assert.AreEqual(2, sut["k"].Count);
    }

    // --------------------------------------------------------
    // RemoveAll(TKey)
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.RemoveAll"/> throws <see cref="ArgumentNullException"/> for a null key.
    /// </summary>
    [TestMethod]
    public void RemoveAll_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.RemoveAll(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.RemoveAll"/> returns <see langword="false"/> when the key is absent.
    /// </summary>
    [TestMethod]
    public void RemoveAll_WhenKeyAbsent_ShouldReturnFalse()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();

        bool result = sut.RemoveAll("missing");

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.RemoveAll"/> removes the key and all its values.
    /// </summary>
    [TestMethod]
    public void RemoveAll_WhenKeyPresent_ShouldReturnTrueAndRemoveAllValues()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("x", 1);
        sut.Add("x", 2);
        sut.Add("x", 3);
        sut.Add("y", 4);

        bool result = sut.RemoveAll("x");

        Assert.IsTrue(result);
        Assert.AreEqual(1, sut.Count);
        Assert.AreEqual(1, sut.KeyCount);
        Assert.IsFalse(sut.ContainsKey("x"));
        Assert.IsTrue(sut.ContainsKey("y"));
    }
}

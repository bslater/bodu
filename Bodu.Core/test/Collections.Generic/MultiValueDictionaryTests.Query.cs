// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.Query.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class MultiValueDictionaryTests
{
    // --------------------------------------------------------
    // GetValues(TKey)
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.GetValues"/> throws <see cref="ArgumentNullException"/> for a null key.
    /// </summary>
    [TestMethod]
    public void GetValues_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.GetValues(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.GetValues"/> throws <see cref="KeyNotFoundException"/> when the key is absent.
    /// </summary>
    [TestMethod]
    public void GetValues_WhenKeyAbsent_ShouldThrowKeyNotFoundException()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();

        Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = sut.GetValues("missing");
        });
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.GetValues"/> returns the values for an existing key.
    /// </summary>
    [TestMethod]
    public void GetValues_WhenKeyPresent_ShouldReturnAssociatedValues()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("k", 100);
        sut.Add("k", 200);

        IReadOnlyList<int> result = sut.GetValues("k");

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(100, result[0]);
        Assert.AreEqual(200, result[1]);
    }

    // --------------------------------------------------------
    // TryGetValues(TKey, out IReadOnlyList<TValue>)
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.TryGetValues"/> throws <see cref="ArgumentNullException"/> for a null key.
    /// </summary>
    [TestMethod]
    public void TryGetValues_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.TryGetValues(null!, out _);
        });
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.TryGetValues"/> returns <see langword="false"/> and an empty list when the key is absent.
    /// </summary>
    [TestMethod]
    public void TryGetValues_WhenKeyAbsent_ShouldReturnFalseAndEmptyList()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();

        bool found = sut.TryGetValues("missing", out IReadOnlyList<int> values);

        Assert.IsFalse(found);
        Assert.IsNotNull(values);
        Assert.AreEqual(0, values.Count);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.TryGetValues"/> returns <see langword="true"/> and the correct values when the key is present.
    /// </summary>
    [TestMethod]
    public void TryGetValues_WhenKeyPresent_ShouldReturnTrueAndValues()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("k", 5);
        sut.Add("k", 10);

        bool found = sut.TryGetValues("k", out IReadOnlyList<int> values);

        Assert.IsTrue(found);
        Assert.AreEqual(2, values.Count);
    }

    // --------------------------------------------------------
    // ContainsValue(TKey, TValue)
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.ContainsValue"/> throws <see cref="ArgumentNullException"/> for a null key.
    /// </summary>
    [TestMethod]
    public void ContainsValue_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.ContainsValue(null!, 1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.ContainsValue"/> returns <see langword="false"/> when the key is absent.
    /// </summary>
    [TestMethod]
    public void ContainsValue_WhenKeyAbsent_ShouldReturnFalse()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();

        Assert.IsFalse(sut.ContainsValue("k", 1));
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.ContainsValue"/> returns <see langword="false"/> when the key exists but the value is absent.
    /// </summary>
    [TestMethod]
    public void ContainsValue_WhenKeyPresentButValueAbsent_ShouldReturnFalse()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("k", 1);

        Assert.IsFalse(sut.ContainsValue("k", 99));
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.ContainsValue"/> returns <see langword="true"/> when both key and value are present.
    /// </summary>
    [TestMethod]
    public void ContainsValue_WhenKeyAndValuePresent_ShouldReturnTrue()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("k", 42);

        Assert.IsTrue(sut.ContainsValue("k", 42));
    }
}

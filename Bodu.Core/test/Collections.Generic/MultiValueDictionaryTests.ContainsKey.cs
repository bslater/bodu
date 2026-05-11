// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.ContainsKey.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class MultiValueDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.ContainsKey"/> returns <see langword="false"/> for an absent key.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyAbsent_ShouldReturnFalse()
    {
        var mvd = new MultiValueDictionary<string, int>();

        Assert.IsFalse(mvd.ContainsKey("missing"));
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.ContainsKey"/> returns <see langword="true"/> when the key is present.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyPresent_ShouldReturnTrue()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("k", 1);

        Assert.IsTrue(mvd.ContainsKey("k"));
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.ContainsKey"/> throws <see cref="ArgumentNullException"/> for a null key.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var mvd = new MultiValueDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = mvd.ContainsKey(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.ContainsKey"/> matches a struct key by
    /// value, not by instance identity.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyIsValueTypeStruct_ShouldMatchByValue()
    {
        var mvd = new MultiValueDictionary<Coord, string>();
        mvd.Add(new Coord(7, 8), "x");

        Assert.IsTrue(mvd.ContainsKey(new Coord(7, 8)));
        Assert.IsFalse(mvd.ContainsKey(new Coord(7, 9)));
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.ContainsKey"/> honours the custom key
    /// comparer, returning <see langword="true"/> for a key that differs only in case.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenCustomComparerUsed_ShouldMatchCaseInsensitively()
    {
        var mvd =
            new MultiValueDictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
        mvd.Add("FOO", 1);

        Assert.IsTrue(mvd.ContainsKey("foo"));
        Assert.IsTrue(mvd.ContainsKey("FOO"));
        Assert.IsTrue(mvd.ContainsKey("Foo"));
    }
}

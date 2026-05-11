// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.ContainsValue.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class MultiValueDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.ContainsValue"/> throws <see cref="ArgumentNullException"/> for a null key.
    /// </summary>
    [TestMethod]
    public void ContainsValue_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var mvd = new MultiValueDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = mvd.ContainsValue(null!, 1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.ContainsValue"/> returns <see langword="false"/> when the key is absent.
    /// </summary>
    [TestMethod]
    public void ContainsValue_WhenKeyAbsent_ShouldReturnFalse()
    {
        var mvd = new MultiValueDictionary<string, int>();

        Assert.IsFalse(mvd.ContainsValue("k", 1));
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.ContainsValue"/> returns <see langword="false"/> when the key exists but the value is absent.
    /// </summary>
    [TestMethod]
    public void ContainsValue_WhenKeyPresentButValueAbsent_ShouldReturnFalse()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("k", 1);

        Assert.IsFalse(mvd.ContainsValue("k", 99));
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.ContainsValue"/> returns <see langword="true"/> when both key and value are present.
    /// </summary>
    [TestMethod]
    public void ContainsValue_WhenKeyAndValuePresent_ShouldReturnTrue()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("k", 42);

        Assert.IsTrue(mvd.ContainsValue("k", 42));
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey, TValue}.ContainsValue" /> uses the configured key comparer.
    /// </summary>
    [TestMethod]
    public void ContainsValue_WhenCustomComparerUsed_ShouldSearchEquivalentKey()
    {
        var mvd = new MultiValueDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        mvd.Add("Alpha", 1);

        Assert.IsTrue(mvd.ContainsValue("alpha", 1));
    }
}

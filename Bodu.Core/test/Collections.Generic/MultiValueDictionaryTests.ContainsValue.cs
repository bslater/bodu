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
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.ContainsValue"/> locates a struct value
    /// by value equality, not by reference.
    /// </summary>
    [TestMethod]
    public void ContainsValue_WhenValueIsValueTypeStruct_ShouldMatchByValue()
    {
        var mvd = new MultiValueDictionary<string, Coord>();
        mvd.Add("k", new Coord(5, 6));

        Assert.IsTrue(mvd.ContainsValue("k", new Coord(5, 6)));
        Assert.IsFalse(mvd.ContainsValue("k", new Coord(5, 7)));
    }

        /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.ContainsValue"/> uses reference equality
    /// for reference-type values with no overridden <see cref="object.Equals(object)"/>.
    /// </summary>
    [TestMethod]
    public void ContainsValue_WhenValueIsReferenceTypeAndSameInstance_ShouldReturnTrue()
    {
        var l1 = new Label("hello");
        var l2 = new Label("hello");

        var mvd = new MultiValueDictionary<string, Label>();
        mvd.Add("k", l1);

        Assert.IsTrue(mvd.ContainsValue("k", l1));
        Assert.IsFalse(mvd.ContainsValue("k", l2));
    }

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
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.ContainsValue"/> returns <see langword="true"/> when the stored value is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void ContainsValue_WhenValueIsNull_ShouldReturnTrue()
    {
        var mvd = new MultiValueDictionary<string, string?>();
        mvd.Add("k", null);

        Assert.IsTrue(mvd.ContainsValue("k", null));
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

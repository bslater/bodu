// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.Keys.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Collections.Generic;

public partial class MultiValueDictionaryTests
{

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Keys"/> no longer contains a key after all its values are removed via <see cref="MultiValueDictionary{TKey,TValue}.RemoveAll"/>.
    /// </summary>
    [TestMethod]
    public void Keys_AfterRemoveAll_ShouldNotContainRemovedKey()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("x", 1);
        mvd.Add("y", 2);

        mvd.RemoveAll("x");

        CollectionAssert.DoesNotContain(mvd.Keys.ToList(), "x");
        CollectionAssert.Contains(mvd.Keys.ToList(), "y");
    }

    /// <summary>
    /// Verifies that the keys view cannot be mutated through collection interfaces.
    /// </summary>
    [TestMethod]
    public void Keys_WhenCastedToCollection_ShouldBeReadOnly()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);

        var keys = (ICollection<string>)mvd.Keys;

        Assert.IsTrue(keys.IsReadOnly);

        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            keys.Add("b");
        });
    }

    /// <summary>
    /// Verifies that key enumeration detects structural modification.
    /// </summary>
    [TestMethod]
    public void Keys_WhenDictionaryModifiedDuringEnumeration_ShouldThrowInvalidOperationException()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);
        mvd.Add("b", 2);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (var _ in mvd.Keys)
                mvd.Add("c", 3);
        });
    }

    /// <summary>
    /// Verifies that the keys view reflects subsequent additions and removals.
    /// </summary>
    [TestMethod]
    public void Keys_WhenDictionaryMutatedAfterViewCaptured_ShouldReflectCurrentKeys()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);

        IReadOnlyCollection<string> keys = mvd.Keys;

        mvd.Add("b", 2);
        mvd.RemoveAll("a");

        CollectionAssert.AreEqual(new[] { "b" }, keys.ToList());
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Keys"/> is empty when the dictionary contains no entries.
    /// </summary>
    [TestMethod]
    public void Keys_WhenEmpty_ShouldBeEmpty()
    {
        var mvd = new MultiValueDictionary<string, int>();

        Assert.AreEqual(0, mvd.Keys.Count);
    }
    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Keys"/> contains all distinct keys.
    /// </summary>
    [TestMethod]
    public void Keys_WhenItemsAdded_ShouldContainAllDistinctKeys()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("x", 1);
        mvd.Add("y", 2);
        mvd.Add("x", 3);

        CollectionAssert.Contains(mvd.Keys.ToList(), "x");
        CollectionAssert.Contains(mvd.Keys.ToList(), "y");
        Assert.AreEqual(2, mvd.Keys.Count);
    }

}

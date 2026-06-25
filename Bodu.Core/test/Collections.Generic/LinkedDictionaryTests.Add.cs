// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LinkedDictionaryTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class LinkedDictionaryTests
{
    /// <summary>
    /// Verifies that adding a new key appends the entry to the end of the iteration order.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyIsNew_ShouldAppendToEnd()
    {
        var dictionary = CreatePopulated();

        dictionary.Add("d", 4);

        CollectionAssert.AreEqual(new[] { "a", "b", "c", "d" }, dictionary.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="LinkedDictionary{TKey, TValue}.Add(TKey, TValue)" /> throws when the key already exists.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyAlreadyExists_ShouldThrowExactly()
    {
        var dictionary = CreatePopulated();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            dictionary.Add("b", 99);
        });
    }

    /// <summary>
    /// Verifies that <see cref="LinkedDictionary{TKey, TValue}.Add(TKey, TValue)" /> throws when the key is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyIsNull_ShouldThrowExactly()
    {
        var dictionary = new LinkedDictionary<string, int>();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            dictionary.Add(null!, 1);
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that adding a key/value pair increments the count.
    /// </summary>
    [TestMethod]
    public void Add_WhenPairAdded_ShouldIncrementCount()
    {
        var dictionary = new LinkedDictionary<string, int>();

        dictionary.Add(new KeyValuePair<string, int>("a", 1));

        Assert.AreEqual(1, dictionary.Count);
        Assert.AreEqual(1, dictionary["a"]);
    }
}

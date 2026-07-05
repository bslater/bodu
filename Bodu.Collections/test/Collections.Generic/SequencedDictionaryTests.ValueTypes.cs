// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionaryTests.ValueTypes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class SequencedDictionaryTests
{
    /// <summary>
    /// Verifies that value-type keys preserve insertion order through enumeration.
    /// </summary>
    [TestMethod]
    public void ValueTypes_WhenStructKeys_ShouldPreserveInsertionOrder()
    {
        var dictionary = new SequencedDictionary<int, string>();
        dictionary.Add(3, "three");
        dictionary.Add(1, "one");
        dictionary.Add(2, "two");

        CollectionAssert.AreEqual(new[] { 3, 1, 2 }, dictionary.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that a default value-type value is stored and retrieved without being mistaken for an absent key.
    /// </summary>
    [TestMethod]
    public void ValueTypes_WhenValueIsDefault_ShouldStoreAndRetrieve()
    {
        var dictionary = new SequencedDictionary<string, int>
        {
            ["zero"] = 0,
        };

        Assert.IsTrue(dictionary.TryGetValue("zero", out int value));
        Assert.AreEqual(0, value);
        Assert.IsTrue(dictionary.Contains(new KeyValuePair<string, int>("zero", 0)));
    }

    /// <summary>
    /// Verifies that access-order reordering works with value-type keys.
    /// </summary>
    [TestMethod]
    public void ValueTypes_WhenAccessOrderWithStructKeys_ShouldMoveToTailOnRead()
    {
        var dictionary = new SequencedDictionary<int, string>(accessOrder: true)
        {
            [1] = "one",
            [2] = "two",
            [3] = "three",
        };

        _ = dictionary[1];

        CollectionAssert.AreEqual(new[] { 2, 3, 1 }, dictionary.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that a default value-type key (zero) is a valid, distinct key.
    /// </summary>
    [TestMethod]
    public void ValueTypes_WhenKeyIsDefault_ShouldBeDistinctKey()
    {
        var dictionary = new SequencedDictionary<int, string>
        {
            [0] = "zero",
            [1] = "one",
        };

        Assert.IsTrue(dictionary.ContainsKey(0));
        Assert.AreEqual("zero", dictionary[0]);
        Assert.AreEqual(2, dictionary.Count);
    }
}

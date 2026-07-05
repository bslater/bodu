// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultingDictionaryTests.Remove.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DefaultingDictionaryTests
{
    /// <summary>
    /// Verifies that removing a stored entry succeeds and that a subsequent indexer read invokes the factory again.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyStored_ShouldRemoveAndAllowRematerialization()
    {
        var invocations = 0;
        var dictionary = new DefaultingDictionary<string, int>(_ =>
        {
            invocations++;
            return 7;
        });

        _ = dictionary["a"];

        Assert.IsTrue(dictionary.Remove("a"));
        Assert.AreEqual(0, dictionary.Count);

        _ = dictionary["a"];

        Assert.AreEqual(2, invocations);
    }

    /// <summary>
    /// Verifies that removing a missing key returns <see langword="false" /> without invoking the factory.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyMissing_ShouldReturnFalseWithoutMaterializing()
    {
        var invocations = 0;
        var dictionary = new DefaultingDictionary<string, int>(_ =>
        {
            invocations++;
            return 7;
        });

        Assert.IsFalse(dictionary.Remove("a"));
        Assert.AreEqual(0, invocations);
    }

    /// <summary>
    /// Verifies that the pair overload removes only an exactly matching stored pair.
    /// </summary>
    [TestMethod]
    public void Remove_WhenPairSupplied_ShouldRemoveOnlyMatchingPair()
    {
        var dictionary = new DefaultingDictionary<string, int>(_ => 0);
        dictionary.Add("a", 1);

        Assert.IsFalse(dictionary.Remove(new KeyValuePair<string, int>("a", 2)));
        Assert.IsTrue(dictionary.Remove(new KeyValuePair<string, int>("a", 1)));
        Assert.AreEqual(0, dictionary.Count);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultingDictionaryTests.Reentrancy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DefaultingDictionaryTests
{
    /// <summary>
    /// Verifies the documented reentrancy contract: when the factory itself assigns the key being materialized, the
    /// factory's return value overwrites the factory's own write and is what the indexer stores and returns.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenFactoryAssignsSameKey_ShouldOverwriteWithFactoryReturnValue()
    {
        DefaultingDictionary<string, int> dictionary = null!;
        dictionary = new DefaultingDictionary<string, int>(key =>
        {
            dictionary[key] = -1;   // The factory's own write for the key being materialized…
            return 42;              // …is overwritten by the factory's return value.
        });

        var value = dictionary["a"];

        Assert.AreEqual(42, value);
        Assert.AreEqual(42, dictionary["a"]);
        Assert.AreEqual(1, dictionary.Count);
    }

    /// <summary>
    /// Verifies that entries the factory adds for <i>other</i> keys survive materialization alongside the requested
    /// key's stored result.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenFactoryAddsOtherKeys_ShouldKeepThoseEntries()
    {
        DefaultingDictionary<string, int> dictionary = null!;
        dictionary = new DefaultingDictionary<string, int>(key =>
        {
            dictionary["other"] = 99;
            return key.Length;
        });

        var value = dictionary["a"];

        Assert.AreEqual(1, value);
        Assert.AreEqual(99, dictionary["other"]);
        Assert.AreEqual(2, dictionary.Count);
    }
}

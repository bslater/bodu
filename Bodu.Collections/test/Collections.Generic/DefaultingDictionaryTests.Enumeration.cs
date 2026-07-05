// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultingDictionaryTests.Enumeration.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DefaultingDictionaryTests
{
    /// <summary>
    /// Verifies that enumeration yields only actually-stored entries — lookups that did not materialize contribute
    /// nothing.
    /// </summary>
    [TestMethod]
    public void Enumeration_WhenEntriesStored_ShouldYieldStoredEntriesOnly()
    {
        var dictionary = new DefaultingDictionary<string, int>(_ => 0);
        dictionary.Add("a", 1);
        _ = dictionary["b"];
        _ = dictionary.TryGetValue("c", out _);
        Assert.IsFalse(dictionary.ContainsKey("d"));

        var pairs = dictionary.ToList();

        CollectionAssert.AreEquivalent(
            new[]
            {
                new KeyValuePair<string, int>("a", 1),
                new KeyValuePair<string, int>("b", 0),
            },
            pairs);
    }

    /// <summary>
    /// Verifies that enumerating an empty dictionary yields nothing and never invokes the factory.
    /// </summary>
    [TestMethod]
    public void Enumeration_WhenEmpty_ShouldYieldNothing()
    {
        var invocations = 0;
        var dictionary = new DefaultingDictionary<string, int>(_ =>
        {
            invocations++;
            return 0;
        });

        Assert.AreEqual(0, dictionary.ToList().Count);
        Assert.AreEqual(0, invocations);
    }
}

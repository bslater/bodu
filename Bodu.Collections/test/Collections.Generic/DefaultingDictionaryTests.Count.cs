// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultingDictionaryTests.Count.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DefaultingDictionaryTests
{
    /// <summary>
    /// Verifies that the count reflects only actually-stored entries and that reading it never invokes the factory.
    /// </summary>
    [TestMethod]
    public void Count_WhenEntriesMaterialized_ShouldCountOnlyStoredEntries()
    {
        var dictionary = new DefaultingDictionary<string, int>(_ => 0);

        Assert.AreEqual(0, dictionary.Count);

        _ = dictionary["a"];
        dictionary["b"] = 2;

        Assert.AreEqual(2, dictionary.Count);

        _ = dictionary.TryGetValue("c", out _);

        Assert.AreEqual(2, dictionary.Count);
    }
}

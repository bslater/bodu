// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.IReadOnlyCollection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class MultiValueDictionaryTests
{

    /// <summary>
    /// Verifies that <see cref="IReadOnlyCollection{T}.Count" /> accessed via the interface returns the number of distinct keys.
    /// </summary>
    [TestMethod]
    public void Count_WhenAccessedViaIReadOnlyCollectionInterface_ShouldReturnKeyCount()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);
        mvd.Add("a", 2);
        mvd.Add("b", 3);

        IReadOnlyCollection<KeyValuePair<string, IReadOnlyList<int>>> collection = mvd;

        Assert.HasCount(mvd.KeyCount, collection);
    }

    /// <summary>
    /// Verifies that reading <see cref="IReadOnlyCollection{T}.Count" /> directly through the interface returns the
    /// number of distinct keys.
    /// </summary>
    [TestMethod]
    public void Count_WhenReadDirectlyViaIReadOnlyCollectionInterface_ShouldReturnKeyCount()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);
        mvd.Add("a", 2);
        mvd.Add("b", 3);

        IReadOnlyCollection<KeyValuePair<string, IReadOnlyList<int>>> collection = mvd;

        Assert.HasCount(2, collection);
    }

}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.IReadOnlyCollection.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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

        Assert.AreEqual(mvd.KeyCount, collection.Count);
    }
}

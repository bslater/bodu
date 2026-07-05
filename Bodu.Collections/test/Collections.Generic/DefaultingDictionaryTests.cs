// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultingDictionaryTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

[TestClass]
public partial class DefaultingDictionaryTests
{
    /// <summary>
    /// Verifies that reading a missing key materializes the factory default, stores it, and returns it, while a
    /// subsequent read returns the stored value.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void DefaultingDictionary_WhenMissingKeyRead_ShouldMaterializeStoreAndReturnDefault()
    {
        var dictionary = new DefaultingDictionary<string, List<int>>(_ => []);

        dictionary["odd"].Add(1);
        dictionary["odd"].Add(3);

        Assert.AreEqual(1, dictionary.Count);
        CollectionAssert.AreEqual(new[] { 1, 3 }, dictionary["odd"]);
    }
}

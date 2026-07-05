// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultingDictionaryTests.ContainsKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DefaultingDictionaryTests
{
    /// <summary>
    /// Verifies that a missing key is not reported as contained and that the lookup does not materialize a default.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyMissing_ShouldReturnFalseWithoutMaterializing()
    {
        var invocations = 0;
        var dictionary = new DefaultingDictionary<string, int>(_ =>
        {
            invocations++;
            return -1;
        });

        Assert.IsFalse(dictionary.ContainsKey("a"));
        Assert.AreEqual(0, invocations);
        Assert.AreEqual(0, dictionary.Count);
    }

    /// <summary>
    /// Verifies that a key becomes contained only after it has been stored — whether by the indexer getter, the
    /// setter, or <c>Add</c>.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyStored_ShouldReturnTrue()
    {
        var dictionary = new DefaultingDictionary<string, int>(_ => 7);

        _ = dictionary["a"];

        Assert.IsTrue(dictionary.ContainsKey("a"));
    }
}

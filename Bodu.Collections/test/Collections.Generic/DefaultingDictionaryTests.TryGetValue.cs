// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultingDictionaryTests.TryGetValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DefaultingDictionaryTests
{
    /// <summary>
    /// Verifies that a missing key returns <see langword="false" /> without invoking the factory or storing anything.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyMissing_ShouldReturnFalseWithoutMaterializing()
    {
        var invocations = 0;
        var dictionary = new DefaultingDictionary<string, int>(_ =>
        {
            invocations++;
            return -1;
        });

        Assert.IsFalse(dictionary.TryGetValue("a", out int value));
        Assert.AreEqual(0, value);
        Assert.AreEqual(0, invocations);
        Assert.AreEqual(0, dictionary.Count);
    }

    /// <summary>
    /// Verifies that a stored key returns its stored value.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyStored_ShouldReturnStoredValue()
    {
        var dictionary = new DefaultingDictionary<string, int>(_ => -1);
        dictionary.Add("a", 42);

        Assert.IsTrue(dictionary.TryGetValue("a", out int value));
        Assert.AreEqual(42, value);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.TotalTouches.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class EvictingDictionaryTests
{

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.TotalTouches" /> resets to zero after Clear is called.
    /// </summary>
    [TestMethod]
    public void TotalTouches_WhenCleared_ShouldResetToZero()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("x", 100);
        dictionary.Touch("x");
        dictionary.Touch("x");

        Assert.IsTrue(dictionary.TotalTouches > 0);

        dictionary.Clear();

        Assert.AreEqual(0, dictionary.TotalTouches);
    }
    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.TotalTouches" /> is zero when a new dictionary is created.
    /// </summary>
    [TestMethod]
    public void TotalTouches_WhenDictionaryIsNew_ShouldBeZero()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        Assert.AreEqual(0, dictionary.TotalTouches);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.TotalTouches" /> increments when a key is accessed via indexer get.
    /// </summary>
    [TestMethod]
    public void TotalTouches_WhenKeyAccessedViaIndexer_ShouldIncrement()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("key", 99);

        var before = dictionary.TotalTouches;
        _ = dictionary["key"];

        Assert.AreEqual(before + 1, dictionary.TotalTouches);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.TotalTouches" /> increments when a key is accessed via TryGetValue.
    /// </summary>
    [TestMethod]
    public void TotalTouches_WhenKeyAccessedViaTryGetValue_ShouldIncrement()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("a", 1);

        var before = dictionary.TotalTouches;
        dictionary.TryGetValue("a", out _);

        Assert.AreEqual(before + 1, dictionary.TotalTouches);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.TotalTouches" /> increments when a key is accessed via Touch.
    /// </summary>
    [TestMethod]
    public void TotalTouches_WhenKeyIsTouched_ShouldIncrement()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("x", 10);

        var before = dictionary.TotalTouches;
        dictionary.Touch("x");

        Assert.AreEqual(before + 1, dictionary.TotalTouches);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.TotalTouches" /> is unaffected by setting values via indexer.
    /// </summary>
    [TestMethod]
    public void TotalTouches_WhenSettingViaIndexer_ShouldNotChange()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        var before = dictionary.TotalTouches;

        dictionary["new"] = 1;
        dictionary["new"] = 2;

        Assert.AreEqual(before, dictionary.TotalTouches);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.TotalTouches" /> does not increment when Touch is called on a missing key.
    /// </summary>
    [TestMethod]
    public void TotalTouches_WhenTouchCalledWithMissingKey_ShouldNotIncrement()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        var before = dictionary.TotalTouches;

        dictionary.Touch("missing");

        Assert.AreEqual(before, dictionary.TotalTouches);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.TotalTouches" /> does not increment when TryGetValue is called on a missing key.
    /// </summary>
    [TestMethod]
    public void TotalTouches_WhenTryGetValueFails_ShouldNotIncrement()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        var before = dictionary.TotalTouches;

        dictionary.TryGetValue("missing", out _);

        Assert.AreEqual(before, dictionary.TotalTouches);
    }

}

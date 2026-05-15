// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodedDictionaryTests.TryGetValue.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats;

public sealed partial class BencodedDictionaryTests
{

    /// <summary>
    /// Verifies that <see cref="BencodedDictionary.TryGetValue(BencodedString, out BencodedValue)" /> returns
    /// <see langword="true" /> for an existing key and outputs the associated value.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyExists_ForBencodedStringKey_ShouldReturnTrueWithValue()
    {
        BencodedDictionary dict = new(CowMooSpamEggs());

        bool found = dict.TryGetValue(BencodedString.FromUtf8("cow"), out BencodedValue value);

        Assert.IsTrue(found);
        Assert.AreEqual("moo", ((BencodedString)value).GetUtf8String());
    }

    /// <summary>
    /// Verifies that <see cref="BencodedDictionary.TryGetValue(string, out BencodedValue)" /> returns
    /// <see langword="true" /> for an existing UTF-8 key and outputs the associated value.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyExists_ForUtf8StringKey_ShouldReturnTrueWithValue()
    {
        BencodedDictionary dict = new(CowMooSpamEggs());

        bool found = dict.TryGetValue("spam", out BencodedValue value);

        Assert.IsTrue(found);
        Assert.AreEqual("eggs", ((BencodedString)value).GetUtf8String());
    }

    /// <summary>
    /// Verifies that <see cref="BencodedDictionary.TryGetValue(BencodedString, out BencodedValue)" /> returns
    /// <see langword="false" /> when the key is absent.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyMissing_ForBencodedStringKey_ShouldReturnFalse()
    {
        BencodedDictionary dict = new(CowMooSpamEggs());

        bool found = dict.TryGetValue(BencodedString.FromUtf8("missing"), out _);

        Assert.IsFalse(found);
    }

    /// <summary>
    /// Verifies that <see cref="BencodedDictionary.TryGetValue(string, out BencodedValue)" /> rejects a
    /// <see langword="null" /> key with <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenNullUtf8StringKey_ShouldThrowArgumentNullException()
    {
        BencodedDictionary dict = new(CowMooSpamEggs());

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = dict.TryGetValue((string)null!, out _);
        });

        Assert.AreEqual("key", ex.ParamName);
    }

}

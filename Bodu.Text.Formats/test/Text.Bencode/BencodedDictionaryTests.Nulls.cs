// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodedDictionaryTests.Nulls.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

public sealed partial class BencodedDictionaryTests
{

    /// <summary>
    /// Verifies that the constructor rejects duplicate keys with <see cref="ArgumentException" /> carrying the
    /// resx-backed message.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenDuplicateKeys_ShouldThrowExactly()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new BencodedDictionary(new[]
            {
                new KeyValuePair<BencodedString, BencodedValue>(BencodedString.FromUtf8("k"), new BencodedInteger(1)),
                new KeyValuePair<BencodedString, BencodedValue>(BencodedString.FromUtf8("k"), new BencodedInteger(2)),
            });
        });

        Assert.AreEqual("items", ex.ParamName);
        StringAssert.Contains(ex.Message, FormatsResourceStrings.Arg_Invalid_DuplicateDictionaryKey);
    }
    /// <summary>
    /// Verifies that the constructor rejects a <see langword="null" /> items enumerable.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenItemsIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new BencodedDictionary(null!);
        });

        Assert.AreEqual("items", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the constructor rejects a pair whose key is <see langword="null" /> with
    /// <see cref="ArgumentException" /> carrying the resx-backed message.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenPairKeyIsNull_ShouldThrowExactly()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new BencodedDictionary(new[]
            {
                new KeyValuePair<BencodedString, BencodedValue>(null!, new BencodedInteger(1)),
            });
        });

        Assert.AreEqual("items", ex.ParamName);
        StringAssert.Contains(ex.Message, FormatsResourceStrings.Arg_Invalid_NullDictionaryKey);
    }

    /// <summary>
    /// Verifies that the constructor rejects a pair whose value is <see langword="null" /> with
    /// <see cref="ArgumentException" /> carrying the resx-backed message.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenPairValueIsNull_ShouldThrowExactly()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new BencodedDictionary(new[]
            {
                new KeyValuePair<BencodedString, BencodedValue>(BencodedString.FromUtf8("k"), null!),
            });
        });

        Assert.AreEqual("items", ex.ParamName);
        StringAssert.Contains(ex.Message, FormatsResourceStrings.Arg_Invalid_NullDictionaryValue);
    }

}

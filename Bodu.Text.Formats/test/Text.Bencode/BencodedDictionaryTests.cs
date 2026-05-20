// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodedDictionaryTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

/// <summary>
/// Behavioural tests for <see cref="BencodedDictionary" />. Partial files split coverage by member or subject.
/// </summary>
[TestClass]
public sealed partial class BencodedDictionaryTests
{

    /// <summary>
    /// Verifies that the constructor stores the supplied pairs and exposes them via
    /// <see cref="BencodedDictionary.Items" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenPairsProvided_ShouldExposeItems()
    {
        BencodedDictionary dict = new(CowMooSpamEggs());

        Assert.AreEqual(2, dict.Count);
    }

    /// <summary>
    /// Verifies that the indexer returns the value associated with the supplied key.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyExists_ShouldReturnAssociatedValue()
    {
        BencodedDictionary dict = new(CowMooSpamEggs());

        Assert.AreEqual("moo", ((BencodedString)dict[BencodedString.FromUtf8("cow")]).GetUtf8String());
    }

    /// <summary>
    /// Verifies that <see cref="BencodedValue.Kind" /> returns <see cref="BencodedValueKind.Dictionary" />.
    /// </summary>
    [TestMethod]
    public void Kind_ShouldReturnDictionary()
    {
        BencodedDictionary dict = new(Array.Empty<KeyValuePair<BencodedString, BencodedValue>>());

        Assert.AreEqual(BencodedValueKind.Dictionary, dict.Kind);
    }
    /// <summary>
    /// Returns two sample pairs used across happy-path tests: <c>cow:moo</c> and <c>spam:eggs</c>.
    /// </summary>
    /// <returns>An enumerable of two pairs.</returns>
    private static IEnumerable<KeyValuePair<BencodedString, BencodedValue>> CowMooSpamEggs() =>
        new[]
        {
            new KeyValuePair<BencodedString, BencodedValue>(BencodedString.FromUtf8("cow"), BencodedString.FromUtf8("moo")),
            new KeyValuePair<BencodedString, BencodedValue>(BencodedString.FromUtf8("spam"), BencodedString.FromUtf8("eggs")),
        };

}

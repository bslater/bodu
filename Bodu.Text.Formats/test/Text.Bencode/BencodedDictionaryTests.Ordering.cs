// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodedDictionaryTests.Ordering.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

public sealed partial class BencodedDictionaryTests
{

    /// <summary>
    /// Verifies that <see cref="BencodedDictionary.GetOrderedItems" /> orders by raw byte value, not by UTF-8
    /// codepoint. The test pairs an ASCII tilde (<c>0x7E</c>) with a multi-byte UTF-8 sequence whose first byte
    /// is <c>0xC3</c>; raw byte order places the tilde first, even though the UTF-8 codepoint for "é" (U+00E9)
    /// is larger.
    /// </summary>
    [TestMethod]
    public void GetOrderedItems_ShouldOrderByRawByteNotByUtf8Codepoint()
    {
        BencodedString tilde = new([0x7E]);
        BencodedString eAccent = new([0xC3, 0xA9]);

        BencodedDictionary dict = new(
        [
            new KeyValuePair<BencodedString, BencodedValue>(eAccent, new BencodedInteger(1)),
            new KeyValuePair<BencodedString, BencodedValue>(tilde, new BencodedInteger(2)),
        ]);

        var orderedKeys = dict.GetOrderedItems()
            .Select(pair => pair.Key.Bytes.ToArray())
            .ToArray();

        Assert.AreEqual(2, orderedKeys.Length);
        Assert.AreEqual(0x7E, orderedKeys[0][0]);
        Assert.AreEqual(0xC3, orderedKeys[1][0]);
    }
    /// <summary>
    /// Verifies that <see cref="BencodedDictionary.GetOrderedItems" /> enumerates entries in raw-byte-string key
    /// order regardless of the order in which keys were supplied to the constructor.
    /// </summary>
    [TestMethod]
    public void GetOrderedItems_WhenKeysSuppliedOutOfOrder_ShouldEnumerateSortedByRawBytes()
    {
        BencodedDictionary dict = new(
        [
            new KeyValuePair<BencodedString, BencodedValue>(BencodedString.FromUtf8("spam"), new BencodedInteger(2)),
            new KeyValuePair<BencodedString, BencodedValue>(BencodedString.FromUtf8("cow"), new BencodedInteger(1)),
            new KeyValuePair<BencodedString, BencodedValue>(BencodedString.FromUtf8("apple"), new BencodedInteger(3)),
        ]);

        var orderedKeys = dict.GetOrderedItems()
            .Select(pair => pair.Key.GetUtf8String())
            .ToArray();

        CollectionAssert.AreEqual(new[] { "apple", "cow", "spam" }, orderedKeys);
    }

}

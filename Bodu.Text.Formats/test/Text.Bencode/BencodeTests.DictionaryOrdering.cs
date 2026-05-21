// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeTests.DictionaryOrdering.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

public sealed partial class BencodeTests
{

    /// <summary>
    /// Verifies that <see cref="Bencode.Decode(ReadOnlySpan{byte})" /> rejects a dictionary that lists the same
    /// key twice.
    /// </summary>
    [TestMethod]
    public void Decode_WhenDictionaryHasDuplicateKeys_ShouldThrowBencodeFormatException()
    {
        BencodeFormatException ex = Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = Bencode.Decode(Bytes("d1:a0:1:a0:e"));
        });

        // The encoded duplicate-key case fails the "strictly greater than previous" sort-order check, so the
        // decoder reports the unordered-keys message rather than a separate duplicate-key error.
        Assert.AreEqual(FormatsResourceStrings.Format_Invalid_BencodeUnorderedDictionaryKeys, ex.Message);
    }
    /// <summary>
    /// Verifies that <see cref="Bencode.Decode(ReadOnlySpan{byte})" /> rejects a dictionary whose encoded keys
    /// are not sorted in raw byte order. The input here lists <c>b</c> before <c>a</c>.
    /// </summary>
    [TestMethod]
    public void Decode_WhenDictionaryKeysOutOfOrder_ShouldThrowBencodeFormatException()
    {
        BencodeFormatException ex = Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = Bencode.Decode(Bytes("d1:b0:1:a0:e"));
        });

        Assert.AreEqual(FormatsResourceStrings.Format_Invalid_BencodeUnorderedDictionaryKeys, ex.Message);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.Encode(BencodedValue)" /> emits dictionary entries in raw-byte-string
    /// key order regardless of the order in which keys were supplied to the <see cref="BencodedDictionary" />
    /// constructor.
    /// </summary>
    [TestMethod]
    public void Encode_WhenDictionaryConstructedWithUnsortedKeys_ShouldEmitSortedOutput()
    {
        BencodedDictionary dict = new(new[]
        {
            new KeyValuePair<BencodedString, BencodedValue>(BencodedString.FromUtf8("zebra"), new BencodedInteger(1)),
            new KeyValuePair<BencodedString, BencodedValue>(BencodedString.FromUtf8("alpha"), new BencodedInteger(2)),
            new KeyValuePair<BencodedString, BencodedValue>(BencodedString.FromUtf8("mango"), new BencodedInteger(3)),
        });

        var encoded = Bencode.Encode(dict);

        CollectionAssert.AreEqual(Bytes("d5:alphai2e5:mangoi3e5:zebrai1ee"), encoded);
    }

}

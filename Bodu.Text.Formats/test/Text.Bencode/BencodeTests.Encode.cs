// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeTests.Encode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

public sealed partial class BencodeTests
{

    /// <summary>
    /// Verifies that <see cref="Bencode.Encode(BencodedValue)" /> produces the canonical encoded form of an
    /// integer value.
    /// </summary>
    [TestMethod]
    public void Encode_WhenBencodedInteger_ShouldReturnCanonicalEncoding()
    {
        byte[] actual = Bencode.Encode(new BencodedInteger(42));

        CollectionAssert.AreEqual(CanonicalIntegerBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.Encode(BencodedValue)" /> produces the canonical encoded form of a UTF-8
    /// string.
    /// </summary>
    [TestMethod]
    public void Encode_WhenBencodedString_ShouldReturnCanonicalEncoding()
    {
        byte[] actual = Bencode.Encode(BencodedString.FromUtf8("spam"));

        CollectionAssert.AreEqual(CanonicalSpamBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.Encode(BencodedValue)" /> emits dictionary keys in canonical raw byte
    /// order regardless of the order in which the keys were supplied to the constructor.
    /// </summary>
    [TestMethod]
    public void Encode_WhenDictionaryWithUnsortedKeysOnConstruction_ShouldEmitSortedKeys()
    {
        // Construct with "spam" before "cow" — the dictionary stores keys sorted, so encode order is canonical.
        BencodedDictionary dict = new(new[]
        {
            new KeyValuePair<BencodedString, BencodedValue>(BencodedString.FromUtf8("spam"), BencodedString.FromUtf8("eggs")),
            new KeyValuePair<BencodedString, BencodedValue>(BencodedString.FromUtf8("cow"), BencodedString.FromUtf8("moo")),
        });

        byte[] actual = Bencode.Encode(dict);

        CollectionAssert.AreEqual(Bytes("d3:cow3:moo4:spam4:eggse"), actual);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.Encode(BencodedValue)" /> emits <c>de</c> for an empty dictionary.
    /// </summary>
    [TestMethod]
    public void Encode_WhenEmptyBencodedDictionary_ShouldReturnEmptyDictionaryEncoding()
    {
        byte[] actual = Bencode.Encode(new BencodedDictionary(Array.Empty<KeyValuePair<BencodedString, BencodedValue>>()));

        CollectionAssert.AreEqual(CanonicalEmptyDictBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.Encode(BencodedValue)" /> emits <c>le</c> for an empty list.
    /// </summary>
    [TestMethod]
    public void Encode_WhenEmptyBencodedList_ShouldReturnEmptyListEncoding()
    {
        byte[] actual = Bencode.Encode(new BencodedList(Array.Empty<BencodedValue>()));

        CollectionAssert.AreEqual(CanonicalEmptyListBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.Encode(BencodedValue)" /> emits the canonical form of a nested list of
    /// heterogeneous values.
    /// </summary>
    [TestMethod]
    public void Encode_WhenListWithStringAndInteger_ShouldReturnCanonicalEncoding()
    {
        BencodedList list = new(new BencodedValue[]
        {
            BencodedString.FromUtf8("spam"),
            new BencodedInteger(42),
        });

        byte[] actual = Bencode.Encode(list);

        CollectionAssert.AreEqual(Bytes("l4:spami42ee"), actual);
    }

}

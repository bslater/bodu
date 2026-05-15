// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeTests.GetEncodedLength.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats;

public sealed partial class BencodeTests
{

    /// <summary>
    /// Verifies that <see cref="Bencode.GetEncodedLength(BencodedValue)" /> reports the exact byte count for a
    /// canonical integer value.
    /// </summary>
    [TestMethod]
    public void GetEncodedLength_WhenBencodedInteger_ShouldReturnExactByteCount()
    {
        Assert.AreEqual(CanonicalIntegerBytes.Length, Bencode.GetEncodedLength(new BencodedInteger(42)));
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.GetEncodedLength(BencodedValue)" /> reports the exact byte count for a
    /// UTF-8 string value.
    /// </summary>
    [TestMethod]
    public void GetEncodedLength_WhenBencodedString_ShouldReturnExactByteCount()
    {
        Assert.AreEqual(CanonicalSpamBytes.Length, Bencode.GetEncodedLength(BencodedString.FromUtf8("spam")));
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.GetEncodedLength(BencodedValue)" /> reports 2 bytes for an empty
    /// dictionary.
    /// </summary>
    [TestMethod]
    public void GetEncodedLength_WhenEmptyDictionary_ShouldReturnTwo()
    {
        Assert.AreEqual(2, Bencode.GetEncodedLength(new BencodedDictionary(Array.Empty<KeyValuePair<BencodedString, BencodedValue>>())));
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.GetEncodedLength(BencodedValue)" /> reports 2 bytes for an empty list.
    /// </summary>
    [TestMethod]
    public void GetEncodedLength_WhenEmptyList_ShouldReturnTwo()
    {
        Assert.AreEqual(2, Bencode.GetEncodedLength(new BencodedList(Array.Empty<BencodedValue>())));
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.GetEncodedLength(BencodedValue)" /> reports the exact byte count for an
    /// integer at <see cref="long.MaxValue" />.
    /// </summary>
    [TestMethod]
    public void GetEncodedLength_WhenInt64MaxValue_ShouldReturnExactByteCount()
    {
        // "i" + 19 digits + "e" = 21 bytes.
        Assert.AreEqual(21, Bencode.GetEncodedLength(new BencodedInteger(long.MaxValue)));
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.GetEncodedLength(BencodedValue)" /> reports the exact byte count for an
    /// integer at <see cref="long.MinValue" />.
    /// </summary>
    [TestMethod]
    public void GetEncodedLength_WhenInt64MinValue_ShouldReturnExactByteCount()
    {
        // "i" + "-" + 19 digits + "e" = 22 bytes.
        Assert.AreEqual(22, Bencode.GetEncodedLength(new BencodedInteger(long.MinValue)));
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.GetEncodedLength(BencodedValue)" /> sums child sizes correctly for a
    /// non-empty list.
    /// </summary>
    [TestMethod]
    public void GetEncodedLength_WhenListOfStringAndInteger_ShouldSumChildSizes()
    {
        BencodedList list = new(new BencodedValue[]
        {
            BencodedString.FromUtf8("spam"),
            new BencodedInteger(42),
        });

        Assert.AreEqual(Bytes("l4:spami42ee").Length, Bencode.GetEncodedLength(list));
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.GetEncodedLength(BencodedValue)" /> reports the exact byte count for a
    /// negative integer value (the sign character adds one byte).
    /// </summary>
    [TestMethod]
    public void GetEncodedLength_WhenNegativeBencodedInteger_ShouldIncludeSignCharacter()
    {
        Assert.AreEqual(5, Bencode.GetEncodedLength(new BencodedInteger(-42)));
    }

}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3CoreTests.StoreChainingValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class Blake3CoreTests
{
    /// <summary>
    /// Verifies that a chaining value is encoded as its eight words, little-endian, in order.
    /// </summary>
    [TestMethod]
    public void StoreChainingValue_WhenCalled_ShouldWriteEightLittleEndianWords()
    {
        uint[] chainingValue = [0x03020100U, 0x07060504U, 0x0B0A0908U, 0x0F0E0D0CU, 0x13121110U, 0x17161514U, 0x1B1A1918U, 0x1F1E1D1CU];
        byte[] destination = new byte[Blake3Core.ChainingValueBytes + 1];

        Blake3Core.StoreChainingValue(chainingValue, destination);

        for (int i = 0; i < Blake3Core.ChainingValueBytes; i++)
            Assert.AreEqual((byte)i, destination[i], $"byte {i}");

        Assert.AreEqual(0, destination[^1], "The byte past the encoding must be left alone.");
    }

    /// <summary>
    /// Verifies that a chaining value shorter than eight words is rejected with
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void StoreChainingValue_WhenChainingValueIsShort_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Blake3Core.StoreChainingValue(new uint[Blake3Core.ChainingValueWords - 1], new byte[Blake3Core.ChainingValueBytes]);
        });

        Assert.AreEqual("chainingValue", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a destination shorter than 32 bytes is rejected with <see cref="ArgumentOutOfRangeException" />
    /// rather than written past its end.
    /// </summary>
    [TestMethod]
    public void StoreChainingValue_WhenDestinationIsShort_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Blake3Core.StoreChainingValue(new uint[Blake3Core.ChainingValueWords], new byte[Blake3Core.ChainingValueBytes - 1]);
        });

        Assert.AreEqual("destination", ex.ParamName);
    }
}

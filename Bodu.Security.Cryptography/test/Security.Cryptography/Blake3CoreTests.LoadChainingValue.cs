// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3CoreTests.LoadChainingValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class Blake3CoreTests
{
    /// <summary>
    /// Verifies that 32 bytes decode as eight little-endian words, in order, the inverse of
    /// <see cref="Blake3Core.StoreChainingValue" />.
    /// </summary>
    [TestMethod]
    public void LoadChainingValue_WhenCalled_ShouldReadEightLittleEndianWords()
    {
        byte[] source = new byte[Blake3Core.ChainingValueBytes];
        for (int i = 0; i < source.Length; i++)
            source[i] = (byte)i;

        uint[] chainingValue = new uint[Blake3Core.ChainingValueWords + 1];
        Blake3Core.LoadChainingValue(source, chainingValue);

        CollectionAssert.AreEqual(
            new uint[] { 0x03020100U, 0x07060504U, 0x0B0A0908U, 0x0F0E0D0CU, 0x13121110U, 0x17161514U, 0x1B1A1918U, 0x1F1E1D1CU, 0U },
            chainingValue);
    }

    /// <summary>
    /// Verifies that a source shorter than 32 bytes is rejected with <see cref="ArgumentOutOfRangeException" /> rather
    /// than read past its end.
    /// </summary>
    [TestMethod]
    public void LoadChainingValue_WhenSourceIsShort_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Blake3Core.LoadChainingValue(new byte[Blake3Core.ChainingValueBytes - 1], new uint[Blake3Core.ChainingValueWords]);
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a chaining value shorter than eight words is rejected with
    /// <see cref="ArgumentOutOfRangeException" /> rather than written past.
    /// </summary>
    [TestMethod]
    public void LoadChainingValue_WhenChainingValueIsShort_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Blake3Core.LoadChainingValue(new byte[Blake3Core.ChainingValueBytes], new uint[Blake3Core.ChainingValueWords - 1]);
        });

        Assert.AreEqual("chainingValue", ex.ParamName);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.ConstantTimeSelect.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.ConstantTimeSelect" /> copies the <c>whenZero</c> operand when the
    /// difference accumulator is zero.
    /// </summary>
    [TestMethod]
    public void ConstantTimeSelect_WhenDifferenceIsZero_ShouldSelectWhenZeroOperand()
    {
        byte[] whenZero = [0x11, 0x22, 0x33, 0x44];
        byte[] whenNonZero = [0xAA, 0xBB, 0xCC, 0xDD];
        byte[] destination = new byte[4];

        CryptographyHelper.ConstantTimeSelect(0, whenZero, whenNonZero, destination);

        CollectionAssert.AreEqual(whenZero, destination);
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.ConstantTimeSelect" /> copies the <c>whenNonZero</c> operand for
    /// every non-zero accumulator shape — small, byte-sized, negative, and the extreme integer values — so the
    /// arithmetic mask derivation covers the full input domain.
    /// </summary>
    [TestMethod]
    [DataRow("one", 1)]
    [DataRow("byte", 0xFF)]
    [DataRow("negative", -1)]
    [DataRow("intMax", int.MaxValue)]
    [DataRow("intMin", int.MinValue)]
    public void ConstantTimeSelect_WhenDifferenceIsNonZero_ShouldSelectWhenNonZeroOperand(string testName, int difference)
    {
        byte[] whenZero = [0x11, 0x22, 0x33, 0x44];
        byte[] whenNonZero = [0xAA, 0xBB, 0xCC, 0xDD];
        byte[] destination = new byte[4];

        CryptographyHelper.ConstantTimeSelect(difference, whenZero, whenNonZero, destination);

        CollectionAssert.AreEqual(whenNonZero, destination,
            $"A {testName} accumulator must select the whenNonZero operand.");
    }

    /// <summary>
    /// Verifies that the selection composes with <see cref="CryptographyHelper.ConstantTimeDifference" /> the way the
    /// ML-KEM implicit-rejection path uses it: the shared secret is selected when the comparison matches and the
    /// rejection value when it does not.
    /// </summary>
    [TestMethod]
    public void ConstantTimeSelect_WhenComposedWithDifference_ShouldImplementCompareAndSelect()
    {
        byte[] expected = [0x01, 0x02, 0x03];
        byte[] secret = [0x5E, 0xC2, 0xE7];
        byte[] rejection = [0x4E, 0x0F, 0xE1];
        byte[] destination = new byte[3];

        CryptographyHelper.ConstantTimeSelect(
            CryptographyHelper.ConstantTimeDifference(expected, [0x01, 0x02, 0x03]), secret, rejection, destination);
        CollectionAssert.AreEqual(secret, destination, "A matching comparison must select the secret operand.");

        CryptographyHelper.ConstantTimeSelect(
            CryptographyHelper.ConstantTimeDifference(expected, [0x01, 0x02, 0x04]), secret, rejection, destination);
        CollectionAssert.AreEqual(rejection, destination, "A mismatching comparison must select the rejection operand.");
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.ConstantTimeSelect" /> throws <see cref="ArgumentException" />
    /// when either source operand or the destination differs in length from the <c>whenZero</c> operand.
    /// </summary>
    [TestMethod]
    public void ConstantTimeSelect_WhenLengthsDiffer_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            CryptographyHelper.ConstantTimeSelect(0, new byte[4], new byte[3], new byte[4]);
        });

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            CryptographyHelper.ConstantTimeSelect(0, new byte[4], new byte[4], new byte[3]);
        });
    }
}

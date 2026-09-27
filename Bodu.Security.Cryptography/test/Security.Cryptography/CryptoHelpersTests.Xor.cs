// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.Xor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.Xor" /> matches a byte-at-a-time exclusive OR at every length from 0
    /// to 100, which crosses each vector width's boundary and leaves every possible tail, and at every misalignment of
    /// the three spans within an array.
    /// </summary>
    [TestMethod]
    public void Xor_WhenLengthAndAlignmentVary_ShouldMatchByteAtATimeResult()
    {
        var random = new Random(0x0A1B);
        byte[] left = new byte[140];
        byte[] right = new byte[140];
        random.NextBytes(left);
        random.NextBytes(right);

        for (int length = 0; length <= 100; length++)
        {
            for (int misalignment = 0; misalignment < 8; misalignment++)
            {
                ReadOnlySpan<byte> x = left.AsSpan(misalignment, length);
                ReadOnlySpan<byte> y = right.AsSpan((misalignment * 3) % 8, length);
                byte[] expected = new byte[length];
                for (int i = 0; i < length; i++)
                    expected[i] = (byte)(x[i] ^ y[i]);

                byte[] destination = new byte[length + 16];
                CryptographyHelper.Xor(x, y, destination.AsSpan((misalignment * 5) % 8, length));

                CollectionAssert.AreEqual(expected, destination.AsSpan((misalignment * 5) % 8, length).ToArray(), $"length {length}, misalignment {misalignment}");
            }
        }
    }

    /// <summary>
    /// Verifies that the destination may be the same memory as either operand, which is how the counter modes apply
    /// keystream in place.
    /// </summary>
    [TestMethod]
    public void Xor_WhenDestinationIsAnOperand_ShouldWriteTheResultInPlace()
    {
        byte[] left = Enumerable.Range(0, 77).Select(i => (byte)(i * 7)).ToArray();
        byte[] right = Enumerable.Range(0, 77).Select(i => (byte)(i * 13 + 1)).ToArray();
        byte[] expected = left.Zip(right, (a, b) => (byte)(a ^ b)).ToArray();

        byte[] intoLeft = (byte[])left.Clone();
        CryptographyHelper.Xor(intoLeft, right, intoLeft);
        byte[] intoRight = (byte[])right.Clone();
        CryptographyHelper.Xor(left, intoRight, intoRight);

        CollectionAssert.AreEqual(expected, intoLeft);
        CollectionAssert.AreEqual(expected, intoRight);
    }

    /// <summary>
    /// Verifies that a second operand or destination shorter than the first operand is rejected with
    /// <see cref="ArgumentException" /> before anything is written.
    /// </summary>
    [TestMethod]
    public void Xor_WhenASpanIsShorterThanTheFirstOperand_ShouldThrowArgumentException()
    {
        byte[] destination = new byte[32];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            CryptographyHelper.Xor(new byte[33], new byte[32], new byte[33]);
        });
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            CryptographyHelper.Xor(new byte[33], new byte[33], destination);
        });
        CollectionAssert.AreEqual(new byte[32], destination);
    }
}

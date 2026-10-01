// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519PointTests.ScalarMultBase.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Security.Cryptography;

public partial class Ed25519PointTests
{
    /// <summary>
    /// Verifies that the precomputed fixed-base <see cref="Ed25519Point.ScalarMultBase" /> agrees with the general
    /// reference <see cref="Ed25519Point.ScalarMult" /> over the base point for boundary and pseudo-random scalars.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void ScalarMultBase_ForVariousScalars_ShouldMatchReferenceScalarMult()
    {
        var random = new Random(8032);
        byte[] scalar = new byte[32];

        for (int iteration = 0; iteration < 256; iteration++)
        {
            SetScalar(scalar, iteration, random);

            byte[] expected = new byte[Ed25519Point.EncodedSizeInBytes];
            byte[] actual = new byte[Ed25519Point.EncodedSizeInBytes];
            Ed25519Point.ScalarMult(Ed25519Point.BasePoint, scalar).Encode(expected);
            Ed25519Point.ScalarMultBase(scalar).Encode(actual);

            CollectionAssert.AreEqual(expected, actual, $"iteration {iteration}");
        }
    }

    /// <summary>
    /// Verifies that <see cref="Ed25519Point.ScalarMultBase" /> matches the ladder and the replaced 1.1.0 table for
    /// scalars that give every window each signed radix-16 digit from −8 to 7, and for scalars at the bounds of the
    /// recoding, whose top digit carries past 2^256 or does not.
    /// </summary>
    [TestMethod]
    public void ScalarMultBase_WhenScalarsTakeEverySignedDigitInEveryWindow_ShouldMatchTheLadderAndTheReplacedTable()
    {
        foreach (byte[] scalar in SignedDigitScalars())
        {
            string name = Convert.ToHexString(scalar);
            byte[] expected = Encoded(Ed25519Point.ScalarMult(Ed25519Point.BasePoint, scalar));

            CollectionAssert.AreEqual(expected, Encoded(Ed25519PointReference.ScalarMultBase(scalar)), $"reference, {name}");
            CollectionAssert.AreEqual(expected, Encoded(Ed25519Point.ScalarMultBase(scalar)), name);
        }
    }

    /// <summary>
    /// Verifies that <see cref="Ed25519Point.ScalarMultBase" /> matches the replaced 1.1.0 table for seeded scalars
    /// of every shape its callers pass: unreduced, clamped as a key is, and reduced modulo the group order as a
    /// signing nonce is.
    /// </summary>
    [TestMethod]
    public void ScalarMultBase_WhenScalarsAreSeededClampedOrReduced_ShouldMatchTheReplacedTable()
    {
        var random = new Random(0x2551_9110);
        byte[] scalar = new byte[32];
        byte[] wide = new byte[64];

        for (int iteration = 0; iteration < 96; iteration++)
        {
            switch (iteration % 3)
            {
                case 0:
                    random.NextBytes(scalar);
                    break;
                case 1:
                    random.NextBytes(scalar);
                    scalar[0] &= 248;
                    scalar[31] &= 127;
                    scalar[31] |= 64;
                    break;
                default:
                    random.NextBytes(wide);
                    Ed25519Scalar.Reduce(wide, scalar);
                    break;
            }

            CollectionAssert.AreEqual(
                Encoded(Ed25519PointReference.ScalarMultBase(scalar)),
                Encoded(Ed25519Point.ScalarMultBase(scalar)),
                $"iteration {iteration}");
        }
    }

    /// <summary>
    /// Verifies that <see cref="Ed25519Point.ScalarMultBase" /> throws <see cref="ArgumentException" /> naming the
    /// scalar when it is not exactly 32 bytes.
    /// </summary>
    /// <param name="length">The scalar's length.</param>
    [TestMethod]
    [DataRow(31)]
    [DataRow(33)]
    public void ScalarMultBase_WhenScalarLengthIsInvalid_ShouldThrowArgumentException(int length)
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Ed25519Point.ScalarMultBase(new byte[length]);
        });

        Assert.AreEqual("scalar", ex.ParamName);
    }

    /// <summary>
    /// Yields scalars that give every window each signed radix-16 digit, and scalars at the bounds of the recoding.
    /// </summary>
    /// <returns>The scalars.</returns>
    /// <remarks>
    /// <para>
    /// A byte of <c>n</c> in every position gives each even window n, or n − 16 from 8 up, since its odd neighbor never
    /// carries; a byte of <c>n · 16</c> gives each odd window the same. Together they give every window every digit
    /// from −8 to 7. From 8 up, the second pattern's top digit also carries past 2^256.
    /// </para>
    /// <para>
    /// The bounds: zero, one, the group order and its neighbors, 2^255 and its predecessor, 2^256 − 1, and a scalar
    /// below 2^255 whose top digit carries all the same, since a carry rises into its top nibble of 7.
    /// </para>
    /// </remarks>
    private static IEnumerable<byte[]> SignedDigitScalars()
    {
        for (int n = 0; n < 16; n++)
        {
            yield return Enumerable.Repeat((byte)n, 32).ToArray();
            yield return Enumerable.Repeat((byte)(n << 4), 32).ToArray();
        }

        BigInteger order = ScalarValue(s_groupOrder);
        yield return ScalarBytes(BigInteger.Zero);
        yield return ScalarBytes(BigInteger.One);
        yield return ScalarBytes(order - 1);
        yield return ScalarBytes(order);
        yield return ScalarBytes(order + 1);
        yield return ScalarBytes((BigInteger.One << 255) - 1);
        yield return ScalarBytes(BigInteger.One << 255);
        yield return ScalarBytes((BigInteger.One << 256) - 1);
        yield return ScalarBytes((BigInteger.One << 255) - (BigInteger.One << 251) - 1);
    }
}

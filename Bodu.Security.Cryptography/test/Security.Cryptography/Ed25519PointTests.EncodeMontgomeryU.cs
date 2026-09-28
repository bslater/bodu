// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519PointTests.EncodeMontgomeryU.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Ed25519PointTests
{
    /// <summary>
    /// Verifies that the Ed25519 base point maps to the X25519 base point, u = 9.
    /// </summary>
    [TestMethod]
    public void EncodeMontgomeryU_WhenPointIsTheBasePoint_ShouldEncodeNine()
    {
        byte[] encoded = new byte[Ed25519Point.EncodedSizeInBytes];

        Ed25519Point.BasePoint.EncodeMontgomeryU(encoded);

        CollectionAssert.AreEqual(Convert.FromHexString(MontgomeryNineHex), encoded);
    }

    /// <summary>
    /// Verifies that the identity, which has no image under the map, encodes as zero.
    /// </summary>
    [TestMethod]
    public void EncodeMontgomeryU_WhenPointIsTheIdentity_ShouldEncodeZero()
    {
        byte[] encoded = new byte[Ed25519Point.EncodedSizeInBytes];
        encoded.AsSpan().Fill(0xAA);

        Ed25519Point.Identity.EncodeMontgomeryU(encoded);

        CollectionAssert.AreEqual(new byte[Ed25519Point.EncodedSizeInBytes], encoded);
    }

    /// <summary>
    /// Verifies that the image of a seeded multiple [k]B, for a clamped scalar k, is the u-coordinate X25519's ladder
    /// computes for k and u = 9, so the map respects scalar multiplication.
    /// </summary>
    [TestMethod]
    public void EncodeMontgomeryU_WhenPointsAreSeededMultiples_ShouldMatchTheLadder()
    {
        var random = new Random(0x2551_9103);
        byte[] scalar = new byte[32];
        byte[] nine = Convert.FromHexString(MontgomeryNineHex);
        byte[] expected = new byte[Curve25519.PointSizeInBytes];
        byte[] actual = new byte[Ed25519Point.EncodedSizeInBytes];

        for (int iteration = 0; iteration < 64; iteration++)
        {
            random.NextBytes(scalar);
            scalar[0] &= 248;
            scalar[31] &= 127;
            scalar[31] |= 64;

            _ = Curve25519.ScalarMult(scalar, nine, expected);
            Ed25519Point.ScalarMultBase(scalar).EncodeMontgomeryU(actual);

            CollectionAssert.AreEqual(expected, actual, $"iteration {iteration}");
        }
    }

    /// <summary>
    /// Verifies that <see cref="Ed25519Point.EncodeMontgomeryU" /> throws <see cref="ArgumentException" /> naming the
    /// destination when it is not exactly 32 bytes.
    /// </summary>
    [TestMethod]
    public void EncodeMontgomeryU_WhenDestinationLengthIsInvalid_ShouldThrowArgumentException()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            Ed25519Point.BasePoint.EncodeMontgomeryU(new byte[31]);
        });

        Assert.AreEqual("destination", ex.ParamName);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519PointTests.AreEqual.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Ed25519PointTests
{
    /// <summary>
    /// Verifies that the same point reached along two routes, with different projective Z coordinates, compares equal:
    /// a seeded multiple of the base point from the fixed-base table and from the reference ladder, and a doubled point
    /// and its self-addition.
    /// </summary>
    [TestMethod]
    public void AreEqual_WhenPointsAreTheSameInDifferentCoordinates_ShouldReturnTrue()
    {
        var random = new Random(0x2551_9203);
        byte[] scalar = new byte[32];

        for (int iteration = 0; iteration < 32; iteration++)
        {
            random.NextBytes(scalar);
            Ed25519Point viaTable = Ed25519Point.ScalarMultBase(scalar);
            Ed25519Point viaLadder = Ed25519Point.ScalarMult(Ed25519Point.BasePoint, scalar);

            Assert.IsTrue(Ed25519Point.AreEqual(viaTable, viaLadder), $"iteration {iteration}: table and ladder");
            Assert.IsTrue(Ed25519Point.AreEqual(viaTable.Double(), viaTable.Add(viaTable)), $"iteration {iteration}: double");
        }
    }

    /// <summary>
    /// Verifies that a point and its negation, which share y and differ only in the sign of x, compare unequal, so
    /// the comparison checks x as well as y.
    /// </summary>
    [TestMethod]
    public void AreEqual_WhenPointsDifferOnlyInTheSignOfX_ShouldReturnFalse()
    {
        var random = new Random(0x2551_9204);
        byte[] scalar = new byte[32];

        for (int iteration = 0; iteration < 32; iteration++)
        {
            random.NextBytes(scalar);
            Ed25519Point point = Ed25519Point.ScalarMultBase(scalar);

            Assert.IsFalse(Ed25519Point.AreEqual(point, point.Negate()), $"iteration {iteration}");
        }
    }

    /// <summary>
    /// Verifies that the identity (0, 1) and the point of order 2, (0, −1), which share x = 0, compare unequal, so the
    /// comparison checks y as well as x.
    /// </summary>
    [TestMethod]
    public void AreEqual_WhenPointsDifferOnlyInY_ShouldReturnFalse()
    {
        Assert.IsTrue(Ed25519Point.TryDecode(
            Convert.FromHexString("ecffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f"), out Ed25519Point orderTwo));

        Assert.IsFalse(Ed25519Point.AreEqual(Ed25519Point.Identity, orderTwo));
    }

    /// <summary>
    /// Verifies that seeded multiples of the base point compare unequal to the next multiple.
    /// </summary>
    [TestMethod]
    public void AreEqual_WhenPointsDiffer_ShouldReturnFalse()
    {
        var random = new Random(0x2551_9205);
        byte[] scalar = new byte[32];

        for (int iteration = 0; iteration < 32; iteration++)
        {
            random.NextBytes(scalar);
            Ed25519Point point = Ed25519Point.ScalarMultBase(scalar);

            Assert.IsFalse(Ed25519Point.AreEqual(point, point.Add(Ed25519Point.BasePoint)), $"iteration {iteration}");
        }
    }
}

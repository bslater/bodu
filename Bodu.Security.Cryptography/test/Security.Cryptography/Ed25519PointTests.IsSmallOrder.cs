// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519PointTests.IsSmallOrder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Ed25519PointTests
{
    /// <summary>
    /// Verifies that each of the eight points whose order divides the cofactor is recognized as small order.
    /// </summary>
    /// <param name="testName">The row's name.</param>
    /// <param name="encodedHex">The point's canonical encoding.</param>
    [TestMethod]
    [DataRow("order 1 (identity)", "0100000000000000000000000000000000000000000000000000000000000000")]
    [DataRow("order 2", "ecffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f")]
    [DataRow("order 4 (x positive)", "0000000000000000000000000000000000000000000000000000000000000000")]
    [DataRow("order 4 (x negative)", "0000000000000000000000000000000000000000000000000000000000000080")]
    [DataRow("order 8 (a)", "26e8958fc2b227b045c3f489f2ef98f0d5dfac05d3c63339b13802886d53fc05")]
    [DataRow("order 8 (b)", "c7176a703d4dd84fba3c0b760d10670f2a2053fa2c39ccc64ec7fd7792ac037a")]
    [DataRow("order 8 (c)", "26e8958fc2b227b045c3f489f2ef98f0d5dfac05d3c63339b13802886d53fc85")]
    [DataRow("order 8 (d)", "c7176a703d4dd84fba3c0b760d10670f2a2053fa2c39ccc64ec7fd7792ac03fa")]
    public void IsSmallOrder_WhenPointHasSmallOrder_ShouldReturnTrue(string testName, string encodedHex)
    {
        Assert.IsTrue(Ed25519Point.TryDecode(Convert.FromHexString(encodedHex), out Ed25519Point point), testName);

        Assert.IsTrue(point.IsSmallOrder(), testName);
    }

    /// <summary>
    /// Verifies that the base point and seeded multiples of it, which lie in the prime-order subgroup, are not
    /// recognized as small order.
    /// </summary>
    [TestMethod]
    public void IsSmallOrder_WhenPointIsInThePrimeOrderSubgroup_ShouldReturnFalse()
    {
        var random = new Random(0x2551_9201);
        byte[] scalar = new byte[32];

        Assert.IsFalse(Ed25519Point.BasePoint.IsSmallOrder(), "base point");

        for (int iteration = 0; iteration < 32; iteration++)
        {
            random.NextBytes(scalar);
            scalar[0] |= 1;

            Assert.IsFalse(Ed25519Point.ScalarMultBase(scalar).IsSmallOrder(), $"iteration {iteration}");
        }
    }

    /// <summary>
    /// Verifies that a point of mixed order, a multiple of the base point plus a point of order 8, is not recognized as
    /// small order: its multiple by 8 is the multiple of the base point, not the identity.
    /// </summary>
    [TestMethod]
    public void IsSmallOrder_WhenPointHasMixedOrder_ShouldReturnFalse()
    {
        Assert.IsTrue(Ed25519Point.TryDecode(
            Convert.FromHexString("26e8958fc2b227b045c3f489f2ef98f0d5dfac05d3c63339b13802886d53fc05"), out Ed25519Point torsion));
        var random = new Random(0x2551_9202);
        byte[] scalar = new byte[32];

        for (int iteration = 0; iteration < 32; iteration++)
        {
            random.NextBytes(scalar);
            scalar[0] |= 1;

            Assert.IsFalse(Ed25519Point.ScalarMultBase(scalar).Add(torsion).IsSmallOrder(), $"iteration {iteration}");
        }
    }
}

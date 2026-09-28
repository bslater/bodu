// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519PointTests.Double.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Ed25519PointTests
{
    /// <summary>
    /// Verifies that doubling the base point matches adding it to itself through an independently constructed copy.
    /// </summary>
    [TestMethod]
    public void Double_WhenComparedToSelfAddition_ShouldProduceSameResult()
    {
        byte[] doubled = new byte[Ed25519Point.EncodedSizeInBytes];
        byte[] added = new byte[Ed25519Point.EncodedSizeInBytes];

        Ed25519Point.BasePoint.Double().Encode(doubled);
        Ed25519Point.BasePoint.Add(Ed25519Point.BasePoint).Encode(added);

        CollectionAssert.AreEqual(doubled, added);
    }

    /// <summary>
    /// Verifies that doubling seeded multiples of the base point, held in projective coordinates with Z other than one,
    /// matches adding each point to itself.
    /// </summary>
    [TestMethod]
    public void Double_WhenPointsAreSeededMultiples_ShouldMatchSelfAddition()
    {
        var random = new Random(0x2551_9101);
        byte[] scalar = new byte[32];
        byte[] doubled = new byte[Ed25519Point.EncodedSizeInBytes];
        byte[] added = new byte[Ed25519Point.EncodedSizeInBytes];

        for (int iteration = 0; iteration < 64; iteration++)
        {
            random.NextBytes(scalar);
            Ed25519Point point = Ed25519Point.ScalarMultBase(scalar);

            point.Double().Encode(doubled);
            point.Add(point).Encode(added);

            CollectionAssert.AreEqual(added, doubled, $"iteration {iteration}");
        }
    }

    /// <summary>
    /// Verifies that a doubled point carries a consistent extended coordinate T, which doubling itself never reads:
    /// adding a further point to it, which does read T, matches adding that point to the self-addition.
    /// </summary>
    [TestMethod]
    public void Double_WhenFollowedByAnAddition_ShouldMatchSelfAdditionFollowedByTheSameAddition()
    {
        var random = new Random(0x2551_9102);
        byte[] scalar = new byte[32];
        byte[] viaDouble = new byte[Ed25519Point.EncodedSizeInBytes];
        byte[] viaAdd = new byte[Ed25519Point.EncodedSizeInBytes];

        for (int iteration = 0; iteration < 64; iteration++)
        {
            random.NextBytes(scalar);
            Ed25519Point point = Ed25519Point.ScalarMultBase(scalar);
            random.NextBytes(scalar);
            Ed25519Point other = Ed25519Point.ScalarMultBase(scalar);

            point.Double().Add(other).Encode(viaDouble);
            point.Add(point).Add(other).Encode(viaAdd);

            CollectionAssert.AreEqual(viaAdd, viaDouble, $"iteration {iteration}");
        }
    }

    /// <summary>
    /// Verifies that doubling each of the eight points of small order matches adding it to itself, and that a point
    /// added to the doubled point gives the same sum, including the identity and the points whose double is the
    /// identity.
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
    public void Double_WhenPointHasSmallOrder_ShouldMatchSelfAddition(string testName, string encodedHex)
    {
        Assert.IsTrue(Ed25519Point.TryDecode(Convert.FromHexString(encodedHex), out Ed25519Point point), testName);
        byte[] doubled = new byte[Ed25519Point.EncodedSizeInBytes];
        byte[] added = new byte[Ed25519Point.EncodedSizeInBytes];

        point.Double().Add(point).Encode(doubled);
        point.Add(point).Add(point).Encode(added);

        CollectionAssert.AreEqual(added, doubled, testName);
    }
}

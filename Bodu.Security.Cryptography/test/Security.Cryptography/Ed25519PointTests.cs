// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519PointTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="Ed25519Point" /> group operations and the RFC 8032 point codec.
/// </summary>
[TestClass]
public class Ed25519PointTests
{
    /// <summary>
    /// The canonical encoding of the Ed25519 base point.
    /// </summary>
    private const string BasePointHex = "5866666666666666666666666666666666666666666666666666666666666666";

    /// <summary>
    /// The canonical encoding of the identity element (0, 1).
    /// </summary>
    private const string IdentityHex = "0100000000000000000000000000000000000000000000000000000000000000";

    /// <summary>
    /// Verifies that <see cref="Ed25519Point.BasePoint" /> re-encodes to its canonical RFC 8032 encoding.
    /// </summary>
    [TestMethod]
    public void Encode_WhenEncodingBasePoint_ShouldProduceCanonicalEncoding()
    {
        var encoded = new byte[Ed25519Point.EncodedSizeInBytes];
        Ed25519Point.BasePoint.Encode(encoded);

        CollectionAssert.AreEqual(Convert.FromHexString(BasePointHex), encoded);
    }

    /// <summary>
    /// Verifies that adding the identity to the base point leaves it unchanged, and that adding the base point to
    /// its negation produces the identity.
    /// </summary>
    [TestMethod]
    public void Add_WhenUsingIdentityAndInverse_ShouldSatisfyGroupAxioms()
    {
        var sum = new byte[Ed25519Point.EncodedSizeInBytes];
        Ed25519Point.BasePoint.Add(Ed25519Point.Identity).Encode(sum);
        CollectionAssert.AreEqual(Convert.FromHexString(BasePointHex), sum);

        Ed25519Point.BasePoint.Add(Ed25519Point.BasePoint.Negate()).Encode(sum);
        CollectionAssert.AreEqual(Convert.FromHexString(IdentityHex), sum);
    }

    /// <summary>
    /// Verifies that doubling the base point matches adding it to itself through an independently constructed copy.
    /// </summary>
    [TestMethod]
    public void Double_WhenComparedToSelfAddition_ShouldProduceSameResult()
    {
        var doubled = new byte[Ed25519Point.EncodedSizeInBytes];
        var added = new byte[Ed25519Point.EncodedSizeInBytes];

        Ed25519Point.BasePoint.Double().Encode(doubled);
        Ed25519Point.BasePoint.Add(Ed25519Point.BasePoint).Encode(added);

        CollectionAssert.AreEqual(doubled, added);
    }

    /// <summary>
    /// Verifies that <see cref="Ed25519Point.ScalarMult" /> by 1 returns the point and that multiplying the base
    /// point by the group order L yields the identity.
    /// </summary>
    [TestMethod]
    public void ScalarMult_WhenScalarIsOneOrGroupOrder_ShouldReturnPointOrIdentity()
    {
        var one = new byte[32];
        one[0] = 1;
        var encoded = new byte[Ed25519Point.EncodedSizeInBytes];

        Ed25519Point.ScalarMult(Ed25519Point.BasePoint, one).Encode(encoded);
        CollectionAssert.AreEqual(Convert.FromHexString(BasePointHex), encoded);

        var order = Convert.FromHexString("edd3f55c1a631258d69cf7a2def9de1400000000000000000000000000000010");
        Ed25519Point.ScalarMult(Ed25519Point.BasePoint, order).Encode(encoded);
        CollectionAssert.AreEqual(Convert.FromHexString(IdentityHex), encoded);
    }

    /// <summary>
    /// Verifies that <see cref="Ed25519Point.TryDecode" /> round-trips the base point, and rejects a y value that is
    /// not on the curve, a non-canonical y at or above p, and the invalid x = 0 with sign bit 1 combination.
    /// </summary>
    [TestMethod]
    [DataRow("base point", BasePointHex, true)]
    [DataRow("identity", IdentityHex, true)]
    [DataRow("y=2 not on curve", "0200000000000000000000000000000000000000000000000000000000000000", false)]
    [DataRow("non-canonical y=p", "edffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f", false)]
    [DataRow("non-canonical y=p+1", "eeffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f", false)]
    [DataRow("x=0 with sign bit set", "0100000000000000000000000000000000000000000000000000000000000080", false)]
    public void TryDecode_WhenGivenEncoding_ShouldAcceptOrRejectPerRfc8032(string testName, string encodedHex, bool expectedValid)
    {
        _ = testName;

        var encoded = Convert.FromHexString(encodedHex);
        var valid = Ed25519Point.TryDecode(encoded, out Ed25519Point point);

        Assert.AreEqual(expectedValid, valid);

        if (expectedValid)
        {
            var roundTrip = new byte[Ed25519Point.EncodedSizeInBytes];
            point.Encode(roundTrip);
            CollectionAssert.AreEqual(encoded, roundTrip);
        }
    }
}

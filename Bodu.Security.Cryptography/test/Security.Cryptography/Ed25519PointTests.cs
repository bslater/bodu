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
        byte[] encoded = new byte[Ed25519Point.EncodedSizeInBytes];
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
        byte[] sum = new byte[Ed25519Point.EncodedSizeInBytes];
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
        byte[] doubled = new byte[Ed25519Point.EncodedSizeInBytes];
        byte[] added = new byte[Ed25519Point.EncodedSizeInBytes];

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
        byte[] one = new byte[32];
        one[0] = 1;
        byte[] encoded = new byte[Ed25519Point.EncodedSizeInBytes];

        Ed25519Point.ScalarMult(Ed25519Point.BasePoint, one).Encode(encoded);
        CollectionAssert.AreEqual(Convert.FromHexString(BasePointHex), encoded);

        byte[] order = Convert.FromHexString("edd3f55c1a631258d69cf7a2def9de1400000000000000000000000000000010");
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

        byte[] encoded = Convert.FromHexString(encodedHex);
        bool valid = Ed25519Point.TryDecode(encoded, out Ed25519Point point);

        Assert.AreEqual(expectedValid, valid);

        if (expectedValid)
        {
            byte[] roundTrip = new byte[Ed25519Point.EncodedSizeInBytes];
            point.Encode(roundTrip);
            CollectionAssert.AreEqual(encoded, roundTrip);
        }
    }

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
    /// Verifies that <see cref="Ed25519Point.DoubleScalarMultBaseVartime" /> equals the reference
    /// [a]B + [b]P computed with two separate scalar multiplications, over pseudo-random scalars and points.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void DoubleScalarMultBaseVartime_ForVariousInputs_ShouldMatchSeparateScalarMults()
    {
        var random = new Random(80322);
        byte[] a = new byte[32];
        byte[] b = new byte[32];
        byte[] pointScalar = new byte[32];

        for (int iteration = 0; iteration < 128; iteration++)
        {
            SetScalar(a, iteration, random);
            SetScalar(b, iteration + 1, random);
            random.NextBytes(pointScalar);

            // Derive an arbitrary prime-order point P = [pointScalar]B as the variable point.
            var point = Ed25519Point.ScalarMultBase(pointScalar);

            byte[] expected = new byte[Ed25519Point.EncodedSizeInBytes];
            byte[] actual = new byte[Ed25519Point.EncodedSizeInBytes];
            Ed25519Point.ScalarMultBase(a).Add(Ed25519Point.ScalarMult(point, b)).Encode(expected);
            Ed25519Point.DoubleScalarMultBaseVartime(a, b, point).Encode(actual);

            CollectionAssert.AreEqual(expected, actual, $"iteration {iteration}");
        }
    }

    /// <summary>
    /// Sets <paramref name="scalar" /> to a boundary value for the first few iterations and a pseudo-random value
    /// thereafter, to exercise the fixed-base and double-scalar routines at their edges.
    /// </summary>
    /// <param name="scalar">The 32-byte buffer to populate.</param>
    /// <param name="iteration">The iteration index selecting a boundary case.</param>
    /// <param name="random">The pseudo-random source for non-boundary iterations.</param>
    private static void SetScalar(byte[] scalar, int iteration, Random random)
    {
        Array.Clear(scalar);
        switch (iteration)
        {
            case 0:
                break; // all zero
            case 1:
                scalar[0] = 1;
                break;
            case 2:
                scalar[0] = 2;
                break;
            case 3:
                Array.Fill(scalar, (byte)0xFF);
                break;
            default:
                random.NextBytes(scalar);
                break;
        }
    }
}

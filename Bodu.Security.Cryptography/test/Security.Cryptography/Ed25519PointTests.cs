// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519PointTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="Ed25519Point" /> group operations and the RFC 8032 point codec.
/// </summary>
[TestClass]
public partial class Ed25519PointTests
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
    /// The canonical encoding of the base point's image on Curve25519, the X25519 base point u = 9.
    /// </summary>
    private const string MontgomeryNineHex = "0900000000000000000000000000000000000000000000000000000000000000";

    /// <summary>
    /// The canonical encodings of the eight points of small order: the identity, the point of order 2, the two of
    /// order 4 and the four of order 8.
    /// </summary>
    private static readonly string[] s_smallOrderEncodings =
    [
        "0100000000000000000000000000000000000000000000000000000000000000",
        "ecffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f",
        "0000000000000000000000000000000000000000000000000000000000000000",
        "0000000000000000000000000000000000000000000000000000000000000080",
        "26e8958fc2b227b045c3f489f2ef98f0d5dfac05d3c63339b13802886d53fc05",
        "c7176a703d4dd84fba3c0b760d10670f2a2053fa2c39ccc64ec7fd7792ac037a",
        "26e8958fc2b227b045c3f489f2ef98f0d5dfac05d3c63339b13802886d53fc85",
        "c7176a703d4dd84fba3c0b760d10670f2a2053fa2c39ccc64ec7fd7792ac03fa",
    ];

    /// <summary>
    /// The group order L = 2^252 + 27742317777372353535851937790883648493, little-endian.
    /// </summary>
    private static readonly byte[] s_groupOrder =
        Convert.FromHexString("edd3f55c1a631258d69cf7a2def9de1400000000000000000000000000000010");

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
    /// Returns the canonical encoding of a point.
    /// </summary>
    /// <param name="point">The point to encode.</param>
    /// <returns>The 32-byte RFC 8032 encoding.</returns>
    private static byte[] Encoded(in Ed25519Point point)
    {
        byte[] encoded = new byte[Ed25519Point.EncodedSizeInBytes];
        point.Encode(encoded);

        return encoded;
    }

    /// <summary>
    /// Decodes a canonical point encoding given in hex, failing the test when it does not decode.
    /// </summary>
    /// <param name="hex">The encoding, in hex.</param>
    /// <returns>The decoded point.</returns>
    private static Ed25519Point Decoded(string hex)
    {
        Assert.IsTrue(Ed25519Point.TryDecode(Convert.FromHexString(hex), out Ed25519Point point), hex);

        return point;
    }

    /// <summary>
    /// Returns the scalar <paramref name="value" /> as a 32-byte little-endian encoding, reduced modulo 2^256.
    /// </summary>
    /// <param name="value">The scalar's value.</param>
    /// <returns>The 32-byte encoding.</returns>
    private static byte[] ScalarBytes(BigInteger value)
    {
        byte[] bytes = new byte[32];
        BigInteger reduced = value & ((BigInteger.One << 256) - 1);
        _ = reduced.TryWriteBytes(bytes, out _, isUnsigned: true, isBigEndian: false);

        return bytes;
    }

    /// <summary>
    /// Returns the value of a little-endian scalar.
    /// </summary>
    /// <param name="scalar">The little-endian scalar.</param>
    /// <returns>The scalar's value.</returns>
    private static BigInteger ScalarValue(ReadOnlySpan<byte> scalar) =>
        new(scalar, isUnsigned: true, isBigEndian: false);

    /// <summary>
    /// Yields triples of points to add: seeded multiples of the base point, each point of small order added to a
    /// seeded multiple and a seeded multiple added to it, and the identity on either side.
    /// </summary>
    /// <param name="seed">The seed of the multiples.</param>
    /// <returns>The triples, each named: the two points to add, and a third to add to their sum.</returns>
    private static IEnumerable<(string Name, Ed25519Point Point, Ed25519Point Other, Ed25519Point Further)> PointTriples(int seed)
    {
        var random = new Random(seed);
        byte[] scalar = new byte[32];

        Ed25519Point Next()
        {
            random.NextBytes(scalar);
            return Ed25519Point.ScalarMultBase(scalar);
        }

        for (int iteration = 0; iteration < 32; iteration++)
            yield return ($"seeded {iteration}", Next(), Next(), Next());

        foreach (string encoding in s_smallOrderEncodings)
        {
            Ed25519Point smallOrder = Decoded(encoding);
            yield return ($"{encoding} on the right", Next(), smallOrder, Next());
            yield return ($"{encoding} on the left", smallOrder, Next(), Next());
            yield return ($"{encoding} with itself", smallOrder, smallOrder, Next());
        }

        yield return ("identity on the right", Next(), Ed25519Point.Identity, Next());
        yield return ("identity on the left", Ed25519Point.Identity, Next(), Next());
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

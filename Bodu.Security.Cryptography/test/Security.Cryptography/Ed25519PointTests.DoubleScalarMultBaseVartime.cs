// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519PointTests.DoubleScalarMultBaseVartime.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Security.Cryptography;

public partial class Ed25519PointTests
{
    /// <summary>
    /// Verifies that <see cref="Ed25519Point.DoubleScalarMultBaseVartime" /> equals the reference
    /// [a]B + [b]P computed with two separate ladders, over pseudo-random scalars and points.
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
            Ed25519Point.ScalarMult(Ed25519Point.BasePoint, a).Add(Ed25519Point.ScalarMult(point, b)).Encode(expected);
            Ed25519Point.DoubleScalarMultBaseVartime(a, b, point).Encode(actual);

            CollectionAssert.AreEqual(expected, actual, $"iteration {iteration}");
        }
    }

    /// <summary>
    /// Verifies that <see cref="Ed25519Point.DoubleScalarMultBaseVartime" /> matches the replaced 1.1.0 routine and
    /// two separate ladders when each scalar is zero, one, next to the group order, next to 2^255 or 2^256 − 1, and
    /// the point is a multiple of B, a decoded and negated point as verification passes, a point with a component of
    /// small order, or the identity.
    /// </summary>
    [TestMethod]
    public void DoubleScalarMultBaseVartime_WhenScalarsAreAtTheirBounds_ShouldMatchTheReplacedRoutineAndSeparateLadders()
    {
        BigInteger order = ScalarValue(s_groupOrder);
        byte[][] scalars =
        [
            ScalarBytes(BigInteger.Zero),
            ScalarBytes(BigInteger.One),
            ScalarBytes(order - 1),
            ScalarBytes((BigInteger.One << 255) - 1),
            ScalarBytes(BigInteger.One << 255),
            ScalarBytes((BigInteger.One << 256) - 1),
        ];

        Ed25519Point multiple = Ed25519Point.ScalarMultBase(ScalarBytes(0x2551_9113));
        Ed25519Point[] points =
        [
            multiple,
            Decoded(Convert.ToHexString(Encoded(multiple))).Negate(),
            multiple.Add(Decoded(s_smallOrderEncodings[4])),
            Ed25519Point.Identity,
        ];

        for (int p = 0; p < points.Length; p++)
        {
            foreach (byte[] a in scalars)
            {
                foreach (byte[] b in scalars)
                {
                    string name = $"point {p}, a {Convert.ToHexString(a)}, b {Convert.ToHexString(b)}";
                    byte[] expected = Encoded(
                        Ed25519Point.ScalarMult(Ed25519Point.BasePoint, a).Add(Ed25519Point.ScalarMult(points[p], b)));

                    CollectionAssert.AreEqual(
                        expected,
                        Encoded(Ed25519PointReference.DoubleScalarMultBaseVartime(a, b, points[p])),
                        $"reference, {name}");
                    CollectionAssert.AreEqual(
                        expected,
                        Encoded(Ed25519Point.DoubleScalarMultBaseVartime(a, b, points[p])),
                        name);
                }
            }
        }
    }

    /// <summary>
    /// Verifies that <see cref="Ed25519Point.DoubleScalarMultBaseVartime" /> matches the replaced 1.1.0 routine for
    /// seeded inputs of the shape verification passes: both scalars reduced modulo the group order, and the point
    /// decoded from its encoding and negated.
    /// </summary>
    [TestMethod]
    public void DoubleScalarMultBaseVartime_WhenInputsTakeTheShapeVerificationPasses_ShouldMatchTheReplacedRoutine()
    {
        var random = new Random(0x2551_9114);
        byte[] wide = new byte[64];
        byte[] a = new byte[32];
        byte[] b = new byte[32];
        byte[] pointScalar = new byte[32];

        for (int iteration = 0; iteration < 64; iteration++)
        {
            random.NextBytes(wide);
            Ed25519Scalar.Reduce(wide, a);
            random.NextBytes(wide);
            Ed25519Scalar.Reduce(wide, b);
            random.NextBytes(pointScalar);
            Ed25519Point point = Decoded(Convert.ToHexString(Encoded(Ed25519Point.ScalarMultBase(pointScalar)))).Negate();

            CollectionAssert.AreEqual(
                Encoded(Ed25519PointReference.DoubleScalarMultBaseVartime(a, b, point)),
                Encoded(Ed25519Point.DoubleScalarMultBaseVartime(a, b, point)),
                $"iteration {iteration}");
        }
    }

    /// <summary>
    /// Verifies that <see cref="Ed25519Point.DoubleScalarMultBaseVartime" /> throws <see cref="ArgumentException" />
    /// naming the scalar that is not exactly 32 bytes.
    /// </summary>
    [TestMethod]
    public void DoubleScalarMultBaseVartime_WhenAScalarLengthIsInvalid_ShouldThrowArgumentException()
    {
        var baseException = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Ed25519Point.DoubleScalarMultBaseVartime(new byte[31], new byte[32], Ed25519Point.BasePoint);
        });
        var pointException = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Ed25519Point.DoubleScalarMultBaseVartime(new byte[32], new byte[33], Ed25519Point.BasePoint);
        });

        Assert.AreEqual("baseScalar", baseException.ParamName);
        Assert.AreEqual("pointScalar", pointException.ParamName);
    }
}

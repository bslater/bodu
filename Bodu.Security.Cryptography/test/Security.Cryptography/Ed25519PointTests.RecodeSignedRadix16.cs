// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519PointTests.RecodeSignedRadix16.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Security.Cryptography;

public partial class Ed25519PointTests
{
    /// <summary>
    /// Verifies that <see cref="Ed25519Point.RecodeSignedRadix16" /> returns digits from −8 to 7 and a carry of 0 or 1
    /// whose sum, Σ digits[j]·16^j plus the carry times 2^256, is the scalar, for the scalars that take every digit in
    /// every window, the recoding's bounds, and seeded scalars.
    /// </summary>
    [TestMethod]
    public void RecodeSignedRadix16_ForBoundaryAndSeededScalars_ShouldReproduceTheScalarFromDigitsBetweenMinusEightAndSeven()
    {
        var random = new Random(0x2551_9111);
        IEnumerable<byte[]> seeded = Enumerable.Range(0, 64).Select(_ =>
        {
            byte[] scalar = new byte[32];
            random.NextBytes(scalar);
            return scalar;
        });

        sbyte[] digits = new sbyte[64];
        foreach (byte[] scalar in SignedDigitScalars().Concat(seeded))
        {
            string name = Convert.ToHexString(scalar);
            int carry = Ed25519Point.RecodeSignedRadix16(scalar, digits);

            Assert.IsTrue(carry is 0 or 1, $"carry {carry}, {name}");
            BigInteger sum = new BigInteger(carry) << 256;
            for (int j = 0; j < digits.Length; j++)
            {
                Assert.IsTrue(digits[j] is >= -8 and <= 7, $"digit {j} is {digits[j]}, {name}");
                sum += new BigInteger(digits[j]) << (4 * j);
            }

            Assert.AreEqual(ScalarValue(scalar), sum, name);
        }
    }

    /// <summary>
    /// Verifies that <see cref="Ed25519Point.RecodeSignedRadix16" /> throws <see cref="ArgumentException" /> naming the
    /// scalar when it is not exactly 32 bytes.
    /// </summary>
    /// <param name="length">The scalar's length.</param>
    [TestMethod]
    [DataRow(31)]
    [DataRow(33)]
    public void RecodeSignedRadix16_WhenScalarLengthIsInvalid_ShouldThrowArgumentException(int length)
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Ed25519Point.RecodeSignedRadix16(new byte[length], new sbyte[64]);
        });

        Assert.AreEqual("scalar", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Ed25519Point.RecodeSignedRadix16" /> throws <see cref="ArgumentException" /> naming the
    /// digits when they are not exactly 64.
    /// </summary>
    /// <param name="length">The digit span's length.</param>
    [TestMethod]
    [DataRow(63)]
    [DataRow(65)]
    public void RecodeSignedRadix16_WhenDigitsLengthIsInvalid_ShouldThrowArgumentException(int length)
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Ed25519Point.RecodeSignedRadix16(new byte[32], new sbyte[length]);
        });

        Assert.AreEqual("digits", ex.ParamName);
    }
}

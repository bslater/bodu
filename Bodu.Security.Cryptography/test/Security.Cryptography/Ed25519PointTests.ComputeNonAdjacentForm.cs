// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519PointTests.ComputeNonAdjacentForm.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Security.Cryptography;

public partial class Ed25519PointTests
{
    /// <summary>
    /// Verifies that <see cref="Ed25519Point.ComputeNonAdjacentForm" /> returns, at each width, a form whose sum
    /// Σ digits[i]·2^i is the scalar, whose nonzero digits are odd, below 2^(width − 1) in magnitude and at least
    /// width positions apart, and whose top nonzero position it reports, for the recoding's bounds, scalars that
    /// carry past bit 255, and seeded scalars.
    /// </summary>
    /// <param name="testName">The row's name.</param>
    /// <param name="width">The window width.</param>
    [TestMethod]
    [DataRow("width 2", 2)]
    [DataRow("width 3", 3)]
    [DataRow("width 4", 4)]
    [DataRow("width 5", 5)]
    [DataRow("width 6", 6)]
    [DataRow("width 7", 7)]
    [DataRow("width 8", 8)]
    public void ComputeNonAdjacentForm_ForBoundaryAndSeededScalars_ShouldReproduceTheScalarFromSparseOddDigits(
        string testName, int width)
    {
        var random = new Random(0x2551_9112 + width);
        IEnumerable<byte[]> seeded = Enumerable.Range(0, 64).Select(_ =>
        {
            byte[] scalar = new byte[32];
            random.NextBytes(scalar);
            return scalar;
        });

        sbyte[] digits = new sbyte[Ed25519Point.NafLength];
        foreach (byte[] scalar in SignedDigitScalars().Concat(seeded))
        {
            string name = $"{testName}, {Convert.ToHexString(scalar)}";
            int top = Ed25519Point.ComputeNonAdjacentForm(scalar, width, digits);

            BigInteger sum = BigInteger.Zero;
            int previous = -width;
            int expectedTop = -1;
            for (int i = 0; i < digits.Length; i++)
            {
                int digit = digits[i];
                if (digit == 0)
                    continue;

                Assert.AreEqual(1, digit & 1, $"digit {i} is {digit}, {name}");
                Assert.IsTrue(Math.Abs(digit) < 1 << (width - 1), $"digit {i} is {digit}, {name}");
                Assert.IsTrue(i - previous >= width, $"digits {previous} and {i}, {name}");

                sum += new BigInteger(digit) << i;
                previous = i;
                expectedTop = i;
            }

            Assert.AreEqual(expectedTop, top, name);
            Assert.AreEqual(ScalarValue(scalar), sum, name);
        }
    }

    /// <summary>
    /// Verifies that <see cref="Ed25519Point.ComputeNonAdjacentForm" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> naming the width when it is below 2 or above 8.
    /// </summary>
    /// <param name="width">The window width.</param>
    [TestMethod]
    [DataRow(1)]
    [DataRow(9)]
    public void ComputeNonAdjacentForm_WhenWidthIsOutOfRange_ShouldThrowArgumentOutOfRangeException(int width)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Ed25519Point.ComputeNonAdjacentForm(new byte[32], width, new sbyte[Ed25519Point.NafLength]);
        });

        Assert.AreEqual("width", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Ed25519Point.ComputeNonAdjacentForm" /> throws <see cref="ArgumentException" /> naming
    /// the scalar when it is not exactly 32 bytes, and the digits when they are not exactly
    /// <see cref="Ed25519Point.NafLength" />.
    /// </summary>
    [TestMethod]
    public void ComputeNonAdjacentForm_WhenALengthIsInvalid_ShouldThrowArgumentException()
    {
        var scalarException = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Ed25519Point.ComputeNonAdjacentForm(new byte[31], 5, new sbyte[Ed25519Point.NafLength]);
        });
        var digitsException = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Ed25519Point.ComputeNonAdjacentForm(new byte[32], 5, new sbyte[256]);
        });

        Assert.AreEqual("scalar", scalarException.ParamName);
        Assert.AreEqual("digits", digitsException.ParamName);
    }
}

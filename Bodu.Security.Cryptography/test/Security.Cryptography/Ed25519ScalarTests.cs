// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519ScalarTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="Ed25519Scalar" /> mod-L arithmetic, cross-checked against
/// <see cref="BigInteger" /> reference computations.
/// </summary>
[TestClass]
public class Ed25519ScalarTests
{
    /// <summary>
    /// The Ed25519 group order L = 2^252 + 27742317777372353535851937790883648493.
    /// </summary>
    private static readonly BigInteger s_order =
        BigInteger.Pow(2, 252) + BigInteger.Parse("27742317777372353535851937790883648493");

    // ── Reduce ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="Ed25519Scalar.Reduce" /> matches a <see cref="BigInteger" /> reference reduction for
    /// structured edge values and seeded random 512-bit inputs.
    /// </summary>
    [TestMethod]
    public void Reduce_WhenComparedToBigIntegerReference_ShouldMatchForEdgeAndRandomInputs()
    {
        foreach (var input in EnumerateReduceInputs())
        {
            var expected = ToLittleEndian32(FromLittleEndian(input) % s_order);

            var actual = new byte[32];
            Ed25519Scalar.Reduce(input, actual);

            CollectionAssert.AreEqual(expected, actual, $"Reduce mismatch for input {Convert.ToHexString(input)}.");
        }
    }

    /// <summary>
    /// Verifies that <see cref="Ed25519Scalar.Reduce" /> throws <see cref="ArgumentException" /> when the source is
    /// not 64 bytes or the destination is not 32 bytes.
    /// </summary>
    [TestMethod]
    public void Reduce_WhenSpanLengthsAreInvalid_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            Ed25519Scalar.Reduce(new byte[63], new byte[32]);
        });

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            Ed25519Scalar.Reduce(new byte[64], new byte[31]);
        });
    }

    // ── MulAdd ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="Ed25519Scalar.MulAdd" /> matches the <see cref="BigInteger" /> reference computation
    /// (a · b + c) mod L for structured edge values and seeded random 256-bit inputs, including unreduced clamped
    /// scalars with bit 254 set.
    /// </summary>
    [TestMethod]
    public void MulAdd_WhenComparedToBigIntegerReference_ShouldMatchForEdgeAndRandomInputs()
    {
        var random = new Random(20260613);

        for (var trial = 0; trial < 500; trial++)
        {
            var a = new byte[32];
            var b = new byte[32];
            var c = new byte[32];
            random.NextBytes(a);
            random.NextBytes(b);
            random.NextBytes(c);

            if (trial % 4 == 0)
            {
                // Mirror a clamped RFC 8032 secret scalar: bits 0-2 clear, bit 254 set, bit 255 clear.
                a[0] &= 248;
                a[31] &= 127;
                a[31] |= 64;
            }

            if (trial == 0)
                Array.Fill(a, (byte)0xFF);

            var expected = ToLittleEndian32(
                ((FromLittleEndian(a) * FromLittleEndian(b)) + FromLittleEndian(c)) % s_order);

            var actual = new byte[32];
            Ed25519Scalar.MulAdd(a, b, c, actual);

            CollectionAssert.AreEqual(expected, actual, $"MulAdd mismatch on trial {trial}.");
        }
    }

    // ── IsCanonical ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="Ed25519Scalar.IsCanonical" /> accepts 0 and L − 1 and rejects L, L + 1, and the
    /// all-ones scalar.
    /// </summary>
    [TestMethod]
    public void IsCanonical_WhenValueStraddlesGroupOrder_ShouldAcceptBelowAndRejectAtOrAbove()
    {
        Assert.IsTrue(Ed25519Scalar.IsCanonical(ToLittleEndian32(BigInteger.Zero)));
        Assert.IsTrue(Ed25519Scalar.IsCanonical(ToLittleEndian32(s_order - 1)));
        Assert.IsFalse(Ed25519Scalar.IsCanonical(ToLittleEndian32(s_order)));
        Assert.IsFalse(Ed25519Scalar.IsCanonical(ToLittleEndian32(s_order + 1)));

        var allOnes = new byte[32];
        Array.Fill(allOnes, (byte)0xFF);
        Assert.IsFalse(Ed25519Scalar.IsCanonical(allOnes));
    }

    /// <summary>
    /// Enumerates the 512-bit reduction inputs: all-zero, all-ones, the order L and neighbors positioned in the low
    /// half, and seeded random values.
    /// </summary>
    /// <returns>The 64-byte little-endian inputs to exercise.</returns>
    private static IEnumerable<byte[]> EnumerateReduceInputs()
    {
        yield return new byte[64];

        var allOnes = new byte[64];
        Array.Fill(allOnes, (byte)0xFF);
        yield return allOnes;

        foreach (var value in new[] { s_order - 1, s_order, s_order + 1 })
        {
            var bytes = new byte[64];
            ToLittleEndian32(value).CopyTo(bytes, 0);
            yield return bytes;
        }

        var random = new Random(8032);
        for (var i = 0; i < 500; i++)
        {
            var bytes = new byte[64];
            random.NextBytes(bytes);
            yield return bytes;
        }
    }

    /// <summary>
    /// Interprets a little-endian byte span as a non-negative <see cref="BigInteger" />.
    /// </summary>
    /// <param name="bytes">The little-endian bytes.</param>
    /// <returns>The decoded value.</returns>
    private static BigInteger FromLittleEndian(ReadOnlySpan<byte> bytes) =>
        new(bytes, isUnsigned: true, isBigEndian: false);

    /// <summary>
    /// Encodes a non-negative value below 2^256 as 32 little-endian bytes.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The 32-byte little-endian encoding.</returns>
    private static byte[] ToLittleEndian32(BigInteger value)
    {
        var bytes = new byte[32];
        value.TryWriteBytes(bytes, out _, isUnsigned: true, isBigEndian: false);

        return bytes;
    }
}

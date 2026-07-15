// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GaloisField128Tests.Multiply.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class GaloisField128Tests
{
    /// <summary>
    /// Verifies that both the scalar and carry-less multiplies reproduce the documented GCM Test Case 2 GHASH product
    /// <c>C₁ · H</c> (NIST SP 800-38D), an oracle independent of either implementation.
    /// </summary>
    [TestMethod]
    public void Multiply_WhenGivenGcmTestCase2Operands_ShouldReturnDocumentedProduct()
    {
        byte[] c1 = Convert.FromHexString("0388dace60b6a392f328c2b971b2fe78");
        byte[] h = Convert.FromHexString("66e94bd4ef8a2c3b884cfa59ca342b2e");
        byte[] expected = Convert.FromHexString("5e2ec746917062882c85b0685353deb7");

        Span<byte> scalar = stackalloc byte[16];
        GaloisField128.MultiplyScalar(c1, h, scalar);
        CollectionAssert.AreEqual(expected, scalar.ToArray(), "Scalar multiply did not match the documented GHASH product.");

        Span<byte> dispatched = stackalloc byte[16];
        GaloisField128.Multiply(c1, h, dispatched);
        CollectionAssert.AreEqual(expected, dispatched.ToArray(), "Dispatched multiply did not match the documented GHASH product.");

        if (System.Runtime.Intrinsics.X86.Pclmulqdq.IsSupported)
        {
            Span<byte> clmul = stackalloc byte[16];
            GaloisField128.MultiplyClmul(c1, h, clmul);
            CollectionAssert.AreEqual(expected, clmul.ToArray(), "Carry-less multiply did not match the documented GHASH product.");
        }
    }

    /// <summary>
    /// Verifies that the carry-less multiply produces output bit-identical to the scalar reference across a large sweep
    /// of pseudo-random operand pairs, so the accelerated GHASH / POLYVAL path matches the spec-pinned scalar path.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void MultiplyClmul_WhenGivenRandomOperands_ShouldMatchScalar()
    {
        if (!System.Runtime.Intrinsics.X86.Pclmulqdq.IsSupported)
        {
            Assert.Inconclusive("PCLMULQDQ is not supported on this host; the carry-less path cannot be exercised.");
            return;
        }

        var rng = new Random(0x5EED_1234);
        Span<byte> x = stackalloc byte[16];
        Span<byte> h = stackalloc byte[16];
        Span<byte> expected = stackalloc byte[16];
        Span<byte> actual = stackalloc byte[16];

        for (int iteration = 0; iteration < 5_000; iteration++)
        {
            rng.NextBytes(x);
            rng.NextBytes(h);

            GaloisField128.MultiplyScalar(x, h, expected);
            GaloisField128.MultiplyClmul(x, h, actual);

            CollectionAssert.AreEqual(expected.ToArray(), actual.ToArray(),
                $"Carry-less product diverged from scalar on iteration {iteration}.");
        }
    }

    /// <summary>
    /// Verifies that the carry-less multiply matches the scalar reference on boundary operands (all-zero, all-ones, and
    /// single set bits) where reduction and shift behaviour is most likely to differ.
    /// </summary>
    [TestMethod]
    public void MultiplyClmul_WhenGivenBoundaryOperands_ShouldMatchScalar()
    {
        if (!System.Runtime.Intrinsics.X86.Pclmulqdq.IsSupported)
        {
            Assert.Inconclusive("PCLMULQDQ is not supported on this host; the carry-less path cannot be exercised.");
            return;
        }

        byte[][] operands = BuildBoundaryOperands();
        Span<byte> expected = stackalloc byte[16];
        Span<byte> actual = stackalloc byte[16];

        foreach (byte[] x in operands)
        {
            foreach (byte[] h in operands)
            {
                GaloisField128.MultiplyScalar(x, h, expected);
                GaloisField128.MultiplyClmul(x, h, actual);

                CollectionAssert.AreEqual(expected.ToArray(), actual.ToArray(),
                    $"Carry-less product diverged from scalar for x={Convert.ToHexString(x)} h={Convert.ToHexString(h)}.");
            }
        }
    }

    /// <summary>
    /// Verifies that in-place multiplication (the <paramref name="result" /> span aliasing the <paramref name="x" />
    /// operand) yields the same output through the carry-less path as a non-aliased scalar multiply, matching the way
    /// the GHASH accumulator is updated in place.
    /// </summary>
    [TestMethod]
    public void MultiplyClmul_WhenResultAliasesLeftOperand_ShouldMatchScalar()
    {
        if (!System.Runtime.Intrinsics.X86.Pclmulqdq.IsSupported)
        {
            Assert.Inconclusive("PCLMULQDQ is not supported on this host; the carry-less path cannot be exercised.");
            return;
        }

        var rng = new Random(0x0C0F_FEE1);
        Span<byte> h = stackalloc byte[16];
        Span<byte> expected = stackalloc byte[16];
        Span<byte> inPlace = stackalloc byte[16];

        for (int iteration = 0; iteration < 1_000; iteration++)
        {
            rng.NextBytes(inPlace);
            rng.NextBytes(h);
            inPlace.CopyTo(expected);

            GaloisField128.MultiplyScalar(expected, h, expected);
            GaloisField128.MultiplyClmul(inPlace, h, inPlace);

            CollectionAssert.AreEqual(expected.ToArray(), inPlace.ToArray(),
                $"In-place carry-less product diverged on iteration {iteration}.");
        }
    }

    /// <summary>
    /// Builds the boundary operand set: all-zero, all-ones, and every single-bit-set 16-byte block.
    /// </summary>
    /// <returns>The array of boundary operand blocks.</returns>
    private static byte[][] BuildBoundaryOperands()
    {
        var operands = new List<byte[]>
        {
            new byte[16],
            Enumerable.Repeat((byte)0xFF, 16).ToArray(),
        };

        for (int bit = 0; bit < 128; bit++)
        {
            var block = new byte[16];
            block[bit >> 3] = (byte)(1 << (7 - (bit & 7)));
            operands.Add(block);
        }

        return operands.ToArray();
    }
}

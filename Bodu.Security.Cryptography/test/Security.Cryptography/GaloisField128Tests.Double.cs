// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GaloisField128Tests.Double.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public sealed partial class GaloisField128Tests
{
    /// <summary>
    /// Verifies that <see cref="GaloisField128.Double" /> reproduces the documented AES-CMAC subkey-generation example
    /// (RFC 4493 §4): <c>K1 = double(L)</c> and <c>K2 = double(K1)</c>, where <c>L = AES-128(K, 0¹²⁸)</c> is derived
    /// here through the BCL AES implementation so the expected values are anchored to an independent oracle. The pair
    /// covers both the carry-free case (<c>L</c> has a clear most-significant bit) and the reduction case (<c>K1</c>
    /// has the most-significant bit set, folding <c>0x87</c> into the last byte).
    /// </summary>
    [TestMethod]
    public void Double_WhenGivenRfc4493SubkeyVectors_ShouldReturnDocumentedSubkeys()
    {
        byte[] key = Convert.FromHexString("2b7e151628aed2a6abf7158809cf4f3c");
        byte[] expectedL = Convert.FromHexString("7df76b0c1ab899b33e42f047b91b546f");
        byte[] expectedK1 = Convert.FromHexString("fbeed618357133667c85e08f7236a8de");
        byte[] expectedK2 = Convert.FromHexString("f7ddac306ae266ccf90bc11ee46d513b");

        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        aes.Key = key;
        byte[] l = aes.EncryptEcb(new byte[16], PaddingMode.None);
        CollectionAssert.AreEqual(expectedL, l, "BCL AES did not reproduce the documented L = AES-128(K, 0) value.");

        Span<byte> k1 = stackalloc byte[16];
        GaloisField128.Double(l, k1);
        CollectionAssert.AreEqual(expectedK1, k1.ToArray(), "double(L) did not match the documented K1 subkey.");

        Span<byte> k2 = stackalloc byte[16];
        GaloisField128.Double(k1, k2);
        CollectionAssert.AreEqual(expectedK2, k2.ToArray(), "double(K1) did not match the documented K2 subkey.");
    }

    /// <summary>
    /// Verifies that doubling a block whose most-significant bit is clear is a plain one-bit left shift with no
    /// reduction, including the cross-byte carry propagation.
    /// </summary>
    [TestMethod]
    public void Double_WhenMsbClear_ShouldEqualLeftShift()
    {
        byte[] input = Convert.FromHexString("0102030405060708090a0b0c0d0e0f10");
        byte[] expected = Convert.FromHexString("020406080a0c0e10121416181a1c1e20");

        Span<byte> result = stackalloc byte[16];
        GaloisField128.Double(input, result);

        CollectionAssert.AreEqual(expected, result.ToArray(), "MSB-clear doubling did not equal a one-bit left shift.");
    }

    /// <summary>
    /// Verifies that doubling a block whose most-significant bit is set folds the reduction constant <c>0x87</c> into
    /// the last byte, on both the minimal single-bit block and the all-ones block.
    /// </summary>
    [TestMethod]
    public void Double_WhenMsbSet_ShouldFoldReductionIntoLastByte()
    {
        byte[] highBit = Convert.FromHexString("80000000000000000000000000000000");
        byte[] expectedHighBit = Convert.FromHexString("00000000000000000000000000000087");

        Span<byte> result = stackalloc byte[16];
        GaloisField128.Double(highBit, result);
        CollectionAssert.AreEqual(expectedHighBit, result.ToArray(), "Doubling x¹²⁷ did not yield the reduction polynomial 0x87.");

        byte[] allOnes = Enumerable.Repeat((byte)0xFF, 16).ToArray();
        byte[] expectedAllOnes = Convert.FromHexString("ffffffffffffffffffffffffffffff79");

        GaloisField128.Double(allOnes, result);
        CollectionAssert.AreEqual(expectedAllOnes, result.ToArray(), "Doubling the all-ones block did not fold the reduction correctly.");
    }

    /// <summary>
    /// Verifies that doubling a zero block yields zero (the field's additive identity is fixed under multiplication).
    /// </summary>
    [TestMethod]
    public void Double_WhenGivenZeroBlock_ShouldReturnZero()
    {
        Span<byte> result = stackalloc byte[16];
        result.Fill(0xCC);

        GaloisField128.Double(new byte[16], result);

        CollectionAssert.AreEqual(new byte[16], result.ToArray(), "Doubling the zero block did not return zero.");
    }

    /// <summary>
    /// Verifies that in-place doubling (the <paramref name="result" /> span fully aliasing the input) yields the same
    /// output as a non-aliased call, matching the way the SIV / EAX subkey derivations double their blocks in place.
    /// </summary>
    [TestMethod]
    public void Double_WhenResultAliasesInput_ShouldMatchNonAliased()
    {
        var rng = new Random(0x0D0B_1E00);
        Span<byte> inPlace = stackalloc byte[16];
        Span<byte> input = stackalloc byte[16];
        Span<byte> expected = stackalloc byte[16];

        for (int iteration = 0; iteration < 1_000; iteration++)
        {
            rng.NextBytes(input);
            input.CopyTo(inPlace);

            GaloisField128.Double(input, expected);
            GaloisField128.Double(inPlace, inPlace);

            CollectionAssert.AreEqual(expected.ToArray(), inPlace.ToArray(),
                $"In-place doubling diverged on iteration {iteration}.");
        }
    }

    /// <summary>
    /// Verifies that the branch-free doubling produces output bit-identical to the classic branchy reference
    /// implementation (the form previously embedded in the EAX / SIV / OCB transforms) across a large sweep of
    /// pseudo-random blocks, so the hardening is a pure control-flow change with no functional difference.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Double_WhenGivenRandomInputs_ShouldMatchBranchyReference()
    {
        var rng = new Random(0x5EED_D0B1);
        Span<byte> input = stackalloc byte[16];
        Span<byte> actual = stackalloc byte[16];

        for (int iteration = 0; iteration < 10_000; iteration++)
        {
            rng.NextBytes(input);

            byte[] expected = BranchyDoubleReference(input.ToArray());
            GaloisField128.Double(input, actual);

            CollectionAssert.AreEqual(expected, actual.ToArray(),
                $"Branch-free doubling diverged from the branchy reference on iteration {iteration}.");
        }
    }

    /// <summary>
    /// Computes the GF(2¹²⁸) doubling using the classic branchy formulation (left shift, then conditionally XOR
    /// <c>0x87</c> into the last byte). Retained here verbatim as the equivalence oracle for the branch-free
    /// production implementation.
    /// </summary>
    /// <param name="x">The 16-byte input block.</param>
    /// <returns>The doubled block.</returns>
    private static byte[] BranchyDoubleReference(byte[] x)
    {
        byte[] result = new byte[x.Length];
        bool msb = (x[0] & 0x80) != 0;

        for (int i = 0; i < x.Length - 1; i++)
            result[i] = (byte)((x[i] << 1) | (x[i + 1] >> 7));

        result[x.Length - 1] = (byte)(x[^1] << 1);

        if (msb)
            result[x.Length - 1] ^= 0x87;

        return result;
    }
}

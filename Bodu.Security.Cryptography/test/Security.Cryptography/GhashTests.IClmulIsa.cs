// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GhashTests.IClmulIsa.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.Intrinsics;

namespace Bodu.Security.Cryptography;

/// <summary>
/// The instruction-set shims of the carry-less kernel: each operation of each shim is held to its software definition,
/// so the shim that runs only on ARM64 is checked operation by operation wherever that hardware runs the suite.
/// </summary>
public sealed partial class GhashTests
{
    /// <summary>The number of seeded random operand pairs each shim test applies.</summary>
    private const int ShimSamples = 1000;

    /// <summary>
    /// Verifies that each shim multiplies the low halves of its operands without carries.
    /// </summary>
    /// <param name="isa">The shim's instruction set.</param>
    [TestMethod]
    [DataRow("Pclmulqdq")]
    [DataRow("Pmull")]
    public void MultiplyLower_WhenGivenBoundaryAndRandomOperands_ForEachIsa_ShouldMultiplyTheLowHalvesWithoutCarries(string isa)
    {
        Func<Vector128<ulong>, Vector128<ulong>, Vector128<ulong>> multiply = IsPclmulqdq(isa)
            ? Ghash.PclmulqdqIsa.MultiplyLower
            : Ghash.PmullIsa.MultiplyLower;

        foreach ((ulong x0, ulong x1, ulong y0, ulong y1) in ShimOperands())
            Assert.AreEqual(CarrylessProduct(x0, y0), multiply(Vector128.Create(x0, x1), Vector128.Create(y0, y1)), $"{x0:X16} {x1:X16} {y0:X16} {y1:X16}");
    }

    /// <summary>
    /// Verifies that each shim multiplies the high halves of its operands without carries.
    /// </summary>
    /// <param name="isa">The shim's instruction set.</param>
    [TestMethod]
    [DataRow("Pclmulqdq")]
    [DataRow("Pmull")]
    public void MultiplyUpper_WhenGivenBoundaryAndRandomOperands_ForEachIsa_ShouldMultiplyTheHighHalvesWithoutCarries(string isa)
    {
        Func<Vector128<ulong>, Vector128<ulong>, Vector128<ulong>> multiply = IsPclmulqdq(isa)
            ? Ghash.PclmulqdqIsa.MultiplyUpper
            : Ghash.PmullIsa.MultiplyUpper;

        foreach ((ulong x0, ulong x1, ulong y0, ulong y1) in ShimOperands())
            Assert.AreEqual(CarrylessProduct(x1, y1), multiply(Vector128.Create(x0, x1), Vector128.Create(y0, y1)), $"{x0:X16} {x1:X16} {y0:X16} {y1:X16}");
    }

    /// <summary>
    /// Verifies that each shim multiplies the first operand's low half by the second's high half without carries.
    /// </summary>
    /// <param name="isa">The shim's instruction set.</param>
    [TestMethod]
    [DataRow("Pclmulqdq")]
    [DataRow("Pmull")]
    public void MultiplyLowerUpper_WhenGivenBoundaryAndRandomOperands_ForEachIsa_ShouldMultiplyTheCrossHalvesWithoutCarries(string isa)
    {
        Func<Vector128<ulong>, Vector128<ulong>, Vector128<ulong>> multiply = IsPclmulqdq(isa)
            ? Ghash.PclmulqdqIsa.MultiplyLowerUpper
            : Ghash.PmullIsa.MultiplyLowerUpper;

        foreach ((ulong x0, ulong x1, ulong y0, ulong y1) in ShimOperands())
            Assert.AreEqual(CarrylessProduct(x0, y1), multiply(Vector128.Create(x0, x1), Vector128.Create(y0, y1)), $"{x0:X16} {x1:X16} {y0:X16} {y1:X16}");
    }

    /// <summary>
    /// Verifies that each shim multiplies the first operand's high half by the second's low half without carries.
    /// </summary>
    /// <param name="isa">The shim's instruction set.</param>
    [TestMethod]
    [DataRow("Pclmulqdq")]
    [DataRow("Pmull")]
    public void MultiplyUpperLower_WhenGivenBoundaryAndRandomOperands_ForEachIsa_ShouldMultiplyTheCrossHalvesWithoutCarries(string isa)
    {
        Func<Vector128<ulong>, Vector128<ulong>, Vector128<ulong>> multiply = IsPclmulqdq(isa)
            ? Ghash.PclmulqdqIsa.MultiplyUpperLower
            : Ghash.PmullIsa.MultiplyUpperLower;

        foreach ((ulong x0, ulong x1, ulong y0, ulong y1) in ShimOperands())
            Assert.AreEqual(CarrylessProduct(x1, y0), multiply(Vector128.Create(x0, x1), Vector128.Create(y0, y1)), $"{x0:X16} {x1:X16} {y0:X16} {y1:X16}");
    }

    /// <summary>
    /// Verifies that each shim reverses the order of the sixteen bytes.
    /// </summary>
    /// <param name="isa">The shim's instruction set.</param>
    [TestMethod]
    [DataRow("Pclmulqdq")]
    [DataRow("Pmull")]
    public void ReverseBytes_WhenGivenBoundaryAndRandomBlocks_ForEachIsa_ShouldReverseTheSixteenBytes(string isa)
    {
        Func<Vector128<byte>, Vector128<byte>> reverse = IsPclmulqdq(isa)
            ? Ghash.PclmulqdqIsa.ReverseBytes
            : Ghash.PmullIsa.ReverseBytes;

        foreach (byte[] value in ShimBlocks())
            Assert.AreEqual(Vector128.Create(ReverseCopy(value)), reverse(Vector128.Create(value)), Convert.ToHexString(value));
    }

    /// <summary>
    /// Verifies that each shim shifts whole bytes toward the high end, filling with zeros.
    /// </summary>
    /// <param name="isa">The shim's instruction set.</param>
    /// <param name="bytes">The shift, in bytes.</param>
    [TestMethod]
    [DataRow("Pclmulqdq", 4)]
    [DataRow("Pclmulqdq", 8)]
    [DataRow("Pclmulqdq", 12)]
    [DataRow("Pmull", 4)]
    [DataRow("Pmull", 8)]
    [DataRow("Pmull", 12)]
    public void ShiftBytesLeft_WhenShiftingByWholeWords_ForEachIsa_ShouldShiftTowardTheHighEnd(string isa, int bytes)
    {
        bool pclmulqdq = IsPclmulqdq(isa);
        Func<Vector128<byte>, Vector128<byte>> shift = bytes switch
        {
            4 => pclmulqdq ? Ghash.PclmulqdqIsa.ShiftBytesLeft4 : Ghash.PmullIsa.ShiftBytesLeft4,
            8 => pclmulqdq ? Ghash.PclmulqdqIsa.ShiftBytesLeft8 : Ghash.PmullIsa.ShiftBytesLeft8,
            _ => pclmulqdq ? Ghash.PclmulqdqIsa.ShiftBytesLeft12 : Ghash.PmullIsa.ShiftBytesLeft12,
        };

        foreach (byte[] value in ShimBlocks())
            Assert.AreEqual(Vector128.Create(ShiftCopy(value, bytes)), shift(Vector128.Create(value)), Convert.ToHexString(value));
    }

    /// <summary>
    /// Verifies that each shim shifts whole bytes toward the low end, filling with zeros.
    /// </summary>
    /// <param name="isa">The shim's instruction set.</param>
    /// <param name="bytes">The shift, in bytes.</param>
    [TestMethod]
    [DataRow("Pclmulqdq", 4)]
    [DataRow("Pclmulqdq", 8)]
    [DataRow("Pclmulqdq", 12)]
    [DataRow("Pmull", 4)]
    [DataRow("Pmull", 8)]
    [DataRow("Pmull", 12)]
    public void ShiftBytesRight_WhenShiftingByWholeWords_ForEachIsa_ShouldShiftTowardTheLowEnd(string isa, int bytes)
    {
        bool pclmulqdq = IsPclmulqdq(isa);
        Func<Vector128<byte>, Vector128<byte>> shift = bytes switch
        {
            4 => pclmulqdq ? Ghash.PclmulqdqIsa.ShiftBytesRight4 : Ghash.PmullIsa.ShiftBytesRight4,
            8 => pclmulqdq ? Ghash.PclmulqdqIsa.ShiftBytesRight8 : Ghash.PmullIsa.ShiftBytesRight8,
            _ => pclmulqdq ? Ghash.PclmulqdqIsa.ShiftBytesRight12 : Ghash.PmullIsa.ShiftBytesRight12,
        };

        foreach (byte[] value in ShimBlocks())
            Assert.AreEqual(Vector128.Create(ShiftCopy(value, -bytes)), shift(Vector128.Create(value)), Convert.ToHexString(value));
    }

    /// <summary>
    /// Returns whether the named shim is the x64 one, marking the test inconclusive when the processor cannot run the
    /// named shim.
    /// </summary>
    /// <param name="isa">The shim's instruction set: <c>Pclmulqdq</c> or <c>Pmull</c>.</param>
    /// <returns><see langword="true" /> for the <c>PCLMULQDQ</c> shim; <see langword="false" /> for the <c>PMULL</c> one.</returns>
    private static bool IsPclmulqdq(string isa)
    {
        Ghash.KernelKind kernel = Enum.Parse<Ghash.KernelKind>(isa);
        if (!Ghash.IsSupported(kernel))
            Assert.Inconclusive($"The {isa} shim cannot run on this processor.");

        return kernel == Ghash.KernelKind.Pclmulqdq;
    }

    /// <summary>
    /// Returns the 128-bit carry-less product of two 64-bit values, computed bit by bit.
    /// </summary>
    /// <param name="x">The first factor.</param>
    /// <param name="y">The second factor.</param>
    /// <returns>The product, low half first.</returns>
    private static Vector128<ulong> CarrylessProduct(ulong x, ulong y)
    {
        UInt128 product = 0;
        for (int bit = 0; bit < 64; bit++)
        {
            if (((y >> bit) & 1) != 0)
                product ^= (UInt128)x << bit;
        }

        return Vector128.Create((ulong)product, (ulong)(product >> 64));
    }

    /// <summary>
    /// Returns a copy of a 16-byte block with its bytes moved <paramref name="bytes" /> positions toward the high end -
    /// toward the low end when negative - and zeros shifted in.
    /// </summary>
    /// <param name="value">The block.</param>
    /// <param name="bytes">The shift, in bytes.</param>
    /// <returns>The shifted copy.</returns>
    private static byte[] ShiftCopy(byte[] value, int bytes)
    {
        byte[] shifted = new byte[16];
        for (int index = 0; index < 16; index++)
        {
            int source = index - bytes;
            if (source >= 0 && source < 16)
                shifted[index] = value[source];
        }

        return shifted;
    }

    /// <summary>
    /// Returns operand quadruples for the multiply tests: boundary words, then seeded random ones.
    /// </summary>
    /// <returns>Two words for each of two vectors.</returns>
    private static IEnumerable<(ulong X0, ulong X1, ulong Y0, ulong Y1)> ShimOperands()
    {
        ulong[] boundaries = [0, 1, 0xFFFF_FFFF, 0x1_0000_0000, 0x8000_0000_0000_0000, 0x0123_4567_89AB_CDEF, ulong.MaxValue];
        foreach (ulong x in boundaries)
        {
            foreach (ulong y in boundaries)
                yield return (x, ~y, y, x ^ 0xA5A5_A5A5_A5A5_A5A5);
        }

        var random = new Random(0x6A5C_0128);
        for (int sample = 0; sample < ShimSamples; sample++)
            yield return ((ulong)random.NextInt64(), (ulong)random.NextInt64() << 1, (ulong)random.NextInt64(), ~(ulong)random.NextInt64());
    }

    /// <summary>
    /// Returns 16-byte blocks for the byte-movement tests: zero, every bit set, ascending bytes, then seeded random ones.
    /// </summary>
    /// <returns>The blocks.</returns>
    private static IEnumerable<byte[]> ShimBlocks()
    {
        yield return new byte[16];
        yield return Enumerable.Repeat((byte)0xFF, 16).ToArray();
        yield return Enumerable.Range(1, 16).Select(value => (byte)value).ToArray();

        var random = new Random(0x6A5C_0129);
        for (int sample = 0; sample < ShimSamples; sample++)
        {
            byte[] block = new byte[16];
            random.NextBytes(block);
            yield return block;
        }
    }
}

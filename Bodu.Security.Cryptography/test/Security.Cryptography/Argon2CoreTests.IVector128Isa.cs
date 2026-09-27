// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2CoreTests.IVector128Isa.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using System.Runtime.Intrinsics;

namespace Bodu.Security.Cryptography;

/// <summary>
/// The instruction-set shims of the 128-bit kernel: each operation of each shim is held to its scalar definition, so a
/// shim that runs only on ARM64 is checked operation by operation wherever that hardware runs the suite.
/// </summary>
public sealed partial class Argon2CoreTests
{
    /// <summary>The number of seeded random operand pairs each shim test applies.</summary>
    private const int ShimSamples = 1000;

    /// <summary>
    /// Verifies that each shim multiplies the low 32 bits of each pair of words into the full 64-bit product.
    /// </summary>
    /// <param name="isa">The shim's instruction set.</param>
    [TestMethod]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    public void MultiplyLow_ForEachIsa_ShouldMultiplyTheLowHalvesIntoFullProducts(string isa)
    {
        Func<Vector128<ulong>, Vector128<ulong>, Vector128<ulong>> multiplyLow = ParseSupportedKernel(isa) == Argon2Core.KernelKind.Ssse3
            ? Argon2Core.Ssse3Isa.MultiplyLow
            : Argon2Core.AdvSimdIsa.MultiplyLow;

        foreach ((ulong x0, ulong x1, ulong y0, ulong y1) in ShimOperands())
        {
            Vector128<ulong> expected = Vector128.Create((ulong)(uint)x0 * (uint)y0, (ulong)(uint)x1 * (uint)y1);

            Assert.AreEqual(expected, multiplyLow(Vector128.Create(x0, x1), Vector128.Create(y0, y1)), $"{x0:X16} {x1:X16} {y0:X16} {y1:X16}");
        }
    }

    /// <summary>
    /// Verifies that each shim rotates each word right by 32, 24, and 16 bits.
    /// </summary>
    /// <param name="isa">The shim's instruction set.</param>
    /// <param name="bits">The rotation.</param>
    [TestMethod]
    [DataRow("Ssse3", 32)]
    [DataRow("Ssse3", 24)]
    [DataRow("Ssse3", 16)]
    [DataRow("AdvSimd", 32)]
    [DataRow("AdvSimd", 24)]
    [DataRow("AdvSimd", 16)]
    public void RotateRight_ForEachIsa_ShouldRotateEachWord(string isa, int bits)
    {
        bool ssse3 = ParseSupportedKernel(isa) == Argon2Core.KernelKind.Ssse3;
        Func<Vector128<ulong>, Vector128<ulong>> rotate = bits switch
        {
            32 => ssse3 ? Argon2Core.Ssse3Isa.RotateRight32 : Argon2Core.AdvSimdIsa.RotateRight32,
            24 => ssse3 ? Argon2Core.Ssse3Isa.RotateRight24 : Argon2Core.AdvSimdIsa.RotateRight24,
            _ => ssse3 ? Argon2Core.Ssse3Isa.RotateRight16 : Argon2Core.AdvSimdIsa.RotateRight16,
        };

        foreach ((ulong x0, ulong x1, _, _) in ShimOperands())
        {
            Vector128<ulong> expected = Vector128.Create(BitOperations.RotateRight(x0, bits), BitOperations.RotateRight(x1, bits));

            Assert.AreEqual(expected, rotate(Vector128.Create(x0, x1)), $"{x0:X16} {x1:X16}");
        }
    }

    /// <summary>
    /// Verifies that each shim returns the first vector's upper word followed by the second vector's lower word.
    /// </summary>
    /// <param name="isa">The shim's instruction set.</param>
    [TestMethod]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    public void UpperThenLower_ForEachIsa_ShouldTakeOneWordFromEachVector(string isa)
    {
        Func<Vector128<ulong>, Vector128<ulong>, Vector128<ulong>> upperThenLower = ParseSupportedKernel(isa) == Argon2Core.KernelKind.Ssse3
            ? Argon2Core.Ssse3Isa.UpperThenLower
            : Argon2Core.AdvSimdIsa.UpperThenLower;

        foreach ((ulong x0, ulong x1, ulong y0, ulong y1) in ShimOperands())
            Assert.AreEqual(Vector128.Create(x1, y0), upperThenLower(Vector128.Create(x0, x1), Vector128.Create(y0, y1)));
    }

    /// <summary>
    /// Returns operand quadruples for the shim tests: boundary words, then seeded random ones.
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

        var random = new Random(0x5E3D_0128);
        for (int sample = 0; sample < ShimSamples; sample++)
            yield return ((ulong)random.NextInt64(), (ulong)random.NextInt64() << 1, (ulong)random.NextInt64(), ~(ulong)random.NextInt64());
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2sCoreTests.IVector128Isa.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using System.Runtime.Intrinsics;

namespace Bodu.Security.Cryptography;

/// <summary>
/// The instruction-set shims of the 128-bit kernel: each rotation, lane rotation and transpose of each shim is held to
/// its definition, so a shim that runs only on ARM64 is checked operation by operation wherever that hardware runs the
/// suite.
/// </summary>
public sealed partial class Blake2sCoreTests
{
    /// <summary>
    /// Verifies that each shim rotates each word right by 16, 12, 8, and 7 bits.
    /// </summary>
    /// <param name="isa">The shim's instruction set.</param>
    /// <param name="bits">The rotation.</param>
    [TestMethod]
    [DataRow("Ssse3", 16)]
    [DataRow("Ssse3", 12)]
    [DataRow("Ssse3", 8)]
    [DataRow("Ssse3", 7)]
    [DataRow("AdvSimd", 16)]
    [DataRow("AdvSimd", 12)]
    [DataRow("AdvSimd", 8)]
    [DataRow("AdvSimd", 7)]
    [DataRow("Avx512", 16)]
    [DataRow("Avx512", 12)]
    [DataRow("Avx512", 8)]
    [DataRow("Avx512", 7)]
    public void RotateRight_ForEachIsa_ShouldRotateEachWord(string isa, int bits)
    {
        Func<Vector128<uint>, Vector128<uint>> rotate = (ParseSupportedKernel(isa), bits) switch
        {
            (Blake2sCore.KernelKind.Ssse3, 16) => Blake2sCore.Ssse3Isa.RotateRight16,
            (Blake2sCore.KernelKind.Ssse3, 12) => Blake2sCore.Ssse3Isa.RotateRight12,
            (Blake2sCore.KernelKind.Ssse3, 8) => Blake2sCore.Ssse3Isa.RotateRight8,
            (Blake2sCore.KernelKind.Ssse3, _) => Blake2sCore.Ssse3Isa.RotateRight7,
            (Blake2sCore.KernelKind.AdvSimd, 16) => Blake2sCore.AdvSimdIsa.RotateRight16,
            (Blake2sCore.KernelKind.AdvSimd, 12) => Blake2sCore.AdvSimdIsa.RotateRight12,
            (Blake2sCore.KernelKind.AdvSimd, 8) => Blake2sCore.AdvSimdIsa.RotateRight8,
            (Blake2sCore.KernelKind.AdvSimd, _) => Blake2sCore.AdvSimdIsa.RotateRight7,
            (_, 16) => Blake2sCore.Avx512Isa.RotateRight16,
            (_, 12) => Blake2sCore.Avx512Isa.RotateRight12,
            (_, 8) => Blake2sCore.Avx512Isa.RotateRight8,
            _ => Blake2sCore.Avx512Isa.RotateRight7,
        };

        var random = new Random(bits);
        uint[] boundaries = [0, 1, 0xFFFF, 0x8000_0000, 0x0123_4567, uint.MaxValue];
        for (int sample = 0; sample < 1000; sample++)
        {
            uint[] words = sample < boundaries.Length
                ? [boundaries[sample], ~boundaries[sample], boundaries[sample] << 1, boundaries[sample] >> 1]
                : [(uint)random.Next(), (uint)random.Next() << 1, ~(uint)random.Next(), (uint)random.Next()];
            Vector128<uint> expected = Vector128.Create(
                BitOperations.RotateRight(words[0], bits),
                BitOperations.RotateRight(words[1], bits),
                BitOperations.RotateRight(words[2], bits),
                BitOperations.RotateRight(words[3], bits));

            Assert.AreEqual(expected, rotate(Vector128.Create(words[0], words[1], words[2], words[3])), $"{words[0]:X8}");
        }
    }

    /// <summary>
    /// Verifies that each shim rotates the four lanes toward lane 0 by one, two, and three places.
    /// </summary>
    /// <param name="isa">The shim's instruction set.</param>
    /// <param name="places">The number of places.</param>
    [TestMethod]
    [DataRow("Ssse3", 1)]
    [DataRow("Ssse3", 2)]
    [DataRow("Ssse3", 3)]
    [DataRow("AdvSimd", 1)]
    [DataRow("AdvSimd", 2)]
    [DataRow("AdvSimd", 3)]
    [DataRow("Avx512", 1)]
    [DataRow("Avx512", 2)]
    [DataRow("Avx512", 3)]
    public void RotateLanes_ForEachIsa_ShouldTakeEachLaneFromItsSuccessor(string isa, int places)
    {
        Func<Vector128<uint>, Vector128<uint>> rotate = (ParseSupportedKernel(isa), places) switch
        {
            (Blake2sCore.KernelKind.Ssse3, 1) => Blake2sCore.Ssse3Isa.RotateLanes1,
            (Blake2sCore.KernelKind.Ssse3, 2) => Blake2sCore.Ssse3Isa.RotateLanes2,
            (Blake2sCore.KernelKind.Ssse3, _) => Blake2sCore.Ssse3Isa.RotateLanes3,
            (Blake2sCore.KernelKind.AdvSimd, 1) => Blake2sCore.AdvSimdIsa.RotateLanes1,
            (Blake2sCore.KernelKind.AdvSimd, 2) => Blake2sCore.AdvSimdIsa.RotateLanes2,
            (Blake2sCore.KernelKind.AdvSimd, _) => Blake2sCore.AdvSimdIsa.RotateLanes3,
            (_, 1) => Blake2sCore.Avx512Isa.RotateLanes1,
            (_, 2) => Blake2sCore.Avx512Isa.RotateLanes2,
            _ => Blake2sCore.Avx512Isa.RotateLanes3,
        };

        uint[] lanes = [0x0102_0304, 0x1112_1314, 0x2122_2324, 0x3132_3334];
        Vector128<uint> expected = Vector128.Create(
            lanes[places % 4],
            lanes[(1 + places) % 4],
            lanes[(2 + places) % 4],
            lanes[(3 + places) % 4]);

        Assert.AreEqual(expected, rotate(Vector128.Create(lanes[0], lanes[1], lanes[2], lanes[3])));
    }

    /// <summary>
    /// Verifies that each shim transposes four rows of four words, so that each row afterwards holds what was the
    /// matching column.
    /// </summary>
    /// <param name="isa">The shim's instruction set.</param>
    [TestMethod]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx512")]
    public void Transpose_ForEachIsa_ShouldExchangeRowsAndColumns(string isa)
    {
        Blake2sCore.KernelKind kind = ParseSupportedKernel(isa);
        Vector128<uint> row0 = Vector128.Create(0x00U, 0x01U, 0x02U, 0x03U);
        Vector128<uint> row1 = Vector128.Create(0x10U, 0x11U, 0x12U, 0x13U);
        Vector128<uint> row2 = Vector128.Create(0x20U, 0x21U, 0x22U, 0x23U);
        Vector128<uint> row3 = Vector128.Create(0x30U, 0x31U, 0x32U, 0x33U);

        switch (kind)
        {
            case Blake2sCore.KernelKind.Ssse3:
                Blake2sCore.Ssse3Isa.Transpose(ref row0, ref row1, ref row2, ref row3);
                break;

            case Blake2sCore.KernelKind.AdvSimd:
                Blake2sCore.AdvSimdIsa.Transpose(ref row0, ref row1, ref row2, ref row3);
                break;

            default:
                Blake2sCore.Avx512Isa.Transpose(ref row0, ref row1, ref row2, ref row3);
                break;
        }

        Assert.AreEqual(Vector128.Create(0x00U, 0x10U, 0x20U, 0x30U), row0);
        Assert.AreEqual(Vector128.Create(0x01U, 0x11U, 0x21U, 0x31U), row1);
        Assert.AreEqual(Vector128.Create(0x02U, 0x12U, 0x22U, 0x32U), row2);
        Assert.AreEqual(Vector128.Create(0x03U, 0x13U, 0x23U, 0x33U), row3);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2bCoreTests.IVector256Isa.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using System.Runtime.Intrinsics;

namespace Bodu.Security.Cryptography;

/// <summary>
/// The instruction-set shims of the 256-bit kernel: each rotation of each shim is held to its scalar definition.
/// </summary>
public sealed partial class Blake2bCoreTests
{
    /// <summary>
    /// Verifies that each shim rotates each word right by 32, 24, 16, and 63 bits.
    /// </summary>
    /// <param name="isa">The shim's instruction set.</param>
    /// <param name="bits">The rotation.</param>
    [TestMethod]
    [DataRow("Avx2", 32)]
    [DataRow("Avx2", 24)]
    [DataRow("Avx2", 16)]
    [DataRow("Avx2", 63)]
    [DataRow("Avx512", 32)]
    [DataRow("Avx512", 24)]
    [DataRow("Avx512", 16)]
    [DataRow("Avx512", 63)]
    public void RotateRight_ForEachIsa_ShouldRotateEachWord(string isa, int bits)
    {
        bool avx2 = ParseSupportedKernel(isa) == Blake2bCore.KernelKind.Avx2;
        Func<Vector256<ulong>, Vector256<ulong>> rotate = bits switch
        {
            32 => avx2 ? Blake2bCore.Avx2Isa.RotateRight32 : Blake2bCore.Avx512Isa.RotateRight32,
            24 => avx2 ? Blake2bCore.Avx2Isa.RotateRight24 : Blake2bCore.Avx512Isa.RotateRight24,
            16 => avx2 ? Blake2bCore.Avx2Isa.RotateRight16 : Blake2bCore.Avx512Isa.RotateRight16,
            _ => avx2 ? Blake2bCore.Avx2Isa.RotateRight63 : Blake2bCore.Avx512Isa.RotateRight63,
        };

        var random = new Random(bits);
        ulong[] boundaries = [0, 1, 0xFFFF_FFFF, 0x8000_0000_0000_0000, 0x0123_4567_89AB_CDEF, ulong.MaxValue];
        for (int sample = 0; sample < 1000; sample++)
        {
            ulong[] words = sample < boundaries.Length
                ? [boundaries[sample], ~boundaries[sample], boundaries[sample] << 1, boundaries[sample] >> 1]
                : [(ulong)random.NextInt64(), (ulong)random.NextInt64() << 1, ~(ulong)random.NextInt64(), (ulong)random.NextInt64()];
            Vector256<ulong> expected = Vector256.Create(
                BitOperations.RotateRight(words[0], bits),
                BitOperations.RotateRight(words[1], bits),
                BitOperations.RotateRight(words[2], bits),
                BitOperations.RotateRight(words[3], bits));

            Assert.AreEqual(expected, rotate(Vector256.Create(words[0], words[1], words[2], words[3])), $"{words[0]:X16}");
        }
    }
}

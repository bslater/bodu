// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3CoreTests.IVector256Isa.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using System.Runtime.Intrinsics;

namespace Bodu.Security.Cryptography;

/// <summary>
/// The instruction-set shims of the 256-bit kernel: each rotation of each shim is held to its definition.
/// </summary>
public sealed partial class Blake3CoreTests
{
    /// <summary>
    /// Verifies that each shim rotates each word right by 16, 12, 8, and 7 bits.
    /// </summary>
    /// <param name="isa">The shim's instruction set.</param>
    /// <param name="bits">The rotation.</param>
    [TestMethod]
    [DataRow("Avx2", 16)]
    [DataRow("Avx2", 12)]
    [DataRow("Avx2", 8)]
    [DataRow("Avx2", 7)]
    [DataRow("Avx512", 16)]
    [DataRow("Avx512", 12)]
    [DataRow("Avx512", 8)]
    [DataRow("Avx512", 7)]
    public void RotateRight_WhenCountIsOneBlake3Uses_ForEachIsa_ShouldRotateEachWord(string isa, int bits)
    {
        Func<Vector256<uint>, Vector256<uint>> rotate = (ParseSupportedKernel(isa), bits) switch
        {
            (Blake3Core.KernelKind.Avx2, 16) => Blake3Core.Avx2Isa.RotateRight16,
            (Blake3Core.KernelKind.Avx2, 12) => Blake3Core.Avx2Isa.RotateRight12,
            (Blake3Core.KernelKind.Avx2, 8) => Blake3Core.Avx2Isa.RotateRight8,
            (Blake3Core.KernelKind.Avx2, _) => Blake3Core.Avx2Isa.RotateRight7,
            (_, 16) => Blake3Core.Avx512Isa.RotateRight16,
            (_, 12) => Blake3Core.Avx512Isa.RotateRight12,
            (_, 8) => Blake3Core.Avx512Isa.RotateRight8,
            _ => Blake3Core.Avx512Isa.RotateRight7,
        };

        var random = new Random(bits);
        for (int sample = 0; sample < 1000; sample++)
        {
            uint[] words = new uint[8];
            for (int i = 0; i < words.Length; i++)
                words[i] = sample == 0 ? (uint)i * 0x1111_1111U : (uint)random.Next() ^ ((uint)random.Next() << 31);

            uint[] expected = new uint[8];
            for (int i = 0; i < words.Length; i++)
                expected[i] = BitOperations.RotateRight(words[i], bits);

            Assert.AreEqual(Vector256.Create(expected), rotate(Vector256.Create(words)), $"sample {sample}");
        }
    }
}

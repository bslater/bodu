// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20CoreTests.IVector256Isa.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using System.Runtime.Intrinsics;

namespace Bodu.Security.Cryptography;

/// <summary>
/// The instruction-set shims of the 256-bit kernels: each rotation ChaCha20 and Salsa20 use, of each shim, is held to
/// its definition.
/// </summary>
public sealed partial class ChaCha20CoreTests
{
    /// <summary>
    /// Verifies that each 256-bit shim rotates every lane left by each count ChaCha20 (16, 12, 8, 7) and Salsa20 (7, 9,
    /// 13, 18) use, in both 128-bit halves.
    /// </summary>
    /// <param name="isa">The shim's instruction set.</param>
    /// <param name="bits">The rotation.</param>
    [TestMethod]
    [DataRow("Avx2", 7)]
    [DataRow("Avx2", 8)]
    [DataRow("Avx2", 9)]
    [DataRow("Avx2", 12)]
    [DataRow("Avx2", 13)]
    [DataRow("Avx2", 16)]
    [DataRow("Avx2", 18)]
    [DataRow("Avx512", 7)]
    [DataRow("Avx512", 8)]
    [DataRow("Avx512", 9)]
    [DataRow("Avx512", 12)]
    [DataRow("Avx512", 13)]
    [DataRow("Avx512", 16)]
    [DataRow("Avx512", 18)]
    public void RotateLeft_ForEach256BitIsa_ShouldRotateEveryLane(string isa, int bits)
    {
        Func<Vector256<uint>, int, Vector256<uint>> rotate = ParseSupportedKernel(isa) switch
        {
            ChaCha20Core.KernelKind.Avx2 => RotateLeft256<ChaCha20Core.Avx2Isa>,
            _ => RotateLeft256<ChaCha20Core.Avx512Isa>,
        };

        var random = new Random(bits + 256);
        for (int sample = 0; sample < 1000; sample++)
        {
            uint[] words = new uint[8];
            for (int i = 0; i < words.Length; i++)
                words[i] = sample == 0 ? (uint)(i + 1) * 0x0101_0101U : (uint)random.NextInt64(0, 1L << 32);

            uint[] expected = new uint[8];
            for (int i = 0; i < words.Length; i++)
                expected[i] = BitOperations.RotateLeft(words[i], bits);

            Assert.AreEqual(Vector256.Create(expected), rotate(Vector256.Create(words), bits), $"sample {sample}");
        }
    }

    /// <summary>
    /// Rotates every lane left through a 256-bit shim, by one of the counts the kernels use, each passed as the constant
    /// the shim expects.
    /// </summary>
    /// <typeparam name="TIsa">The shim.</typeparam>
    /// <param name="value">The lanes to rotate.</param>
    /// <param name="bits">The rotation: 7, 8, 9, 12, 13, 16 or 18.</param>
    /// <returns>The rotated lanes.</returns>
    private static Vector256<uint> RotateLeft256<TIsa>(Vector256<uint> value, int bits)
        where TIsa : struct, ChaCha20Core.IVector256Isa => bits switch
        {
            7 => TIsa.RotateLeft(value, 7),
            8 => TIsa.RotateLeft(value, 8),
            9 => TIsa.RotateLeft(value, 9),
            12 => TIsa.RotateLeft(value, 12),
            13 => TIsa.RotateLeft(value, 13),
            16 => TIsa.RotateLeft(value, 16),
            18 => TIsa.RotateLeft(value, 18),
            _ => throw new ArgumentOutOfRangeException(nameof(bits)),
        };
}

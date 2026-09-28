// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20CoreTests.IVector128Isa.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using System.Runtime.Intrinsics;

namespace Bodu.Security.Cryptography;

/// <summary>
/// The instruction-set shims of the 128-bit kernels: each rotation ChaCha20 and Salsa20 use, and the transpose, of each
/// shim is held to its definition.
/// </summary>
public sealed partial class ChaCha20CoreTests
{
    /// <summary>
    /// Verifies that each 128-bit shim rotates every lane left by each count ChaCha20 (16, 12, 8, 7) and Salsa20 (7, 9,
    /// 13, 18) use.
    /// </summary>
    /// <param name="isa">The shim's instruction set.</param>
    /// <param name="bits">The rotation.</param>
    [TestMethod]
    [DataRow("Ssse3", 7)]
    [DataRow("Ssse3", 8)]
    [DataRow("Ssse3", 9)]
    [DataRow("Ssse3", 12)]
    [DataRow("Ssse3", 13)]
    [DataRow("Ssse3", 16)]
    [DataRow("Ssse3", 18)]
    [DataRow("AdvSimd", 7)]
    [DataRow("AdvSimd", 8)]
    [DataRow("AdvSimd", 9)]
    [DataRow("AdvSimd", 12)]
    [DataRow("AdvSimd", 13)]
    [DataRow("AdvSimd", 16)]
    [DataRow("AdvSimd", 18)]
    [DataRow("Avx512", 7)]
    [DataRow("Avx512", 8)]
    [DataRow("Avx512", 9)]
    [DataRow("Avx512", 12)]
    [DataRow("Avx512", 13)]
    [DataRow("Avx512", 16)]
    [DataRow("Avx512", 18)]
    public void RotateLeft_WhenCountIsOneChaChaOrSalsaUses_ForEach128BitIsa_ShouldRotateEveryLane(string isa, int bits)
    {
        Func<Vector128<uint>, int, Vector128<uint>> rotate = ParseSupportedKernel(isa) switch
        {
            ChaCha20Core.KernelKind.Ssse3 => RotateLeft<ChaCha20Core.Ssse3Isa>,
            ChaCha20Core.KernelKind.AdvSimd => RotateLeft<ChaCha20Core.AdvSimdIsa>,
            _ => RotateLeft<ChaCha20Core.Avx512Isa>,
        };

        var random = new Random(bits);
        for (int sample = 0; sample < 1000; sample++)
        {
            uint[] words = new uint[4];
            for (int i = 0; i < words.Length; i++)
                words[i] = sample == 0 ? (uint)(i + 1) * 0x0101_0101U : (uint)random.NextInt64(0, 1L << 32);

            uint[] expected = new uint[4];
            for (int i = 0; i < words.Length; i++)
                expected[i] = BitOperations.RotateLeft(words[i], bits);

            Assert.AreEqual(Vector128.Create(expected), rotate(Vector128.Create(words), bits), $"sample {sample}");
        }
    }

    /// <summary>
    /// Verifies that each 128-bit shim transposes four rows of four words.
    /// </summary>
    /// <param name="isa">The shim's instruction set.</param>
    [TestMethod]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx512")]
    public void Transpose_WhenGivenFourRows_ForEach128BitIsa_ShouldTransposeTheRows(string isa)
    {
        Vector128<uint> row0 = Vector128.Create(0u, 1u, 2u, 3u);
        Vector128<uint> row1 = Vector128.Create(4u, 5u, 6u, 7u);
        Vector128<uint> row2 = Vector128.Create(8u, 9u, 10u, 11u);
        Vector128<uint> row3 = Vector128.Create(12u, 13u, 14u, 15u);

        switch (ParseSupportedKernel(isa))
        {
            case ChaCha20Core.KernelKind.Ssse3:
                ChaCha20Core.Ssse3Isa.Transpose(ref row0, ref row1, ref row2, ref row3);
                break;

            case ChaCha20Core.KernelKind.AdvSimd:
                ChaCha20Core.AdvSimdIsa.Transpose(ref row0, ref row1, ref row2, ref row3);
                break;

            default:
                ChaCha20Core.Avx512Isa.Transpose(ref row0, ref row1, ref row2, ref row3);
                break;
        }

        Assert.AreEqual(Vector128.Create(0u, 4u, 8u, 12u), row0);
        Assert.AreEqual(Vector128.Create(1u, 5u, 9u, 13u), row1);
        Assert.AreEqual(Vector128.Create(2u, 6u, 10u, 14u), row2);
        Assert.AreEqual(Vector128.Create(3u, 7u, 11u, 15u), row3);
    }

    /// <summary>
    /// Rotates every lane left through a 128-bit shim, by one of the counts the kernels use, each passed as the constant
    /// the shim expects.
    /// </summary>
    /// <typeparam name="TIsa">The shim.</typeparam>
    /// <param name="value">The lanes to rotate.</param>
    /// <param name="bits">The rotation: 7, 8, 9, 12, 13, 16 or 18.</param>
    /// <returns>The rotated lanes.</returns>
    private static Vector128<uint> RotateLeft<TIsa>(Vector128<uint> value, int bits)
        where TIsa : struct, ChaCha20Core.IVector128Isa => bits switch
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

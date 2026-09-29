// ---------------------------------------------------------------------------------------------------------------
// <copyright file="VectorExtensionsTests.RotateBitsLeftUnchecked.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using System.Runtime.Intrinsics;

namespace Bodu.Extensions;

public partial class VectorExtensionsTests
{
    /// <summary>
    /// Verifies that each instruction set's 128-bit rotation rotates every lane left by every count from 1 to 31,
    /// matching <see cref="BitOperations.RotateLeft(uint, int)" /> on each lane.
    /// </summary>
    /// <param name="isa">The <see cref="VectorRotation" /> implementation.</param>
    [TestMethod]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx512")]
    public void RotateBitsLeftUnchecked_WhenCountIsEachFromOneTo31_For128BitVectors_ShouldRotateEveryLaneLeft(string isa)
    {
        AssumeSupported(isa, 128);
        Func<Vector128<uint>, int, Vector128<uint>> rotate = isa switch
        {
            "Ssse3" => RotateLeft128<VectorRotation.Ssse3>,
            "AdvSimd" => RotateLeft128<VectorRotation.AdvSimd>,
            _ => RotateLeft128<VectorRotation.Avx512>,
        };

        var random = new Random(128);
        for (int count = 1; count < 32; count++)
        {
            for (int sample = 0; sample < 256; sample++)
            {
                uint[] lanes = NextLanes(random, 4, sample);
                uint[] expected = new uint[lanes.Length];
                for (int i = 0; i < lanes.Length; i++)
                    expected[i] = BitOperations.RotateLeft(lanes[i], count);

                Assert.AreEqual(Vector128.Create(expected), rotate(Vector128.Create(lanes), count), $"count {count}, sample {sample}");
            }
        }
    }

    /// <summary>
    /// Verifies that each instruction set's 256-bit rotation rotates every lane left by every count from 1 to 31,
    /// matching <see cref="BitOperations.RotateLeft(uint, int)" /> on each lane.
    /// </summary>
    /// <param name="isa">The <see cref="VectorRotation" /> implementation.</param>
    [TestMethod]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    public void RotateBitsLeftUnchecked_WhenCountIsEachFromOneTo31_For256BitVectors_ShouldRotateEveryLaneLeft(string isa)
    {
        AssumeSupported(isa, 256);
        Func<Vector256<uint>, int, Vector256<uint>> rotate = isa switch
        {
            "Avx2" => RotateLeft256<VectorRotation.Avx2>,
            _ => RotateLeft256<VectorRotation.Avx512>,
        };

        var random = new Random(256);
        for (int count = 1; count < 32; count++)
        {
            for (int sample = 0; sample < 256; sample++)
            {
                uint[] lanes = NextLanes(random, 8, sample);
                uint[] expected = new uint[lanes.Length];
                for (int i = 0; i < lanes.Length; i++)
                    expected[i] = BitOperations.RotateLeft(lanes[i], count);

                Assert.AreEqual(Vector256.Create(expected), rotate(Vector256.Create(lanes), count), $"count {count}, sample {sample}");
            }
        }
    }

    /// <summary>
    /// Verifies that each instruction set's 512-bit rotation rotates every lane left by every count from 1 to 31,
    /// matching <see cref="BitOperations.RotateLeft(uint, int)" /> on each lane.
    /// </summary>
    /// <param name="isa">The <see cref="VectorRotation" /> implementation.</param>
    [TestMethod]
    [DataRow("Avx512")]
    public void RotateBitsLeftUnchecked_WhenCountIsEachFromOneTo31_For512BitVectors_ShouldRotateEveryLaneLeft(string isa)
    {
        AssumeSupported(isa, 512);
        Func<Vector512<uint>, int, Vector512<uint>> rotate = RotateLeft512<VectorRotation.Avx512>;

        var random = new Random(512);
        for (int count = 1; count < 32; count++)
        {
            for (int sample = 0; sample < 256; sample++)
            {
                uint[] lanes = NextLanes(random, 16, sample);
                uint[] expected = new uint[lanes.Length];
                for (int i = 0; i < lanes.Length; i++)
                    expected[i] = BitOperations.RotateLeft(lanes[i], count);

                Assert.AreEqual(Vector512.Create(expected), rotate(Vector512.Create(lanes), count), $"count {count}, sample {sample}");
            }
        }
    }

    /// <summary>
    /// Rotates every lane of a 128-bit vector left through
    /// <see cref="VectorExtensions.RotateBitsLeftUnchecked{TIsa}(Vector128{uint}, byte)" />, passing the count as the
    /// constant the extension expects.
    /// </summary>
    /// <typeparam name="TIsa">The instruction set that performs the rotation.</typeparam>
    /// <param name="value">The lanes to rotate.</param>
    /// <param name="count">The number of bits, from 1 to 31.</param>
    /// <returns>The rotated lanes.</returns>
    private static Vector128<uint> RotateLeft128<TIsa>(Vector128<uint> value, int count)
        where TIsa : struct, IVector128Rotation => count switch
        {
            1 => value.RotateBitsLeftUnchecked<TIsa>(1),
            2 => value.RotateBitsLeftUnchecked<TIsa>(2),
            3 => value.RotateBitsLeftUnchecked<TIsa>(3),
            4 => value.RotateBitsLeftUnchecked<TIsa>(4),
            5 => value.RotateBitsLeftUnchecked<TIsa>(5),
            6 => value.RotateBitsLeftUnchecked<TIsa>(6),
            7 => value.RotateBitsLeftUnchecked<TIsa>(7),
            8 => value.RotateBitsLeftUnchecked<TIsa>(8),
            9 => value.RotateBitsLeftUnchecked<TIsa>(9),
            10 => value.RotateBitsLeftUnchecked<TIsa>(10),
            11 => value.RotateBitsLeftUnchecked<TIsa>(11),
            12 => value.RotateBitsLeftUnchecked<TIsa>(12),
            13 => value.RotateBitsLeftUnchecked<TIsa>(13),
            14 => value.RotateBitsLeftUnchecked<TIsa>(14),
            15 => value.RotateBitsLeftUnchecked<TIsa>(15),
            16 => value.RotateBitsLeftUnchecked<TIsa>(16),
            17 => value.RotateBitsLeftUnchecked<TIsa>(17),
            18 => value.RotateBitsLeftUnchecked<TIsa>(18),
            19 => value.RotateBitsLeftUnchecked<TIsa>(19),
            20 => value.RotateBitsLeftUnchecked<TIsa>(20),
            21 => value.RotateBitsLeftUnchecked<TIsa>(21),
            22 => value.RotateBitsLeftUnchecked<TIsa>(22),
            23 => value.RotateBitsLeftUnchecked<TIsa>(23),
            24 => value.RotateBitsLeftUnchecked<TIsa>(24),
            25 => value.RotateBitsLeftUnchecked<TIsa>(25),
            26 => value.RotateBitsLeftUnchecked<TIsa>(26),
            27 => value.RotateBitsLeftUnchecked<TIsa>(27),
            28 => value.RotateBitsLeftUnchecked<TIsa>(28),
            29 => value.RotateBitsLeftUnchecked<TIsa>(29),
            30 => value.RotateBitsLeftUnchecked<TIsa>(30),
            31 => value.RotateBitsLeftUnchecked<TIsa>(31),
            _ => throw new ArgumentOutOfRangeException(nameof(count)),
        };

    /// <summary>
    /// Rotates every lane of a 256-bit vector left through
    /// <see cref="VectorExtensions.RotateBitsLeftUnchecked{TIsa}(Vector256{uint}, byte)" />, passing the count as the
    /// constant the extension expects.
    /// </summary>
    /// <typeparam name="TIsa">The instruction set that performs the rotation.</typeparam>
    /// <param name="value">The lanes to rotate.</param>
    /// <param name="count">The number of bits, from 1 to 31.</param>
    /// <returns>The rotated lanes.</returns>
    private static Vector256<uint> RotateLeft256<TIsa>(Vector256<uint> value, int count)
        where TIsa : struct, IVector256Rotation => count switch
        {
            1 => value.RotateBitsLeftUnchecked<TIsa>(1),
            2 => value.RotateBitsLeftUnchecked<TIsa>(2),
            3 => value.RotateBitsLeftUnchecked<TIsa>(3),
            4 => value.RotateBitsLeftUnchecked<TIsa>(4),
            5 => value.RotateBitsLeftUnchecked<TIsa>(5),
            6 => value.RotateBitsLeftUnchecked<TIsa>(6),
            7 => value.RotateBitsLeftUnchecked<TIsa>(7),
            8 => value.RotateBitsLeftUnchecked<TIsa>(8),
            9 => value.RotateBitsLeftUnchecked<TIsa>(9),
            10 => value.RotateBitsLeftUnchecked<TIsa>(10),
            11 => value.RotateBitsLeftUnchecked<TIsa>(11),
            12 => value.RotateBitsLeftUnchecked<TIsa>(12),
            13 => value.RotateBitsLeftUnchecked<TIsa>(13),
            14 => value.RotateBitsLeftUnchecked<TIsa>(14),
            15 => value.RotateBitsLeftUnchecked<TIsa>(15),
            16 => value.RotateBitsLeftUnchecked<TIsa>(16),
            17 => value.RotateBitsLeftUnchecked<TIsa>(17),
            18 => value.RotateBitsLeftUnchecked<TIsa>(18),
            19 => value.RotateBitsLeftUnchecked<TIsa>(19),
            20 => value.RotateBitsLeftUnchecked<TIsa>(20),
            21 => value.RotateBitsLeftUnchecked<TIsa>(21),
            22 => value.RotateBitsLeftUnchecked<TIsa>(22),
            23 => value.RotateBitsLeftUnchecked<TIsa>(23),
            24 => value.RotateBitsLeftUnchecked<TIsa>(24),
            25 => value.RotateBitsLeftUnchecked<TIsa>(25),
            26 => value.RotateBitsLeftUnchecked<TIsa>(26),
            27 => value.RotateBitsLeftUnchecked<TIsa>(27),
            28 => value.RotateBitsLeftUnchecked<TIsa>(28),
            29 => value.RotateBitsLeftUnchecked<TIsa>(29),
            30 => value.RotateBitsLeftUnchecked<TIsa>(30),
            31 => value.RotateBitsLeftUnchecked<TIsa>(31),
            _ => throw new ArgumentOutOfRangeException(nameof(count)),
        };

    /// <summary>
    /// Rotates every lane of a 512-bit vector left through
    /// <see cref="VectorExtensions.RotateBitsLeftUnchecked{TIsa}(Vector512{uint}, byte)" />, passing the count as the
    /// constant the extension expects.
    /// </summary>
    /// <typeparam name="TIsa">The instruction set that performs the rotation.</typeparam>
    /// <param name="value">The lanes to rotate.</param>
    /// <param name="count">The number of bits, from 1 to 31.</param>
    /// <returns>The rotated lanes.</returns>
    private static Vector512<uint> RotateLeft512<TIsa>(Vector512<uint> value, int count)
        where TIsa : struct, IVector512Rotation => count switch
        {
            1 => value.RotateBitsLeftUnchecked<TIsa>(1),
            2 => value.RotateBitsLeftUnchecked<TIsa>(2),
            3 => value.RotateBitsLeftUnchecked<TIsa>(3),
            4 => value.RotateBitsLeftUnchecked<TIsa>(4),
            5 => value.RotateBitsLeftUnchecked<TIsa>(5),
            6 => value.RotateBitsLeftUnchecked<TIsa>(6),
            7 => value.RotateBitsLeftUnchecked<TIsa>(7),
            8 => value.RotateBitsLeftUnchecked<TIsa>(8),
            9 => value.RotateBitsLeftUnchecked<TIsa>(9),
            10 => value.RotateBitsLeftUnchecked<TIsa>(10),
            11 => value.RotateBitsLeftUnchecked<TIsa>(11),
            12 => value.RotateBitsLeftUnchecked<TIsa>(12),
            13 => value.RotateBitsLeftUnchecked<TIsa>(13),
            14 => value.RotateBitsLeftUnchecked<TIsa>(14),
            15 => value.RotateBitsLeftUnchecked<TIsa>(15),
            16 => value.RotateBitsLeftUnchecked<TIsa>(16),
            17 => value.RotateBitsLeftUnchecked<TIsa>(17),
            18 => value.RotateBitsLeftUnchecked<TIsa>(18),
            19 => value.RotateBitsLeftUnchecked<TIsa>(19),
            20 => value.RotateBitsLeftUnchecked<TIsa>(20),
            21 => value.RotateBitsLeftUnchecked<TIsa>(21),
            22 => value.RotateBitsLeftUnchecked<TIsa>(22),
            23 => value.RotateBitsLeftUnchecked<TIsa>(23),
            24 => value.RotateBitsLeftUnchecked<TIsa>(24),
            25 => value.RotateBitsLeftUnchecked<TIsa>(25),
            26 => value.RotateBitsLeftUnchecked<TIsa>(26),
            27 => value.RotateBitsLeftUnchecked<TIsa>(27),
            28 => value.RotateBitsLeftUnchecked<TIsa>(28),
            29 => value.RotateBitsLeftUnchecked<TIsa>(29),
            30 => value.RotateBitsLeftUnchecked<TIsa>(30),
            31 => value.RotateBitsLeftUnchecked<TIsa>(31),
            _ => throw new ArgumentOutOfRangeException(nameof(count)),
        };
}

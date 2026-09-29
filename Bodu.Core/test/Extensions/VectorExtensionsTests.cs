// ---------------------------------------------------------------------------------------------------------------
// <copyright file="VectorExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Extensions;

/// <summary>
/// Tests for <see cref="VectorExtensions" />, grouped into member-named partial files. Each rotation is driven through
/// every <see cref="VectorRotation" /> implementation the processor supports, whichever one a kernel would pick, and
/// held lane by lane to the scalar rotation.
/// </summary>
[TestClass]
public partial class VectorExtensionsTests
{
    /// <summary>
    /// Returns the lanes of one sample: a fixed pattern in which every byte differs for the first sample, and seeded
    /// random words after it.
    /// </summary>
    /// <param name="random">The source of the random words.</param>
    /// <param name="count">The number of lanes.</param>
    /// <param name="sample">The index of the sample.</param>
    /// <returns>The lanes.</returns>
    private static uint[] NextLanes(Random random, int count, int sample)
    {
        uint[] lanes = new uint[count];
        for (int i = 0; i < lanes.Length; i++)
            lanes[i] = sample == 0 ? unchecked((uint)(i + 1) * 0x0403_0201U) : (uint)random.NextInt64(0, 1L << 32);

        return lanes;
    }

    /// <summary>
    /// Returns the 64-bit lanes of one sample: a fixed pattern in which every byte differs for the first sample, and
    /// seeded random words after it.
    /// </summary>
    /// <param name="random">The source of the random words.</param>
    /// <param name="count">The number of lanes.</param>
    /// <param name="sample">The index of the sample.</param>
    /// <returns>The lanes.</returns>
    private static ulong[] NextWideLanes(Random random, int count, int sample)
    {
        ulong[] lanes = new ulong[count];
        for (int i = 0; i < lanes.Length; i++)
            lanes[i] = sample == 0 ? unchecked((ulong)(i + 1) * 0x0807_0605_0403_0201UL) : ((ulong)random.NextInt64() << 1) ^ (ulong)random.Next(2);

        return lanes;
    }

    /// <summary>
    /// Marks the test inconclusive when the processor lacks the instruction set that a
    /// <see cref="VectorRotation" /> implementation needs at a vector width.
    /// </summary>
    /// <param name="isa">The implementation's name: <c>Ssse3</c>, <c>AdvSimd</c>, <c>Avx2</c> or <c>Avx512</c>.</param>
    /// <param name="width">The vector width, in bits.</param>
    private static void AssumeSupported(string isa, int width)
    {
        bool supported = isa switch
        {
            "Ssse3" => Ssse3.IsSupported,
            "AdvSimd" => AdvSimd.Arm64.IsSupported,
            "Avx2" => Avx2.IsSupported,
            "Avx512" => width == 512 ? Avx512F.IsSupported : Avx512F.VL.IsSupported,
            _ => throw new ArgumentOutOfRangeException(nameof(isa)),
        };

        if (!supported)
            Assert.Inconclusive($"The {isa} rotation of {width}-bit vectors cannot run on this processor.");
    }
}

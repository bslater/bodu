// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2CoreTests.IVector256Isa.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using System.Runtime.Intrinsics;

namespace Bodu.Security.Cryptography;

/// <summary>
/// The instruction-set shims of the 256-bit kernel: each shim's rotation by 63 bits is held to its scalar definition.
/// </summary>
public sealed partial class Argon2CoreTests
{
    /// <summary>
    /// Verifies that each shim rotates each word right by 63 bits.
    /// </summary>
    /// <param name="isa">The shim's instruction set, by the name of the kernel it serves.</param>
    [TestMethod]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    public void RotateRight63_WhenGivenBoundaryAndRandomWords_ForEachIsa_ShouldRotateEachWord(string isa)
    {
        Func<Vector256<ulong>, Vector256<ulong>> rotate = ParseSupportedKernel(isa) == Argon2Core.KernelKind.Avx512
            ? Argon2Core.Avx512Isa.RotateRight63
            : Argon2Core.Avx2Isa.RotateRight63;

        foreach ((ulong x0, ulong x1, ulong y0, ulong y1) in ShimOperands())
        {
            Vector256<ulong> expected = Vector256.Create(
                BitOperations.RotateRight(x0, 63),
                BitOperations.RotateRight(x1, 63),
                BitOperations.RotateRight(y0, 63),
                BitOperations.RotateRight(y1, 63));

            Assert.AreEqual(expected, rotate(Vector256.Create(x0, x1, y0, y1)), $"{x0:X16} {x1:X16} {y0:X16} {y1:X16}");
        }
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20CoreTests.Transpose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography;

/// <summary>
/// The 4×4 transpose the 128-bit kernels share, held to its definition on whichever architecture runs the tests: SSE2
/// unpacks on x64, and zips on ARM64.
/// </summary>
public sealed partial class ChaCha20CoreTests
{
    /// <summary>
    /// Verifies that <see cref="ChaCha20Core.Transpose" /> replaces each of four rows of four words with the matching
    /// column.
    /// </summary>
    [TestMethod]
    public void Transpose_WhenGivenFourRows_ShouldReplaceEachRowWithTheMatchingColumn()
    {
        if (!Sse2.IsSupported && !AdvSimd.Arm64.IsSupported)
            Assert.Inconclusive("The transpose needs SSE2 or ARM64 AdvSimd.");

        Vector128<uint> row0 = Vector128.Create(0u, 1u, 2u, 3u);
        Vector128<uint> row1 = Vector128.Create(4u, 5u, 6u, 7u);
        Vector128<uint> row2 = Vector128.Create(8u, 9u, 10u, 11u);
        Vector128<uint> row3 = Vector128.Create(12u, 13u, 14u, 15u);

        ChaCha20Core.Transpose(ref row0, ref row1, ref row2, ref row3);

        Assert.AreEqual(Vector128.Create(0u, 4u, 8u, 12u), row0);
        Assert.AreEqual(Vector128.Create(1u, 5u, 9u, 13u), row1);
        Assert.AreEqual(Vector128.Create(2u, 6u, 10u, 14u), row2);
        Assert.AreEqual(Vector128.Create(3u, 7u, 11u, 15u), row3);
    }
}

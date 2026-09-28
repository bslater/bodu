// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScryptCoreTests.IVector128Isa.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.Intrinsics;

namespace Bodu.Security.Cryptography;

/// <summary>
/// The instruction-set shims of the 128-bit kernel: each lane rotation of each shim is held to its definition, so a
/// shim that runs only on ARM64 is checked rotation by rotation wherever that hardware runs the suite.
/// </summary>
public sealed partial class ScryptCoreTests
{
    /// <summary>
    /// Verifies that each shim rotates the four lanes toward lane 0 by one, two, and three places.
    /// </summary>
    /// <param name="isa">The shim's instruction set.</param>
    /// <param name="places">The number of places.</param>
    [TestMethod]
    [DataRow("Sse2", 1)]
    [DataRow("Sse2", 2)]
    [DataRow("Sse2", 3)]
    [DataRow("AdvSimd", 1)]
    [DataRow("AdvSimd", 2)]
    [DataRow("AdvSimd", 3)]
    public void RotateLanes_ForEachIsa_ShouldTakeEachLaneFromItsSuccessor(string isa, int places)
    {
        bool sse2 = ParseSupportedKernel(isa) == ScryptCore.KernelKind.Sse2;
        Func<Vector128<uint>, Vector128<uint>> rotate = places switch
        {
            1 => sse2 ? ScryptCore.Sse2Isa.RotateLanes1 : ScryptCore.AdvSimdIsa.RotateLanes1,
            2 => sse2 ? ScryptCore.Sse2Isa.RotateLanes2 : ScryptCore.AdvSimdIsa.RotateLanes2,
            _ => sse2 ? ScryptCore.Sse2Isa.RotateLanes3 : ScryptCore.AdvSimdIsa.RotateLanes3,
        };

        uint[] lanes = [0x0102_0304, 0x1112_1314, 0x2122_2324, 0x3132_3334];
        Vector128<uint> expected = Vector128.Create(
            lanes[places % 4],
            lanes[(1 + places) % 4],
            lanes[(2 + places) % 4],
            lanes[(3 + places) % 4]);

        Assert.AreEqual(expected, rotate(Vector128.Create(lanes[0], lanes[1], lanes[2], lanes[3])));
    }
}

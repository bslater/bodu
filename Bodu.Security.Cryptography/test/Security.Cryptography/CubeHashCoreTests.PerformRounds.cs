// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CubeHashCoreTests.PerformRounds.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class CubeHashCoreTests
{
    /// <summary>
    /// Verifies that each vector kernel leaves the state the scalar kernel leaves, over seeded states and round counts
    /// from none to the 160 of a default-parameter finalization.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    public void PerformRounds_ForEachKernel_ShouldMatchTheScalarKernel(string kernel)
    {
        CubeHashCore.KernelKind kind = ParseSupportedKernel(kernel);
        var random = new Random(0x0C0B_0001);

        foreach (int rounds in new[] { 0, 1, 2, 3, 16, 32, 160 })
        {
            for (int iteration = 0; iteration < 32; iteration++)
            {
                uint[] expected = NextState(random);
                uint[] actual = (uint[])expected.Clone();

                CubeHashCore.PerformRounds(CubeHashCore.KernelKind.Scalar, expected, rounds);
                CubeHashCore.PerformRounds(kind, actual, rounds);

                CollectionAssert.AreEqual(expected, actual, $"{rounds} rounds, iteration {iteration}");
            }
        }
    }

    /// <summary>
    /// Verifies that the rounds apply to the first 32 words of a longer span and leave the words after them alone.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    public void PerformRounds_ForEachKernel_WhenSpanIsLonger_ShouldLeaveTheWordsAfterTheStateAlone(string kernel)
    {
        CubeHashCore.KernelKind kind = ParseSupportedKernel(kernel);
        uint[] state = NextState(new Random(0x0C0B_0002));
        uint[] expected = (uint[])state.Clone();
        uint[] longer = [.. state, 0xDEADBEEFu, 0x01234567u];

        CubeHashCore.PerformRounds(CubeHashCore.KernelKind.Scalar, expected, 16);
        CubeHashCore.PerformRounds(kind, longer, 16);

        CollectionAssert.AreEqual(expected.Concat([0xDEADBEEFu, 0x01234567u]).ToArray(), longer);
    }

    /// <summary>
    /// Verifies that the rounds reject a span shorter than the state with <see cref="ArgumentOutOfRangeException" />
    /// naming it, rather than reading or writing past its end.
    /// </summary>
    [TestMethod]
    public void PerformRounds_WhenStateIsShorterThan32Words_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            CubeHashCore.PerformRounds(new uint[CubeHashCore.StateWords - 1], 1);
        });

        Assert.AreEqual("state", ex.ParamName);
    }
}

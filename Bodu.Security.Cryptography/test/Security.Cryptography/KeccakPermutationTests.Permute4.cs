// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeccakPermutationTests.Permute4.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Runtime.Intrinsics;

namespace Bodu.Security.Cryptography;

public sealed partial class KeccakPermutationTests
{
    /// <summary>
    /// Verifies that each four-way kernel leaves every one of four seeded random states as the scalar permutation
    /// leaves it, over three consecutive permutations so an error in any round compounds rather than cancels.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    public void Permute4_WhenStatesAreSeededRandom_ForEachKernel_ShouldMatchTheScalarPermutation(string kernel)
    {
        KeccakPermutation.KernelKind kind = ParseSupportedKernel(kernel);
        var random = new Random(0x5EED_4400);
        ulong[][] expected = new ulong[4][];
        Vector256<ulong>[] actual = new Vector256<ulong>[KeccakPermutation.StateWords];

        for (int trial = 0; trial < 500; trial++)
        {
            for (int state = 0; state < expected.Length; state++)
            {
                expected[state] = new ulong[KeccakPermutation.StateWords];
                for (int lane = 0; lane < KeccakPermutation.StateWords; lane++)
                    expected[state][lane] = (ulong)random.NextInt64() ^ ((ulong)random.Next() << 63);
            }

            Interleave(expected, actual);
            for (int repeat = 0; repeat < 3; repeat++)
            {
                KeccakPermutation.Permute4(kind, actual);
                foreach (ulong[] state in expected)
                    KeccakPermutation.Permute(state);
            }

            AssertInterleaved(expected, actual, $"trial {trial}");
        }
    }

    /// <summary>
    /// Verifies that each four-way kernel permutes four different uniform states, all zero, all one and two patterns,
    /// as the scalar permutation does, so a lane taken from the wrong state shows.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    public void Permute4_WhenStatesAreUniformButDistinct_ForEachKernel_ShouldMatchTheScalarPermutation(string kernel)
    {
        KeccakPermutation.KernelKind kind = ParseSupportedKernel(kernel);
        ulong[] fills = [0UL, ulong.MaxValue, 0x5555_5555_5555_5555UL, 0x0123_4567_89AB_CDEFUL];
        ulong[][] expected = fills.Select(fill => Enumerable.Repeat(fill, KeccakPermutation.StateWords).ToArray()).ToArray();
        Vector256<ulong>[] actual = new Vector256<ulong>[KeccakPermutation.StateWords];
        Interleave(expected, actual);

        for (int repeat = 0; repeat < 4; repeat++)
        {
            KeccakPermutation.Permute4(kind, actual);
            foreach (ulong[] state in expected)
                KeccakPermutation.Permute(state);

            AssertInterleaved(expected, actual, $"permutation {repeat + 1}");
        }
    }

    /// <summary>
    /// Verifies that dispatch, through the overload without a kernel, permutes four states as the scalar permutation
    /// does.
    /// </summary>
    [TestMethod]
    public void Permute4_WhenKernelIsDispatched_ShouldMatchTheScalarPermutation()
    {
        var random = new Random(0x5EED_4401);
        ulong[][] expected = new ulong[4][];
        for (int state = 0; state < expected.Length; state++)
            expected[state] = Enumerable.Range(0, KeccakPermutation.StateWords).Select(_ => (ulong)random.NextInt64()).ToArray();

        Vector256<ulong>[] actual = new Vector256<ulong>[KeccakPermutation.StateWords];
        Interleave(expected, actual);

        KeccakPermutation.Permute4(actual);
        foreach (ulong[] state in expected)
            KeccakPermutation.Permute(state);

        AssertInterleaved(expected, actual, "dispatched");
    }

    /// <summary>
    /// Verifies that interleaved states that are not exactly 25 vectors are rejected with
    /// <see cref="ArgumentException" /> naming the parameter.
    /// </summary>
    /// <param name="length">The rejected length.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(24)]
    [DataRow(26)]
    public void Permute4_WhenStatesLengthIsNot25_ShouldThrowArgumentException(int length)
    {
        Vector256<ulong>[] states = new Vector256<ulong>[length];

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            KeccakPermutation.Permute4(KeccakPermutation.KernelKind.Scalar, states);
        });

        Assert.AreEqual("states", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the vector kernel's permutation, whichever rotation it is compiled over, forbids inlining and is
    /// aggressively optimized, so it is compiled on its own rather than into the dispatcher.
    /// </summary>
    /// <remarks>
    /// A kernel that cannot be inlined is compiled on its own, with its own inlining budget, whatever dynamic PGO makes
    /// of the dispatcher; one that is also aggressively optimized is compiled fully optimized at its first call.
    /// </remarks>
    [TestMethod]
    public void Permute4_WhenDeclared_ForTheVectorKernel_ShouldForbidInliningIntoTheDispatcher()
    {
        MethodInfo kernel = typeof(KeccakPermutation.Vector256Kernel<>).GetMethod("Permute", BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.IsTrue(kernel.MethodImplementationFlags.HasFlag(MethodImplAttributes.NoInlining));
        Assert.IsTrue(kernel.MethodImplementationFlags.HasFlag(MethodImplAttributes.AggressiveOptimization));
    }

    /// <summary>
    /// Interleaves four states, state j's lane i into element j of vector i.
    /// </summary>
    /// <param name="states">The four states.</param>
    /// <param name="interleaved">The 25 vectors receiving them.</param>
    private static void Interleave(ulong[][] states, Vector256<ulong>[] interleaved)
    {
        for (int lane = 0; lane < KeccakPermutation.StateWords; lane++)
            interleaved[lane] = Vector256.Create(states[0][lane], states[1][lane], states[2][lane], states[3][lane]);
    }

    /// <summary>
    /// Asserts that interleaved vectors hold four states lane for lane.
    /// </summary>
    /// <param name="expected">The four states.</param>
    /// <param name="actual">The 25 interleaved vectors.</param>
    /// <param name="context">The failure context.</param>
    private static void AssertInterleaved(ulong[][] expected, Vector256<ulong>[] actual, string context)
    {
        for (int state = 0; state < expected.Length; state++)
        {
            ulong[] lanes = actual.Select(vector => vector.GetElement(state)).ToArray();
            CollectionAssert.AreEqual(expected[state], lanes, $"{context}, state {state}");
        }
    }
}

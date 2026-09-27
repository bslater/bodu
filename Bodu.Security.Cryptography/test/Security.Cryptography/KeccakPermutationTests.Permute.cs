// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeccakPermutationTests.Permute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class KeccakPermutationTests
{
    /// <summary>
    /// Verifies that the permutation matches the loop-based reference on a thousand seeded random states, each run
    /// through three consecutive permutations so an error in any round compounds rather than cancels.
    /// </summary>
    [TestMethod]
    public void Permute_WhenStateIsSeededRandom_ShouldMatchReferenceImplementation()
    {
        var random = new Random(0x5EED_1600);
        ulong[] actual = new ulong[KeccakPermutation.StateWords];
        ulong[] expected = new ulong[KeccakPermutation.StateWords];

        for (int trial = 0; trial < 1000; trial++)
        {
            for (int lane = 0; lane < actual.Length; lane++)
                actual[lane] = expected[lane] = (ulong)random.NextInt64() ^ ((ulong)random.Next() << 63);

            for (int repeat = 0; repeat < 3; repeat++)
            {
                KeccakPermutation.Permute(actual);
                KeccakReference.Permute(expected);
            }

            CollectionAssert.AreEqual(expected, actual, $"trial {trial}");
        }
    }

    /// <summary>
    /// Verifies that the permutation matches the reference on the all-zero and all-one states, where every lane holds
    /// the same value and a misplaced lane cannot show.
    /// </summary>
    /// <param name="fill">The value every lane starts with.</param>
    [TestMethod]
    [DataRow(0UL)]
    [DataRow(ulong.MaxValue)]
    public void Permute_WhenEveryLaneIsEqual_ShouldMatchReferenceImplementation(ulong fill)
    {
        ulong[] actual = new ulong[KeccakPermutation.StateWords];
        ulong[] expected = new ulong[KeccakPermutation.StateWords];
        Array.Fill(actual, fill);
        Array.Fill(expected, fill);

        for (int repeat = 0; repeat < 4; repeat++)
        {
            KeccakPermutation.Permute(actual);
            KeccakReference.Permute(expected);
            CollectionAssert.AreEqual(expected, actual, $"permutation {repeat + 1}");
        }
    }

    /// <summary>
    /// Verifies that a state that is not exactly 25 lanes is rejected with <see cref="ArgumentException" /> naming the
    /// parameter.
    /// </summary>
    /// <param name="length">The rejected state length.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(24)]
    [DataRow(26)]
    public void Permute_WhenStateLengthIsNot25_ShouldThrowArgumentException(int length)
    {
        ulong[] state = new ulong[length];

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            KeccakPermutation.Permute(state);
        });

        Assert.AreEqual("state", ex.ParamName);
    }
}

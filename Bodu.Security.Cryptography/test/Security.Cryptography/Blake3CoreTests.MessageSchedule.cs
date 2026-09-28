// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3CoreTests.MessageSchedule.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class Blake3CoreTests
{
    /// <summary>
    /// Verifies that the schedule holds seven rounds of sixteen indices, the first the block as given and each later
    /// one the BLAKE3 message permutation applied once more.
    /// </summary>
    [TestMethod]
    public void MessageSchedule_WhenReadRoundByRound_ShouldApplyThePermutationOnceMore()
    {
        ReadOnlySpan<byte> permutation = [2, 6, 3, 10, 7, 0, 4, 13, 1, 11, 12, 5, 9, 14, 15, 8];
        ReadOnlySpan<byte> schedule = Blake3Core.MessageSchedule;

        Assert.AreEqual(7 * 16, schedule.Length);

        byte[] expected = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15];
        for (int round = 0; round < 7; round++)
        {
            CollectionAssert.AreEqual(expected, schedule.Slice(round * 16, 16).ToArray(), $"round {round}");

            byte[] next = new byte[16];
            for (int i = 0; i < next.Length; i++)
                next[i] = expected[permutation[i]];

            expected = next;
        }
    }
}

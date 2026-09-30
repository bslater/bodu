// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CounterKeystreamTests.TransformBigEndian128.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class CounterKeystreamTests
{
    /// <summary>
    /// Verifies that the big-endian 128-bit counter of EAX and SIV gives the output of a block-at-a-time reference over
    /// lengths either side of the block, of a group of blocks and of a run of counters, from a counter of zero and from
    /// counters just below the carries out of its lowest 32-bit word, out of its lower half, and out of its top, where it
    /// wraps to zero.
    /// </summary>
    /// <param name="name">The cipher's name, for the test's display.</param>
    /// <param name="create">Creates a fresh keyed cipher.</param>
    [TestMethod]
    [DynamicData(nameof(Ciphers))]
    public void TransformBigEndian128_ForLengthsAndCounterCarries_ShouldMatchBlockAtATimeReference(string name, Func<IBlockCipher> create)
    {
        using IBlockCipher cipher = create();
        int[] lengths = [0, 1, 15, 16, 17, 63, 64, 65, 127, 128, 129, (16 * 13) + 7, 4096, 4097, (3 * 4096) + 17];

        foreach ((string counterName, byte[] initialCounter) in InitialCounters())
        {
            foreach (int length in lengths)
            {
                byte[] input = new byte[length];
                new Random(length).NextBytes(input);
                byte[] expected = ReferenceBigEndian128(cipher, initialCounter, input);

                byte[] actual = new byte[length];
                CounterKeystream.TransformBigEndian128(cipher, initialCounter, input, actual);

                CollectionAssert.AreEqual(expected, actual, $"{name}, {counterName}, {length} bytes");
            }
        }
    }

    /// <summary>
    /// Verifies that the big-endian 128-bit counter transforms its input in place, into the memory that holds it, as a
    /// block-at-a-time reference does.
    /// </summary>
    /// <param name="name">The cipher's name, for the test's display.</param>
    /// <param name="create">Creates a fresh keyed cipher.</param>
    [TestMethod]
    [DynamicData(nameof(Ciphers))]
    public void TransformBigEndian128_WhenOutputIsTheInput_ShouldMatchBlockAtATimeReference(string name, Func<IBlockCipher> create)
    {
        using IBlockCipher cipher = create();
        byte[] initialCounter = new byte[16];
        initialCounter[^1] = 0xF9;

        byte[] input = new byte[(16 * 21) + 5];
        new Random(0x5E1B).NextBytes(input);
        byte[] expected = ReferenceBigEndian128(cipher, initialCounter, input);

        byte[] buffer = (byte[])input.Clone();
        CounterKeystream.TransformBigEndian128(cipher, initialCounter, buffer, buffer);

        CollectionAssert.AreEqual(expected, buffer, name);
    }

    /// <summary>
    /// Yields the initial counters the tests start from: zero, and values a few blocks below each carry of the counter.
    /// </summary>
    /// <returns>The counters, each named for its failure messages.</returns>
    private static IEnumerable<(string Name, byte[] Counter)> InitialCounters()
    {
        yield return ("zero", new byte[16]);

        byte[] word = new byte[16];
        word[0] = 0x3C;
        word.AsSpan(12).Fill(0xFF);
        word[^1] = 0xFB;
        yield return ("below the carry out of the lowest word", word);

        byte[] half = new byte[16];
        half[0] = 0x3C;
        half.AsSpan(8).Fill(0xFF);
        half[^1] = 0xFB;
        yield return ("below the carry out of the lower half", half);

        byte[] top = new byte[16];
        top.AsSpan().Fill(0xFF);
        top[^1] = 0xFB;
        yield return ("below the wrap to zero", top);
    }
}

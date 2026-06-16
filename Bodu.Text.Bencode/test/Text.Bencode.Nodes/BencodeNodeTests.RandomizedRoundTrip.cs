// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeNodeTests.RandomizedRoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Nodes;

/// <summary>
/// Verifies, over randomized node trees, that writing and re-reading preserves structure and canonical bytes —
/// insurance for the streaming writer's buffering and sorting logic across arbitrary shapes.
/// </summary>
public partial class BencodeNodeTests
{
    /// <summary>
    /// Verifies that randomly generated node trees survive a write/parse round trip with deep equality, and that
    /// re-serializing the parsed tree reproduces the canonical bytes exactly. The generator is seeded so failures
    /// reproduce deterministically.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void RoundTrip_WhenRandomizedNodeTrees_ShouldPreserveStructureAndCanonicalBytes()
    {
        var random = new Random(20260611);

        for (int iteration = 0; iteration < 1000; iteration++)
        {
            BencodeNode original = CreateRandomNode(random, depth: 0);

            byte[] encoded = original.ToByteArray();
            var reparsed = BencodeNode.Parse(encoded);

            Assert.IsTrue(BencodeNode.DeepEquals(original, reparsed), $"Iteration {iteration} did not round-trip.");
            CollectionAssert.AreEqual(encoded, reparsed!.ToByteArray(), $"Iteration {iteration} bytes diverged.");
        }
    }

    /// <summary>
    /// Creates a random node, biasing towards scalars as the depth grows so trees terminate.
    /// </summary>
    /// <param name="random">The seeded random source.</param>
    /// <param name="depth">The current nesting depth.</param>
    /// <returns>A randomly shaped node.</returns>
    private static BencodeNode CreateRandomNode(Random random, int depth)
    {
        // Above depth 5 only scalars are produced, bounding both tree size and nesting.
        int kind = depth >= 5 ? random.Next(2) : random.Next(6);
        switch (kind)
        {
            case 0:
                return BencodeValue.Create(random.NextInt64(long.MinValue, long.MaxValue));

            case 1:
                byte[] content = new byte[random.Next(0, 24)];
                random.NextBytes(content);
                return BencodeValue.Create(content);

            case 2:
            case 3:
            {
                var array = new BencodeArray();
                int count = random.Next(0, 5);
                for (int i = 0; i < count; i++)
                    array.Add(CreateRandomNode(random, depth + 1));

                return array;
            }

            default:
            {
                var obj = new BencodeObject();
                int count = random.Next(0, 5);
                for (int i = 0; i < count; i++)
                    obj[RandomKey(random)] = CreateRandomNode(random, depth + 1);

                return obj;
            }
        }
    }

    /// <summary>
    /// Creates a random ASCII key of length 0 through 7.
    /// </summary>
    /// <param name="random">The seeded random source.</param>
    /// <returns>The key text.</returns>
    private static string RandomKey(Random random)
    {
        int length = random.Next(0, 8);
        char[] chars = new char[length];
        for (int i = 0; i < length; i++)
            chars[i] = (char)random.Next(0x20, 0x7F);

        return new string(chars);
    }
}

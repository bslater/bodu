// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RadixTrieTests.DifferentialSweep.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Collections.Generic.Trees;

public sealed partial class RadixTrieTests
{
    /// <summary>
    /// Verifies that 10,000 seeded add / remove / contains / prefix operations on random short strings agree
    /// operation-for-operation with the uncompressed <see cref="Trie" /> oracle, with periodic full-content
    /// checkpoints — exercising every edge split and merge path the compressed representation can take.
    /// </summary>
    /// <param name="seed">The seed for the deterministic operation generator.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(20260705)]
    [DataRow(42)]
    [DataRow(987654321)]
    public void Members_WhenSweptAgainstTrieOracle_ShouldAgree(int seed)
    {
        const string alphabet = "abc";
        const int operations = 10_000;
        var random = new Random(seed);
        var sut = new RadixTrie();
        var oracle = new Trie();

        string NextKey()
        {
            int length = random.Next(0, 7);
            var builder = new StringBuilder(length);
            for (int i = 0; i < length; i++)
                builder.Append(alphabet[random.Next(alphabet.Length)]);
            return builder.ToString();
        }

        for (int op = 0; op < operations; op++)
        {
            string key = NextKey();
            switch (random.Next(4))
            {
                case 0:
                    Assert.AreEqual(oracle.Add(key), sut.Add(key), $"Add('{key}') diverged at op {op} (seed {seed}).");
                    break;

                case 1:
                    Assert.AreEqual(oracle.Remove(key), sut.Remove(key), $"Remove('{key}') diverged at op {op} (seed {seed}).");
                    break;

                case 2:
                    Assert.AreEqual(oracle.Contains(key), sut.Contains(key), $"Contains('{key}') diverged at op {op} (seed {seed}).");
                    break;

                default:
                    Assert.AreEqual(oracle.StartsWith(key), sut.StartsWith(key), $"StartsWith('{key}') diverged at op {op} (seed {seed}).");
                    Assert.IsTrue(
                        sut.KeysWithPrefix(key).ToHashSet().SetEquals(oracle.KeysWithPrefix(key)),
                        $"KeysWithPrefix('{key}') diverged at op {op} (seed {seed}).");
                    break;
            }

            Assert.AreEqual(oracle.Count, sut.Count, $"Count diverged at op {op} (seed {seed}).");

            if (op % 1_000 == 999)
            {
                Assert.IsTrue(
                    sut.ToHashSet().SetEquals(oracle),
                    $"Full contents diverged at checkpoint op {op} (seed {seed}).");
            }
        }
    }
}

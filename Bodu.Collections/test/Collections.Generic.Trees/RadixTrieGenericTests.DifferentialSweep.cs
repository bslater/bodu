// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RadixTrieGenericTests.DifferentialSweep.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Collections.Generic.Trees;

public sealed partial class RadixTrieGenericTests
{
    /// <summary>
    /// Verifies that 10,000 seeded set / remove / lookup / prefix operations on random short string keys agree
    /// operation-for-operation with the uncompressed <see cref="Trie{TValue}" /> oracle, including value equality on
    /// every lookup and periodic full-content checkpoints.
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
        var sut = new RadixTrie<int>();
        var oracle = new Trie<int>();

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
                    int value = random.Next(1_000);
                    oracle.Set(key, value);
                    sut.Set(key, value);
                    break;

                case 1:
                    Assert.AreEqual(oracle.Remove(key), sut.Remove(key), $"Remove('{key}') diverged at op {op} (seed {seed}).");
                    break;

                case 2:
                    Assert.AreEqual(oracle.ContainsKey(key), sut.ContainsKey(key), $"ContainsKey('{key}') diverged at op {op} (seed {seed}).");
                    Assert.AreEqual(oracle.TryGetValue(key, out int expectedValue), sut.TryGetValue(key, out int actualValue), $"TryGetValue('{key}') diverged at op {op} (seed {seed}).");
                    Assert.AreEqual(expectedValue, actualValue, $"Value for '{key}' diverged at op {op} (seed {seed}).");
                    break;

                default:
                    Assert.AreEqual(oracle.StartsWith(key), sut.StartsWith(key), $"StartsWith('{key}') diverged at op {op} (seed {seed}).");
                    Assert.IsTrue(
                        sut.ItemsWithPrefix(key).ToHashSet().SetEquals(oracle.ItemsWithPrefix(key)),
                        $"ItemsWithPrefix('{key}') diverged at op {op} (seed {seed}).");
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

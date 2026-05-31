// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGeneration.LookAndSay.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Extensions;

[TestClass]
public class LookAndSayTests
{

    /// <summary>
    /// Verifies that <see cref="SequenceGenerator.LookAndSay" /> throws <see cref="ArgumentOutOfRangeException" /> when count is below 1.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void LookAndSay_WhenCountIsLessThanOne_ShouldThrowExactly(int count)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = SequenceGenerator.LookAndSay(count).ToList();
        });
    }

    /// <summary>
    /// Verifies that <see cref="SequenceGenerator.LookAndSay" /> with a count of one returns only the seed term.
    /// </summary>
    [TestMethod]
    public void LookAndSay_WhenCountIsOne_ShouldReturnSeedOnly()
    {
        var actual = SequenceGenerator.LookAndSay(1).ToArray();
        CollectionAssert.AreEqual(new[] { "1" }, actual);
    }

    /// <summary>
    /// Verifies that the canonical Conway Look-and-Say prefix is produced for a count of six terms.
    /// </summary>
    [TestMethod]
    public void LookAndSay_WhenCountIsSix_ShouldReturnCanonicalPrefix()
    {
        var actual = SequenceGenerator.LookAndSay(6).ToArray();
        CollectionAssert.AreEqual(new[] { "1", "11", "21", "1211", "111221", "312211" }, actual);
    }

    /// <summary>
    /// Verifies that each term in the Look-and-Say sequence has length not less than the previous term, matching the monotonic growth property.
    /// </summary>
    [TestMethod]
    public void LookAndSay_WhenEnumerated_ShouldProduceNonDecreasingLengths()
    {
        var actual = SequenceGenerator.LookAndSay(10).ToArray();
        for (var i = 1; i < actual.Length; i++)
        {
            Assert.IsGreaterThanOrEqualTo(actual[i - 1].Length, actual[i].Length,
                $"Term at index {i} ({actual[i].Length}) is shorter than the previous term ({actual[i - 1].Length}).");
        }
    }

    /// <summary>
    /// Verifies that every term in the Look-and-Say sequence is composed solely of decimal digits.
    /// </summary>
    [TestMethod]
    public void LookAndSay_WhenEnumerated_ShouldYieldOnlyDigitCharacters()
    {
        foreach (var term in SequenceGenerator.LookAndSay(8))
        {
            foreach (var ch in term)
                Assert.IsTrue(ch >= '0' && ch <= '9', $"Term '{term}' contained a non-digit character '{ch}'.");
        }
    }

}

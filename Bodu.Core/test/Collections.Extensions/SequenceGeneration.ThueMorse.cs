// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGeneration.ThueMorse.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Extensions;

[TestClass]
public class ThueMorseTests
{

    /// <summary>
    /// Verifies that <see cref="SequenceGenerator.ThueMorse" /> throws <see cref="ArgumentOutOfRangeException" /> when count is negative.
    /// </summary>
    [TestMethod]
    public void ThueMorse_WhenCountIsNegative_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = SequenceGenerator.ThueMorse(-1).ToList();
        });
    }

    /// <summary>
    /// Verifies that the first sixteen terms of the Thue–Morse sequence match the canonical prefix.
    /// </summary>
    [TestMethod]
    public void ThueMorse_WhenCountIsSixteen_ShouldReturnCanonicalPrefix()
    {
        var actual = SequenceGenerator.ThueMorse(16).ToArray();
        CollectionAssert.AreEqual(new[] { 0, 1, 1, 0, 1, 0, 0, 1, 1, 0, 0, 1, 0, 1, 1, 0 }, actual);
    }

    /// <summary>
    /// Verifies that <see cref="SequenceGenerator.ThueMorse" /> returns an empty sequence when count is zero.
    /// </summary>
    [TestMethod]
    public void ThueMorse_WhenCountIsZero_ShouldReturnEmptySequence()
    {
        var actual = SequenceGenerator.ThueMorse(0).ToArray();
        Assert.AreEqual(0, actual.Length);
    }

    /// <summary>
    /// Verifies that every emitted Thue–Morse term equals the parity of the number of set bits in its zero-based index.
    /// </summary>
    [TestMethod]
    public void ThueMorse_WhenEnumerated_ShouldMatchPopcountParity()
    {
        const int count = 64;
        var actual = SequenceGenerator.ThueMorse(count).ToArray();

        for (var i = 0; i < count; i++)
        {
            int parity = 0, n = i;
            while (n > 0) { parity ^= n & 1; n >>= 1; }
            Assert.AreEqual(parity, actual[i], $"Element at index {i} does not match popcount parity.");
        }
    }

    /// <summary>
    /// Verifies that every emitted term is either <c>0</c> or <c>1</c>.
    /// </summary>
    [TestMethod]
    public void ThueMorse_WhenEnumerated_ShouldYieldOnlyZeroOrOne()
    {
        foreach (var v in SequenceGenerator.ThueMorse(32))
            Assert.IsTrue(v == 0 || v == 1, $"Expected 0 or 1, got {v}.");
    }

}

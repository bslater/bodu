// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGeneratorTests.ThueMorse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Sequences;

public partial class SequenceGeneratorTests
{
    /// <summary>
    /// Verifies that <see cref="SequenceGenerator.ThueMorse" /> throws <see cref="ArgumentOutOfRangeException" /> when
    /// count is negative.
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
        int[] actual = SequenceGenerator.ThueMorse(16).ToArray();
        CollectionAssert.AreEqual(new[] { 0, 1, 1, 0, 1, 0, 0, 1, 1, 0, 0, 1, 0, 1, 1, 0 }, actual);
    }

    /// <summary>
    /// Verifies that <see cref="SequenceGenerator.ThueMorse" /> returns an empty sequence when count is zero.
    /// </summary>
    [TestMethod]
    public void ThueMorse_WhenCountIsZero_ShouldReturnEmptySequence()
    {
        int[] actual = SequenceGenerator.ThueMorse(0).ToArray();
        Assert.IsEmpty(actual);
    }

    /// <summary>
    /// Verifies that every emitted Thue–Morse term equals the parity of the number of set bits in its zero-based index.
    /// </summary>
    [TestMethod]
    public void ThueMorse_WhenEnumerated_ShouldMatchPopcountParity()
    {
        const int count = 64;
        int[] actual = SequenceGenerator.ThueMorse(count).ToArray();

        for (int i = 0; i < count; i++)
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
        foreach (int v in SequenceGenerator.ThueMorse(32))
            Assert.IsTrue(v is 0 or 1, $"Expected 0 or 1, got {v}.");
    }
}

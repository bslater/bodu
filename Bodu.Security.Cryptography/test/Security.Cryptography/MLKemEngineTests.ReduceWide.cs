// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemEngineTests.ReduceWide.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLKemEngineTests
{
    /// <summary>
    /// Verifies that the wide Barrett reduction returns its input modulo q for inputs at the ends of its range, around
    /// multiples of q, at the largest base-case product sum, and seeded across the range.
    /// </summary>
    [TestMethod]
    public void ReduceWide_WhenValueIsBelowTwoToThe36_ShouldReturnTheValueModuloQ()
    {
        const ulong Limit = (1UL << 36) - 1;
        var random = new Random(0x0203_0003);
        IEnumerable<ulong> values = new ulong[] { 0, 1, Q - 1, Q, (2 * Q) - 1, 2 * Q, Limit, ((ulong)Q * Q * Q) + ((ulong)Q * Q) }
            .Concat(Enumerable.Range(0, 100_000).Select(_ => (ulong)random.NextInt64() & Limit));

        foreach (ulong value in values)
        {
            int reduced = MLKemEngine.ReduceWide(value);

            if (reduced != (int)(value % Q))
                Assert.Fail($"ReduceWide({value}) = {reduced}.");
        }
    }
}

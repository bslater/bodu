// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngineTests.MontgomeryReduce.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLDsaEngineTests
{
    /// <summary>
    /// Verifies that Montgomery reduction returns a value congruent to its input times 2^−32 modulo q, within (−q, q),
    /// for inputs at the ends of its range, around zero, and seeded across it.
    /// </summary>
    [TestMethod]
    public void MontgomeryReduce_WhenValueIsWithinItsRange_ShouldReturnTheValueTimesTheInverseOfTwoToThe32()
    {
        long limit = ((long)Q << 31) - 1;
        long inverse = LatticeCommon.PowMod(LatticeCommon.PowMod(2, 32, Q), Q - 2, Q);
        var random = new Random(0x0204_0002);
        IEnumerable<long> values = new[] { 0L, 1, -1, Q, -Q, limit, -limit, 1L << 32, -(1L << 32) }
            .Concat(Enumerable.Range(0, 100_000).Select(_ => random.NextInt64(-limit, limit + 1)));

        foreach (long value in values)
        {
            int reduced = MLDsaEngine.MontgomeryReduce(value);

            if (reduced <= -Q || reduced >= Q || Mod(reduced) != (int)(((System.Numerics.BigInteger)value * inverse % Q + Q) % Q))
                Assert.Fail($"MontgomeryReduce({value}) = {reduced}.");
        }
    }
}

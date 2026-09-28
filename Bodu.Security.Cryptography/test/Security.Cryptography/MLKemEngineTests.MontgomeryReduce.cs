// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemEngineTests.MontgomeryReduce.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLKemEngineTests
{
    /// <summary>
    /// Verifies that Montgomery reduction returns a value congruent to its input times 2^−16 modulo q, within (−q, q),
    /// for inputs at the ends of its range, around zero, and seeded across it.
    /// </summary>
    [TestMethod]
    public void MontgomeryReduce_WhenValueIsWithinItsRange_ShouldReturnTheValueTimesTheInverseOfTwoToThe16()
    {
        const int Limit = (Q << 15) - 1;
        int inverse = LatticeCommon.PowMod(1 << 16, Q - 2, Q);
        var random = new Random(0x0203_0002);
        IEnumerable<int> values = new[] { 0, 1, -1, Q, -Q, Limit, -Limit, 1 << 16, -(1 << 16) }
            .Concat(Enumerable.Range(0, 100_000).Select(_ => random.Next(-Limit, Limit + 1)));

        foreach (int value in values)
        {
            int reduced = MLKemEngine.MontgomeryReduce(value);

            if (reduced <= -Q || reduced >= Q || Mod(reduced) != Mod((long)value * inverse))
                Assert.Fail($"MontgomeryReduce({value}) = {reduced}.");
        }
    }
}

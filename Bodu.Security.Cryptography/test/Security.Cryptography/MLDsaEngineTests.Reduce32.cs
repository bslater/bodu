// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngineTests.Reduce32.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLDsaEngineTests
{
    /// <summary>
    /// Verifies that the sum reduction returns a representative within [−6283008, 6283008] for inputs at the ends of its
    /// range and seeded across it, and that freezing it lands in [0, q).
    /// </summary>
    [TestMethod]
    public void Reduce32_WhenValueIsWithinItsRange_ShouldReturnARepresentativeWithinBound()
    {
        const int Largest = int.MaxValue - (1 << 22);
        var random = new Random(0x0204_0004);
        IEnumerable<int> values = new[] { 0, 1, -1, Q, -Q, Largest, int.MinValue, Largest - 1, int.MinValue + 1 }
            .Concat(Enumerable.Range(0, 100_000).Select(_ => random.Next(int.MinValue, Largest)));

        foreach (int value in values)
        {
            int reduced = MLDsaEngine.Reduce32(value);
            int frozen = MLDsaEngine.Freeze(value);

            if (reduced < -6283008 || reduced > 6283008 || Mod(reduced) != Mod(value) || frozen != Mod(value))
                Assert.Fail($"Reduce32({value}) = {reduced}, Freeze({value}) = {frozen}.");
        }
    }
}

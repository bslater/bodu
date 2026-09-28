// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngineTests.Freeze.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLDsaEngineTests
{
    /// <summary>
    /// Verifies that freezing returns the representative in [0, q) across the whole input range, including both of
    /// its ends.
    /// </summary>
    [TestMethod]
    public void Freeze_WhenValueIsWithinItsRange_ShouldReturnTheCanonicalRepresentative()
    {
        const int Maximum = int.MaxValue - (1 << 22);
        var random = new Random(0x0204_0009);
        IEnumerable<int> values = new[] { int.MinValue, int.MinValue + 1, -Q, -1, 0, 1, Q - 1, Q, Maximum - 1, Maximum }
            .Concat(Enumerable.Range(0, 100_000).Select(_ => random.Next(int.MinValue, Maximum)));

        foreach (int value in values)
        {
            int frozen = MLDsaEngine.Freeze(value);

            if (frozen != Mod(value))
                Assert.Fail($"Freeze({value}) = {frozen}.");
        }
    }
}

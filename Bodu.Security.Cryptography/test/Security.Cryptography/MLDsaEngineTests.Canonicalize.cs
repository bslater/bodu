// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngineTests.Canonicalize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLDsaEngineTests
{
    /// <summary>
    /// Verifies that canonicalization maps every value in [−q, q) to its representative in [0, q).
    /// </summary>
    [TestMethod]
    public void Canonicalize_WhenValueIsWithinQOfZero_ShouldReturnTheRepresentativeInRange()
    {
        var random = new Random(0x0204_0008);
        IEnumerable<int> values = new[] { -Q, -Q + 1, -1, 0, 1, Q - 1 }.Concat(Enumerable.Range(0, 100_000).Select(_ => random.Next(-Q, Q)));

        foreach (int value in values)
        {
            int canonical = MLDsaEngine.Canonicalize(value);

            if (canonical != Mod(value))
                Assert.Fail($"Canonicalize({value}) = {canonical}.");
        }
    }
}

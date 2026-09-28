// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemEngineTests.Canonicalize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLKemEngineTests
{
    /// <summary>
    /// Verifies that every value in [−q, q) maps to its representative in [0, q).
    /// </summary>
    [TestMethod]
    public void Canonicalize_WhenValueIsWithinQOfZero_ShouldReturnTheRepresentativeInRange()
    {
        for (int value = -Q; value < Q; value++)
        {
            int canonical = MLKemEngine.Canonicalize(value);

            if (canonical != Mod(value))
                Assert.Fail($"Canonicalize({value}) = {canonical}.");
        }
    }
}

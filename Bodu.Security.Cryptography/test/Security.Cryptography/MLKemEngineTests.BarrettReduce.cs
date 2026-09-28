// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemEngineTests.BarrettReduce.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLKemEngineTests
{
    /// <summary>
    /// Verifies that Barrett reduction returns the centered representative modulo q, within
    /// [−(q − 1) / 2, (q − 1) / 2], for every input below 2^15 in magnitude.
    /// </summary>
    [TestMethod]
    public void BarrettReduce_WhenValueIsWithinItsRange_ShouldReturnTheCenteredRepresentative()
    {
        for (int value = -(1 << 15) + 1; value < (1 << 15); value++)
        {
            int reduced = MLKemEngine.BarrettReduce(value);

            if (reduced < -(Q - 1) / 2 || reduced > (Q - 1) / 2 || Mod(reduced) != Mod(value))
                Assert.Fail($"BarrettReduce({value}) = {reduced}.");
        }
    }
}

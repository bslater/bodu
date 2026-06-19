// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkeinTests{T,T,T}.256.KnownAnswers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Skein256Tests
{
    /// <summary>
    /// Verifies that <see cref="Skein256.AlgorithmName" /> formats the state size and the configured output size.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenConfiguredWithOutputSize_ShouldReturnFormattedName()
    {
        using var skein = new Skein256(224);

        Assert.AreEqual("Skein-256-224", skein.AlgorithmName);
    }
}

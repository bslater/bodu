// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkeinTests{T,T,T}.1024.KnownAnswers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Skein1024Tests
{
    /// <summary>
    /// Verifies that <see cref="Skein1024.AlgorithmName" /> formats the state size and the configured output size.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenConfiguredWithOutputSize_ShouldReturnFormattedName()
    {
        using var skein = new Skein1024(512);

        Assert.AreEqual("Skein-1024-512", skein.AlgorithmName);
    }
}

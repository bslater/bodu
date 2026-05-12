// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Skein512Tests.KnownAnswers.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Skein512Tests
{
    /// <summary>
    /// Verifies that <see cref="Skein512.AlgorithmName" /> formats the state size and the configured output size.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenConfiguredWithOutputSize_ShouldReturnFormattedName()
    {
        using var skein = new Skein512(384);

        Assert.AreEqual("Skein-512-384", skein.AlgorithmName);
    }
}

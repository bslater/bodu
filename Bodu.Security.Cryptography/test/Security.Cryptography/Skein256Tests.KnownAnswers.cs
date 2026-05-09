// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Skein256Tests.KnownAnswers.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Skein256Tests
{
    /// <summary>
    /// Verifies that <see cref="Skein256.AlgorithmName" /> formats the state size and the configured output size.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void AlgorithmName_WhenConfiguredWithOutputSize_ShouldReturnFormattedName()
    {
        using var skein = new Skein256(224);

        Assert.AreEqual("Skein-256-224", skein.AlgorithmName);
    }
}

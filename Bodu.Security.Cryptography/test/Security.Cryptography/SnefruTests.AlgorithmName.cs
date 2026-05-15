// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SnefruTests.AlgorithmName.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Snefru128Tests
{
    /// <summary>
    /// Verifies that <see cref="Snefru128.AlgorithmName" /> returns <c>"Snefru/128"</c> on a fresh instance.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenInstanceIsFresh_ShouldReturnSnefru128Name()
    {
        using var algorithm = new Snefru128();

        Assert.AreEqual("Snefru/128", algorithm.AlgorithmName);
    }
}

public partial class Snefru256Tests
{
    /// <summary>
    /// Verifies that <see cref="Snefru256.AlgorithmName" /> returns <c>"Snefru/256"</c> on a fresh instance.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenInstanceIsFresh_ShouldReturnSnefru256Name()
    {
        using var algorithm = new Snefru256();

        Assert.AreEqual("Snefru/256", algorithm.AlgorithmName);
    }
}

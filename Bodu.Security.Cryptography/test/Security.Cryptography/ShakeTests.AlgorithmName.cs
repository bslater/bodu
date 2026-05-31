// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ShakeTests.AlgorithmName.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class ShakeTests
{
    /// <summary>
    /// Verifies that the parameterless <see cref="Shake.Shake()" /> constructor produces an instance with the
    /// default SHAKE128 security level and a 256-bit output, with <see cref="Shake.AlgorithmName" /> reporting
    /// <c>"SHAKE128"</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenParameterless_ShouldUseShake128AndReportName()
    {
        using var algorithm = new Shake();

        Assert.AreEqual(128, algorithm.SecurityLevel);
        Assert.AreEqual(256, algorithm.HashSize);
        Assert.AreEqual("SHAKE128", algorithm.AlgorithmName);
    }

    /// <summary>
    /// Verifies that <see cref="Shake.AlgorithmName" /> reports the correct name for each supported security
    /// level.
    /// </summary>
    [DataRow(128, "SHAKE128")]
    [DataRow(256, "SHAKE256")]
    [TestMethod]
    public void AlgorithmName_WhenUsingSecurityLevel_ShouldReturnFormattedName(int securityLevel, string expected)
    {
        using var algorithm = new Shake(outputBits: 256, securityLevel: securityLevel);

        Assert.AreEqual(expected, algorithm.AlgorithmName);
    }

    /// <summary>
    /// Verifies that <see cref="Shake.HashSize" /> can be set to a supported multiple-of-eight value on a fresh
    /// instance and the change is reflected by the <see cref="System.Security.Cryptography.HashAlgorithm.HashSize" />
    /// getter.
    /// </summary>
    [TestMethod]
    public void HashSize_WhenSetToMultipleOfEight_ShouldUpdateHashSize()
    {
        using var algorithm = new Shake();

        algorithm.HashSize = 512;

        Assert.AreEqual(512, algorithm.HashSize);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconHashA256Tests.AlgorithmName.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class AsconHashA256Tests
{
    /// <summary>
    /// Verifies that <see cref="AsconHashA256.AlgorithmName" /> returns the canonical identifier defined in NIST SP 800-232.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_ShouldReturnAsconHashA256()
    {
        using var algorithm = new AsconHashA256();

        Assert.AreEqual("ASCON-HASHA256", algorithm.AlgorithmName);
    }

}

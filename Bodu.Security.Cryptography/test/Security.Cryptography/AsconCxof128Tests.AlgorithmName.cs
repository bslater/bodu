// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconCxof128Tests.AlgorithmName.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class AsconCxof128Tests
{
    /// <summary>
    /// Verifies that <see cref="AsconCxof128.AlgorithmName" /> returns the canonical NIST SP 800-232
    /// algorithm identifier.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_ShouldReturnAsconCxof128()
    {
        using var sut = new AsconCxof128();
        Assert.AreEqual("ASCON-CXOF128", sut.AlgorithmName);
    }

    /// <summary>
    /// Verifies that reading <see cref="AsconXof{T}.AlgorithmName" /> on a disposed
    /// <see cref="AsconCxof128" /> instance throws <see cref="ObjectDisposedException" /> rather
    /// than returning the cached identifier from cleared state.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenAccessedAfterDispose_ShouldThrowExactly()
    {
        var sut = new AsconCxof128();
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = sut.AlgorithmName;
        });
    }
}

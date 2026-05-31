// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ShakeTests.Ctors.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class ShakeTests
{
    /// <summary>
    /// Verifies that the security level property returns the value supplied at construction time.
    /// </summary>
    [TestMethod]
    public void SecurityLevel_AfterConstruction_ShouldReturnSuppliedValue()
    {
        using Shake sut128 = new(256, 128);
        using Shake sut256 = new(256, 256);

        Assert.AreEqual(128, sut128.SecurityLevel);
        Assert.AreEqual(256, sut256.SecurityLevel);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TigerTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class TigerTests
{
    /// <summary>
    /// Verifies that constructing a <see cref="Tiger" /> with an unsupported hash size throws
    /// <see cref="ArgumentOutOfRangeException" /> rather than allowing the bad size to silently
    /// propagate to <see cref="HashAlgorithm.HashSize" />.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(64)]
    [DataRow(96)]
    [DataRow(193)]
    [DataRow(256)]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    [DataRow(int.MaxValue)]
    public void Ctor_WhenHashSizeIsInvalid_ShouldThrowExactly(int hashSize)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Tiger(hashSize);
        });
    }
}

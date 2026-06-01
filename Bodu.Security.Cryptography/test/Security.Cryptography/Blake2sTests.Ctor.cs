// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2sTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class Blake2sTests
{
    /// <summary>
    /// Verifies that constructing a <see cref="Blake2s" /> with an unsupported hash size throws
    /// <see cref="ArgumentOutOfRangeException" /> rather than allowing the bad size to silently
    /// propagate to <see cref="HashAlgorithm.HashSize" />.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(7)]
    [DataRow(8)]
    [DataRow(127)]
    [DataRow(129)]
    [DataRow(257)]
    [DataRow(384)]
    [DataRow(512)]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    [DataRow(int.MaxValue)]
    public void Ctor_WhenHashSizeIsInvalid_ShouldThrowExactly(int hashSize)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Blake2s(hashSize);
        });
    }

    /// <summary>
    /// Verifies that constructing a <see cref="Blake2s" /> with each documented valid hash size
    /// succeeds and reports the corresponding <see cref="HashAlgorithm.HashSize" /> value.
    /// </summary>
    [TestMethod]
    [DataRow(128)]
    [DataRow(160)]
    [DataRow(192)]
    [DataRow(224)]
    [DataRow(256)]
    public void Ctor_WhenHashSizeIsValid_ShouldSetHashSize(int hashSize)
    {
        using var algorithm = new Blake2s(hashSize);

        Assert.AreEqual(hashSize, algorithm.HashSize);
    }
}

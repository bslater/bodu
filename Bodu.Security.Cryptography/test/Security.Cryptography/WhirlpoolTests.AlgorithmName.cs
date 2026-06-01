// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WhirlpoolTests.AlgorithmName.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class WhirlpoolTests
{
    /// <summary>
    /// Verifies that <see cref="Whirlpool.AlgorithmName" /> maps each <see cref="WhirlpoolVersion" /> to its
    /// canonical published name: <c>Whirlpool-0</c> for the original 2000 submission, <c>Whirlpool-T</c> for the
    /// 2001 revision, and <c>Whirlpool</c> for the standardized ISO/IEC 10118-3 final.
    /// </summary>
    [DataRow(WhirlpoolVersion.WhirlpoolInfo1, "Whirlpool-0")]
    [DataRow(WhirlpoolVersion.WhirlpoolInfo2, "Whirlpool-T")]
    [DataRow(WhirlpoolVersion.WhirlpoolInfo3, "Whirlpool")]
    [TestMethod]
    public void AlgorithmName_WhenVersionIsSet_ShouldReturnPublishedName(WhirlpoolVersion version, string expected)
    {
        using var algorithm = new Whirlpool { Version = version };

        Assert.AreEqual(expected, algorithm.AlgorithmName);
    }
}

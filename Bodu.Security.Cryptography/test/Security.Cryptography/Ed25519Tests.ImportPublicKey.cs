// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519Tests.ImportPublicKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains Ed25519-specific tests for <see cref="Ed25519.ImportPublicKey" /> — the RFC 8032 point-encoding
/// validation; the public-only-instance and length-validation contract is inherited from the asymmetric base.
/// </summary>
public sealed partial class Ed25519Tests
{
    /// <summary>
    /// Verifies that <see cref="Ed25519.ImportPublicKey" /> throws <see cref="ArgumentException" /> for material
    /// that is not on the curve, is a non-canonical point encoding, or carries the invalid x = 0 sign-bit
    /// combination.
    /// </summary>
    [TestMethod]
    [DataRow("y=2 not on curve", "0200000000000000000000000000000000000000000000000000000000000000")]
    [DataRow("non-canonical y=p", "edffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f")]
    [DataRow("x=0 with sign bit set", "0100000000000000000000000000000000000000000000000000000000000080")]
    public void ImportPublicKey_WhenEncodingIsInvalid_ShouldThrowArgumentException(string testName, string encodedHex)
    {
        _ = testName;

        using var algorithm = new Ed25519();

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            algorithm.ImportPublicKey(Convert.FromHexString(encodedHex));
        });

        Assert.AreEqual("publicKey", ex.ParamName);
    }
}

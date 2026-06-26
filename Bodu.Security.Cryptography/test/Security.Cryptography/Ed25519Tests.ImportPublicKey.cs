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

    /// <summary>
    /// Verifies that <see cref="Ed25519.ImportPublicKey" /> rejects each of the eight small-order points. These
    /// decode as valid curve points but lie in the order-8 cofactor subgroup rather than the prime-order subgroup,
    /// and are incompatible with Bodu's strict cofactorless verification policy.
    /// </summary>
    [TestMethod]
    [DataRow("order 1 (identity)", "0100000000000000000000000000000000000000000000000000000000000000")]
    [DataRow("order 2", "ecffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f")]
    [DataRow("order 4 (x positive)", "0000000000000000000000000000000000000000000000000000000000000000")]
    [DataRow("order 4 (x negative)", "0000000000000000000000000000000000000000000000000000000000000080")]
    [DataRow("order 8 (a)", "26e8958fc2b227b045c3f489f2ef98f0d5dfac05d3c63339b13802886d53fc05")]
    [DataRow("order 8 (b)", "c7176a703d4dd84fba3c0b760d10670f2a2053fa2c39ccc64ec7fd7792ac037a")]
    [DataRow("order 8 (c)", "26e8958fc2b227b045c3f489f2ef98f0d5dfac05d3c63339b13802886d53fc85")]
    [DataRow("order 8 (d)", "c7176a703d4dd84fba3c0b760d10670f2a2053fa2c39ccc64ec7fd7792ac03fa")]
    public void ImportPublicKey_WhenKeyIsSmallOrder_ShouldThrowArgumentException(string testName, string encodedHex)
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

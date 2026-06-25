// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519Tests.VerifyData.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains Ed25519-specific tests for <see cref="Ed25519.VerifyData(ReadOnlySpan{byte}, ReadOnlySpan{byte})" /> —
/// the RFC 8032 canonical-S malleability check; the round-trip, tamper, and malformed-signature contracts are
/// inherited from the signature base.
/// </summary>
public sealed partial class Ed25519Tests
{
    /// <summary>
    /// Verifies that a signature whose S component has the group order L added — an otherwise-forgeable malleated
    /// twin — is rejected by the canonical-S check.
    /// </summary>
    [TestMethod]
    public void VerifyData_WhenSignatureSComponentIsMalleated_ShouldReturnFalse()
    {
        using var algorithm = new Ed25519();
        algorithm.GenerateKey();
        byte[] message = new byte[] { 9, 9, 9 };
        byte[] signature = algorithm.SignData(message);

        // S' = S + L: congruent modulo L, so it satisfies the verification equation arithmetically, but RFC 8032
        // requires S < L. Adding the little-endian order with manual carry produces the non-canonical twin.
        byte[] order = Convert.FromHexString("edd3f55c1a631258d69cf7a2def9de1400000000000000000000000000000010");
        byte[] malleated = (byte[])signature.Clone();
        int carry = 0;
        for (int i = 0; i < 32; i++)
        {
            int sum = malleated[32 + i] + order[i] + carry;
            malleated[32 + i] = (byte)sum;
            carry = sum >> 8;
        }

        Assert.IsFalse(algorithm.VerifyData(message, malleated));
    }

    /// <summary>
    /// Verifies that a signature whose commitment R is a small-order point is rejected. Under the cofactorless
    /// verification equation a small-order R could otherwise contribute a torsion component without changing the
    /// arithmetic outcome, so the strict policy rejects it outright.
    /// </summary>
    [TestMethod]
    [DataRow("order 1 (identity)", "0100000000000000000000000000000000000000000000000000000000000000")]
    [DataRow("order 2", "ecffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f")]
    [DataRow("order 8", "26e8958fc2b227b045c3f489f2ef98f0d5dfac05d3c63339b13802886d53fc05")]
    public void VerifyData_WhenSignatureRIsSmallOrder_ShouldReturnFalse(string testName, string smallOrderRHex)
    {
        _ = testName;

        using var algorithm = new Ed25519();
        algorithm.GenerateKey();
        byte[] message = new byte[] { 4, 2 };

        // Replace R with a small-order encoding while keeping a canonical S, so rejection is driven by the
        // small-order check rather than a malformed S or length error.
        byte[] signature = algorithm.SignData(message);
        Convert.FromHexString(smallOrderRHex).CopyTo(signature, 0);

        Assert.IsFalse(algorithm.VerifyData(message, signature));
    }
}

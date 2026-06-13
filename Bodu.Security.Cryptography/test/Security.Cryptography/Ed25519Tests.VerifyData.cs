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
        var message = new byte[] { 9, 9, 9 };
        var signature = algorithm.SignData(message);

        // S' = S + L: congruent modulo L, so it satisfies the verification equation arithmetically, but RFC 8032
        // requires S < L. Adding the little-endian order with manual carry produces the non-canonical twin.
        var order = Convert.FromHexString("edd3f55c1a631258d69cf7a2def9de1400000000000000000000000000000010");
        var malleated = (byte[])signature.Clone();
        var carry = 0;
        for (var i = 0; i < 32; i++)
        {
            var sum = malleated[32 + i] + order[i] + carry;
            malleated[32 + i] = (byte)sum;
            carry = sum >> 8;
        }

        Assert.IsFalse(algorithm.VerifyData(message, malleated));
    }
}

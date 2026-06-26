// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519Tests.VerifyData.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

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
    /// Yields the known small-order Ed25519 R encodings, each paired with the expected
    /// <see cref="Ed25519.VerifyData(ReadOnlySpan{byte}, ReadOnlySpan{byte})" /> result of <see langword="false" />.
    /// </summary>
    /// <returns>One <see cref="BinaryKat{TInput, TExpected}" /> row per small-order R encoding.</returns>
    private static IEnumerable<object[]> SmallOrderRVectors()
    {
        (string Name, string Hex)[] points =
        {
            ("order 1 (identity)", "0100000000000000000000000000000000000000000000000000000000000000"),
            ("order 2", "ecffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f"),
            ("order 8", "26e8958fc2b227b045c3f489f2ef98f0d5dfac05d3c63339b13802886d53fc05"),
        };

        foreach ((string name, string hex) in points)
        {
            yield return new object[] { new BinaryKat<byte[], bool>(name, Convert.FromHexString(hex), false) };
        }
    }

    /// <summary>
    /// Yields the non-canonical Ed25519 S encodings at or above the group order L, each paired with the expected
    /// <see cref="Ed25519.VerifyData(ReadOnlySpan{byte}, ReadOnlySpan{byte})" /> result of <see langword="false" />.
    /// </summary>
    /// <returns>One <see cref="BinaryKat{TInput, TExpected}" /> row per non-canonical S encoding.</returns>
    private static IEnumerable<object[]> NonCanonicalSVectors()
    {
        (string Name, string Hex)[] values =
        {
            ("S = L", "edd3f55c1a631258d69cf7a2def9de1400000000000000000000000000000010"),
            ("S = L + 1", "eed3f55c1a631258d69cf7a2def9de1400000000000000000000000000000010"),
        };

        foreach ((string name, string hex) in values)
        {
            yield return new object[] { new BinaryKat<byte[], bool>(name, Convert.FromHexString(hex), false) };
        }
    }

    /// <summary>
    /// Verifies that a signature whose commitment R is a small-order point is rejected. Under the cofactorless
    /// verification equation a small-order R could otherwise contribute a torsion component without changing the
    /// arithmetic outcome, so the strict policy rejects it outright.
    /// </summary>
    /// <param name="vector">The small-order R KAT vector under test.</param>
    [TestMethod]
    [DynamicData(nameof(SmallOrderRVectors), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void VerifyData_WhenSignatureRIsSmallOrder_ShouldReturnFalse(BinaryKat<byte[], bool> vector)
    {
        using var algorithm = new Ed25519();
        algorithm.GenerateKey();
        byte[] message = new byte[] { 4, 2 };

        // Replace R with a small-order encoding while keeping a canonical S, so rejection is driven by the
        // small-order check rather than a malformed S or length error.
        byte[] signature = algorithm.SignData(message);
        vector.Input.CopyTo(signature, 0);

        Assert.AreEqual(vector.Expected, algorithm.VerifyData(message, signature));
    }

    /// <summary>
    /// Verifies that a signature whose S component is replaced by a value at or above the group order L is rejected,
    /// because RFC 8032 requires 0 ≤ S &lt; L.
    /// </summary>
    /// <param name="vector">The non-canonical S KAT vector under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCanonicalSVectors), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void VerifyData_WhenSComponentIsAtOrAboveGroupOrder_ShouldReturnFalse(BinaryKat<byte[], bool> vector)
    {
        using var algorithm = new Ed25519();
        algorithm.GenerateKey();
        byte[] message = new byte[] { 1, 2, 3 };

        byte[] signature = algorithm.SignData(message);
        vector.Input.CopyTo(signature, 32);

        Assert.AreEqual(vector.Expected, algorithm.VerifyData(message, signature));
    }

    /// <summary>
    /// Verifies that a signature whose S component has bit 255 set is rejected, because such a value is at least
    /// 2^255 and therefore far above the group order L.
    /// </summary>
    [TestMethod]
    public void VerifyData_WhenSComponentHighBitIsSet_ShouldReturnFalse()
    {
        using var algorithm = new Ed25519();
        algorithm.GenerateKey();
        byte[] message = new byte[] { 1, 2, 3 };

        byte[] signature = algorithm.SignData(message);
        signature[63] |= 0x80;

        Assert.IsFalse(algorithm.VerifyData(message, signature));
    }
}

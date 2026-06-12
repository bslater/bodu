// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Curve25519Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="Curve25519" /> scalar multiplication engine, validated against the
/// published RFC 7748 test vectors.
/// </summary>
[TestClass]
public class Curve25519Tests
{
    // ── ScalarMult (RFC 7748 §5.2) ────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="Curve25519.ScalarMult" /> reproduces the two single-call X25519 test vectors from
    /// RFC 7748 §5.2.
    /// </summary>
    [TestMethod]
    [DataRow(
        "RFC 7748 §5.2 vector 1",
        "a546e36bf0527c9d3b16154b82465edd62144c0ac1fc5a18506a2244ba449ac4",
        "e6db6867583030db3594c1a424b15f7c726624ec26b3353b10a903a6d0ab1c4c",
        "c3da55379de9c6908e94ea4df28d084f32eccf03491c71f754b4075577a28552")]
    [DataRow(
        "RFC 7748 §5.2 vector 2",
        "4b66e9d4d1b4673c5ad22691957d6af5c11b6421e0ea01d42ca4169e7918ba0d",
        "e5210f12786811d3f4b7959d0538ae2c31dbe7106fc03c3efc4cd549c715a493",
        "95cbde9476e8907d7aade45cb4b873f88b595a68799fa152e6f8f7647aac7957")]
    public void ScalarMult_WhenGivenRfc7748Vector_ShouldProduceExpectedOutput(
        string testName, string scalarHex, string uHex, string expectedHex)
    {
        _ = testName;

        var destination = new byte[Curve25519.PointSizeInBytes];
        var allZero = Curve25519.ScalarMult(Convert.FromHexString(scalarHex), Convert.FromHexString(uHex), destination);

        Assert.IsFalse(allZero);
        CollectionAssert.AreEqual(Convert.FromHexString(expectedHex), destination);
    }

    /// <summary>
    /// Verifies that <see cref="Curve25519.ScalarMult" /> returns the all-zero flag when the peer u-coordinate is the
    /// low-order point 0, whose subgroup collapses every result to zero.
    /// </summary>
    [TestMethod]
    public void ScalarMult_WhenUIsLowOrderZeroPoint_ShouldReturnAllZeroFlag()
    {
        var scalar = Convert.FromHexString("a546e36bf0527c9d3b16154b82465edd62144c0ac1fc5a18506a2244ba449ac4");
        var destination = new byte[Curve25519.PointSizeInBytes];

        var allZero = Curve25519.ScalarMult(scalar, new byte[Curve25519.PointSizeInBytes], destination);

        Assert.IsTrue(allZero);
        CollectionAssert.AreEqual(new byte[Curve25519.PointSizeInBytes], destination);
    }

    /// <summary>
    /// Verifies that <see cref="Curve25519.ScalarMult" /> does not modify the caller's scalar buffer when applying
    /// RFC 7748 clamping.
    /// </summary>
    [TestMethod]
    public void ScalarMult_WhenScalarRequiresClamping_ShouldNotModifyCallerScalar()
    {
        var scalar = Convert.FromHexString("a546e36bf0527c9d3b16154b82465edd62144c0ac1fc5a18506a2244ba449ac4");
        var original = (byte[])scalar.Clone();
        var u = Convert.FromHexString("e6db6867583030db3594c1a424b15f7c726624ec26b3353b10a903a6d0ab1c4c");

        _ = Curve25519.ScalarMult(scalar, u, new byte[Curve25519.PointSizeInBytes]);

        CollectionAssert.AreEqual(original, scalar);
    }

    // ── ScalarMultBase / Diffie-Hellman (RFC 7748 §6.1) ───────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="Curve25519.ScalarMultBase" /> derives the public keys published in the RFC 7748 §6.1
    /// Diffie-Hellman example.
    /// </summary>
    [TestMethod]
    [DataRow(
        "RFC 7748 §6.1 Alice",
        "77076d0a7318a57d3c16c17251b26645df4c2f87ebc0992ab177fba51db92c2a",
        "8520f0098930a754748b7ddcb43ef75a0dbf3a0d26381af4eba4a98eaa9b4e6a")]
    [DataRow(
        "RFC 7748 §6.1 Bob",
        "5dab087e624a8a4b79e17f8b83800ee66f3bb1292618b6fd1c2f8b27ff88e0eb",
        "de9edb7d7b7dc1b4d35b61c2ece435373f8343c85b78674dadfc7e146f882b4f")]
    public void ScalarMultBase_WhenGivenRfc7748PrivateKey_ShouldProducePublishedPublicKey(
        string testName, string privateHex, string expectedPublicHex)
    {
        _ = testName;

        var destination = new byte[Curve25519.PointSizeInBytes];
        Curve25519.ScalarMultBase(Convert.FromHexString(privateHex), destination);

        CollectionAssert.AreEqual(Convert.FromHexString(expectedPublicHex), destination);
    }

    /// <summary>
    /// Verifies that both sides of the RFC 7748 §6.1 Diffie-Hellman exchange derive the published shared secret.
    /// </summary>
    [TestMethod]
    public void ScalarMult_WhenRunAsRfc7748DiffieHellman_ShouldDerivePublishedSharedSecret()
    {
        var alicePrivate = Convert.FromHexString("77076d0a7318a57d3c16c17251b26645df4c2f87ebc0992ab177fba51db92c2a");
        var bobPrivate = Convert.FromHexString("5dab087e624a8a4b79e17f8b83800ee66f3bb1292618b6fd1c2f8b27ff88e0eb");
        var alicePublic = Convert.FromHexString("8520f0098930a754748b7ddcb43ef75a0dbf3a0d26381af4eba4a98eaa9b4e6a");
        var bobPublic = Convert.FromHexString("de9edb7d7b7dc1b4d35b61c2ece435373f8343c85b78674dadfc7e146f882b4f");
        var expectedShared = Convert.FromHexString("4a5d9d5ba4ce2de1728e3bf480350f25e07e21c947d19e3376f09b3c1e161742");

        var aliceShared = new byte[Curve25519.PointSizeInBytes];
        var bobShared = new byte[Curve25519.PointSizeInBytes];
        _ = Curve25519.ScalarMult(alicePrivate, bobPublic, aliceShared);
        _ = Curve25519.ScalarMult(bobPrivate, alicePublic, bobShared);

        CollectionAssert.AreEqual(expectedShared, aliceShared);
        CollectionAssert.AreEqual(expectedShared, bobShared);
    }

    // ── Iterated ladder (RFC 7748 §5.2) ───────────────────────────────────────────────────────

    /// <summary>
    /// Verifies the first step of the RFC 7748 §5.2 iterated test, where the scalar and u-coordinate both start as
    /// the base point encoding.
    /// </summary>
    [TestMethod]
    public void ScalarMult_WhenIteratedOnce_ShouldMatchRfc7748Result() =>
        AssertIteratedLadder(1, "422c8e7a6227d7bca1350b3e2bb7279f7897b87bb6854b783c60e80311ae3079");

    /// <summary>
    /// Verifies the 1,000-iteration result of the RFC 7748 §5.2 iterated test, exercising sustained chaining of
    /// ladder outputs back into scalars.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void ScalarMult_WhenIteratedOneThousandTimes_ShouldMatchRfc7748Result() =>
        AssertIteratedLadder(1_000, "684cf59ba83309552800ef566f2f4d3c1c3887c49360e3875f2eb94d99532c51");

    /// <summary>
    /// Verifies the 1,000,000-iteration result of the RFC 7748 §5.2 iterated test.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Stress)]
    public void ScalarMult_WhenIteratedOneMillionTimes_ShouldMatchRfc7748Result() =>
        AssertIteratedLadder(1_000_000, "7c3911e0ab2586fd864497297e575e6f3bc601c0883c30df5f4dd2d24f665424");

    /// <summary>
    /// Runs the RFC 7748 §5.2 iterated ladder, feeding each output back as the next scalar with the previous scalar
    /// as the u-coordinate, and asserts the final value.
    /// </summary>
    /// <param name="iterations">The number of ladder iterations to run.</param>
    /// <param name="expectedHex">The published value of the scalar after the final iteration.</param>
    private static void AssertIteratedLadder(int iterations, string expectedHex)
    {
        var k = new byte[Curve25519.PointSizeInBytes];
        var u = new byte[Curve25519.PointSizeInBytes];
        k[0] = 9;
        u[0] = 9;

        var result = new byte[Curve25519.PointSizeInBytes];
        for (var i = 0; i < iterations; i++)
        {
            _ = Curve25519.ScalarMult(k, u, result);
            Array.Copy(k, u, k.Length);
            Array.Copy(result, k, result.Length);
        }

        CollectionAssert.AreEqual(Convert.FromHexString(expectedHex), k);
    }
}

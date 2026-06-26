// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519Tests.ImportPublicKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;
using Bodu.Test.Kat;

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
    /// Yields the eight small-order Ed25519 points as <see cref="InvalidKat{TInput}" /> rows. They decode as valid
    /// curve points but lie in the order-8 cofactor subgroup, so import must reject them with
    /// <see cref="ArgumentException" />.
    /// </summary>
    /// <returns>One row per small-order encoding.</returns>
    private static IEnumerable<object[]> SmallOrderPublicKeys()
    {
        (string Name, string Hex)[] points =
        {
            ("order 1 (identity)", "0100000000000000000000000000000000000000000000000000000000000000"),
            ("order 2", "ecffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f"),
            ("order 4 (x positive)", "0000000000000000000000000000000000000000000000000000000000000000"),
            ("order 4 (x negative)", "0000000000000000000000000000000000000000000000000000000000000080"),
            ("order 8 (a)", "26e8958fc2b227b045c3f489f2ef98f0d5dfac05d3c63339b13802886d53fc05"),
            ("order 8 (b)", "c7176a703d4dd84fba3c0b760d10670f2a2053fa2c39ccc64ec7fd7792ac037a"),
            ("order 8 (c)", "26e8958fc2b227b045c3f489f2ef98f0d5dfac05d3c63339b13802886d53fc85"),
            ("order 8 (d)", "c7176a703d4dd84fba3c0b760d10670f2a2053fa2c39ccc64ec7fd7792ac03fa"),
        };

        foreach ((string name, string hex) in points)
            yield return new object[] { new InvalidKat<byte[]>(name, Convert.FromHexString(hex), typeof(ArgumentException), "publicKey") };
    }

    /// <summary>
    /// Verifies that <see cref="Ed25519.ImportPublicKey" /> rejects a small-order point, which is incompatible with
    /// Bodu's strict cofactorless verification policy.
    /// </summary>
    /// <param name="vector">The small-order KAT vector under test.</param>
    [TestMethod]
    [DynamicData(nameof(SmallOrderPublicKeys), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ImportPublicKey_WhenKeyIsSmallOrder_ShouldThrowArgumentException(InvalidKat<byte[]> vector)
    {
        using var algorithm = new Ed25519();

        ExceptionAssert.AssertGuard(
            vector.Name,
            () => algorithm.ImportPublicKey(vector.Input),
            vector.ExceptionType,
            vector.ParamName);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ComplementaryTypeSmokeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography;

namespace Bodu.Smoke;

/// <summary>
/// Provides smoke-tier coverage for the complementary semantic value types in
/// <c>Bodu.Security.Cryptography</c>. Each test exercises one happy-path entry point so catastrophic breakage is
/// caught by the smallest possible build run.
/// </summary>
[TestClass]
public sealed class ComplementaryTypeSmokeTests
{
    /// <summary>
    /// Verifies that <see cref="HashValue" /> round-trips a digest through hexadecimal parse and format.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void HashValue_HexRoundTrip_ShouldPreserveValue()
    {
        const string hex = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        var hash = HashValue.ParseHex(hex);

        Assert.AreEqual(hex, hash.ToHexString());
        Assert.IsTrue(hash.FixedTimeEquals(HashValue.FromBytes(Convert.FromHexString(hex))));
    }

    /// <summary>
    /// Verifies that <see cref="SecretBytes" /> generates random secret material that survives a copy round-trip
    /// under fixed-time comparison.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void SecretBytes_RandomCopyRoundTrip_ShouldCompareEqual()
    {
        using var secret = SecretBytes.Random(32);

        Assert.AreEqual(32, secret.Length);
        Assert.IsTrue(secret.FixedTimeEquals(secret.ToArray()));
    }
}

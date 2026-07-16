// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeccakSpongeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="KeccakSponge" /> incremental sponge, validated against published FIPS 202
/// digests and cross-checked against the existing <see cref="Shake" /> implementation.
/// </summary>
[TestClass]
public partial class KeccakSpongeTests
{
    // ── Fixed-output SHA-3 known answers (FIPS 202) ───────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="KeccakSponge.Sha3_256(ReadOnlySpan{byte}, Span{byte})" /> reproduces the published
    /// FIPS 202 digests for the empty message and "abc".
    /// </summary>
    [TestMethod]
    [DataRow(
        "empty message",
        "",
        "a7ffc6f8bf1ed76651c14756a061d662f580ff4de43b49fa82d80a4b80f8434a")]
    [DataRow(
        "abc",
        "616263",
        "3a985da74fe225b2045c172d6bd390bd855f086e3e9d525b46bfe24511431532")]
    public void Sha3_256_WhenGivenFips202Vector_ShouldProducePublishedDigest(string testName, string messageHex, string expectedHex)
    {
        _ = testName;

        byte[] digest = new byte[32];
        KeccakSponge.Sha3_256(Convert.FromHexString(messageHex), digest);

        CollectionAssert.AreEqual(Convert.FromHexString(expectedHex), digest);
    }

    /// <summary>
    /// Verifies that <see cref="KeccakSponge.Sha3_512(ReadOnlySpan{byte}, Span{byte})" /> reproduces the published
    /// FIPS 202 digests for the empty message and "abc".
    /// </summary>
    [TestMethod]
    [DataRow(
        "empty message",
        "",
        "a69f73cca23a9ac5c8b567dc185a756e97c982164fe25859e0d1dcc1475c80a615b2123af1f5f94c11e3e9402c3ac558f500199d95b6d3e301758586281dcd26")]
    [DataRow(
        "abc",
        "616263",
        "b751850b1a57168a5693cd924b6b096e08f621827444f70d884f5d0240d2712e10e116e9192af3c91a7ec57647e3934057340b4cf408d5a56592f8274eec53f0")]
    public void Sha3_512_WhenGivenFips202Vector_ShouldProducePublishedDigest(string testName, string messageHex, string expectedHex)
    {
        _ = testName;

        byte[] digest = new byte[64];
        KeccakSponge.Sha3_512(Convert.FromHexString(messageHex), digest);

        CollectionAssert.AreEqual(Convert.FromHexString(expectedHex), digest);
    }

    // ── SHAKE known answers and cross-checks ──────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the SHAKE one-shots reproduce the published FIPS 202 outputs for the empty message.
    /// </summary>
    [TestMethod]
    public void Shake_WhenGivenEmptyMessage_ShouldProducePublishedPrefixes()
    {
        byte[] shake128 = new byte[32];
        KeccakSponge.Shake128(ReadOnlySpan<byte>.Empty, shake128);
        CollectionAssert.AreEqual(
            Convert.FromHexString("7f9c2ba4e88f827d616045507605853ed73b8093f6efbc88eb1a6eacfa66ef26"),
            shake128);

        byte[] shake256 = new byte[32];
        KeccakSponge.Shake256(ReadOnlySpan<byte>.Empty, shake256);
        CollectionAssert.AreEqual(
            Convert.FromHexString("46b9dd2b0ba88d13233b3feb743eeb243fcd52ea62b81b82b50c27646ed5762f"),
            shake256);
    }

    /// <summary>
    /// Verifies that the sponge SHAKE output matches the existing <see cref="Shake" /> hash algorithm for a range of
    /// message and output lengths spanning multiple rate blocks.
    /// </summary>
    [TestMethod]
    public void Shake_WhenComparedToExistingShakeType_ShouldProduceIdenticalOutput()
    {
        byte[] message = Encoding.UTF8.GetBytes(new string('q', 600));

        foreach (int securityLevel in new[] { 128, 256 })
        {
            foreach (int outputBytes in new[] { 16, 32, 137, 200, 400 })
            {
                using var reference = new Shake(outputBytes * 8, securityLevel);
                byte[] expected = reference.ComputeHash(message);

                byte[] actual = new byte[outputBytes];
                if (securityLevel == 128)
                    KeccakSponge.Shake128(message, actual);
                else
                    KeccakSponge.Shake256(message, actual);

                CollectionAssert.AreEqual(expected, actual, $"SHAKE{securityLevel} mismatch at {outputBytes} output bytes.");
            }
        }
    }

    // ── Incremental semantics ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that absorbing byte-by-byte and squeezing in irregular chunks produces the same stream as a single
    /// one-shot call.
    /// </summary>
    [TestMethod]
    public void Squeeze_WhenAbsorbedAndSqueezedIncrementally_ShouldMatchOneShotStream()
    {
        byte[] message = Encoding.UTF8.GetBytes("incremental sponge equivalence");

        byte[] oneShot = new byte[500];
        KeccakSponge.Shake256(message, oneShot);

        var sponge = KeccakSponge.CreateShake256();
        foreach (byte b in message)
            sponge.Absorb(new[] { b });

        byte[] streamed = new byte[500];
        int offset = 0;
        foreach (int chunk in new[] { 1, 7, 135, 136, 137, 84 })
        {
            sponge.Squeeze(streamed.AsSpan(offset, chunk));
            offset += chunk;
        }

        Assert.AreEqual(streamed.Length, offset);
        CollectionAssert.AreEqual(oneShot, streamed);
        sponge.Clear();
    }

    /// <summary>
    /// Verifies that absorbing after squeezing has begun throws <see cref="InvalidOperationException" />, and that
    /// <see cref="KeccakSponge.Reset" /> returns the sponge to a usable initial state.
    /// </summary>
    [TestMethod]
    public void Absorb_WhenCalledAfterSqueeze_ShouldThrowInvalidOperationExceptionUntilReset()
    {
        var sponge = KeccakSponge.CreateShake128();
        sponge.Absorb(new byte[] { 1, 2, 3 });
        sponge.Squeeze(new byte[8]);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            KeccakSponge local = sponge;
            local.Absorb(new byte[] { 4 });
        });

        sponge.Reset();
        sponge.Absorb(ReadOnlySpan<byte>.Empty);

        byte[] fresh = new byte[32];
        sponge.Squeeze(fresh);

        byte[] expected = new byte[32];
        KeccakSponge.Shake128(ReadOnlySpan<byte>.Empty, expected);
        CollectionAssert.AreEqual(expected, fresh);
    }

    /// <summary>
    /// Verifies that the two-segment SHAKE256 and SHA3 overloads match hashing the concatenated input.
    /// </summary>
    [TestMethod]
    public void Shake256_WhenUsingTwoSegmentOverload_ShouldMatchConcatenatedInput()
    {
        byte[] first = Encoding.UTF8.GetBytes("first-segment");
        byte[] second = Encoding.UTF8.GetBytes("second-segment");
        byte[] combined = first.Concat(second).ToArray();

        byte[] viaSegments = new byte[64];
        byte[] viaCombined = new byte[64];
        KeccakSponge.Shake256(first, second, viaSegments);
        KeccakSponge.Shake256(combined, viaCombined);
        CollectionAssert.AreEqual(viaCombined, viaSegments);

        byte[] sha3Segments = new byte[32];
        byte[] sha3Combined = new byte[32];
        KeccakSponge.Sha3_256(first, second, sha3Segments);
        KeccakSponge.Sha3_256(combined, sha3Combined);
        CollectionAssert.AreEqual(sha3Combined, sha3Segments);

        byte[] sha512Segments = new byte[64];
        byte[] sha512Combined = new byte[64];
        KeccakSponge.Sha3_512(first, second, sha512Segments);
        KeccakSponge.Sha3_512(combined, sha512Combined);
        CollectionAssert.AreEqual(sha512Combined, sha512Segments);
    }
}

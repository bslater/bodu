// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XChaCha20Tests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


using System.Reflection;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;
/// <summary>
/// Locks the <see cref="XChaCha20" /> extended-nonce stream cipher and its HChaCha20 subkey-derivation core against the
/// published <c>draft-irtf-cfrg-xchacha</c> known-answer test vectors, and inherits the shared
/// <see cref="SymmetricStreamAlgorithmTests{TTest, TAlgorithm}" /> behavioural contract.
/// </summary>
[TestClass]
public sealed partial class XChaCha20Tests
    : SymmetricStreamAlgorithmTests<XChaCha20Tests, XChaCha20>
{
    /// <inheritdoc />
    protected override SymmetricStreamAlgorithmSpecification GetSpecification() =>
        new()
        {
            DefaultKeySizeBits = 256,
            NonceSizeBits = 192,
            LegalKeySizesBits = [256],
        };

    /// <summary>
    /// Represents one HChaCha20 block-function known-answer test row.
    /// </summary>
    private sealed record HChaCha20Kat
    {
        public required string Name { get; init; }

        public required string Source { get; init; }

        public required string KeyHex { get; init; }

        public required string Nonce16Hex { get; init; }

        public required string SubkeyHex { get; init; }
    }

    /// <summary>
    /// Represents one XChaCha20 keystream known-answer test row, recovered as the ciphertext of an all-zero plaintext.
    /// </summary>
    private sealed record XChaCha20KeystreamKat
    {
        public required string Name { get; init; }

        public required string Source { get; init; }

        public required string KeyHex { get; init; }

        public required string Nonce24Hex { get; init; }

        public required uint Counter { get; init; }

        public required string KeystreamHex { get; init; }
    }

    // ── draft-irtf-cfrg-xchacha §2.2.1 — HChaCha20 block function ─────────────────────────────
    //
    // The canonical draft vector plus the libsodium tv_hchacha20 corpus.
    // Source: draft-irtf-cfrg-xchacha §2.2.1; libsodium test/default/xchacha20.c.
    private static readonly HChaCha20Kat[] HChaCha20KnownAnswers =
    [
        new HChaCha20Kat
        {
            Name = "draft-irtf-cfrg-xchacha 2.2.1 HChaCha20",
            Source = "draft-irtf-cfrg-xchacha Section 2.2.1",
            KeyHex = "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f",
            Nonce16Hex = "000000090000004a0000000031415927",
            SubkeyHex = "82413b4227b27bfed30e42508a877d73a0f9e4d58a74a853c12ec41326d3ecdc",
        },
        new HChaCha20Kat
        {
            Name = "libsodium hchacha20 vector 1",
            Source = "libsodium tv_hchacha20",
            KeyHex = "24f11cce8a1b3d61e441561a696c1c1b7e173d084fd4812425435a8896a013dc",
            Nonce16Hex = "d9660c5900ae19ddad28d6e06e45fe5e",
            SubkeyHex = "5966b3eec3bff1189f831f06afe4d4e3be97fa9235ec8c20d08acfbbb4e851e3",
        },
        new HChaCha20Kat
        {
            Name = "libsodium hchacha20 vector 2",
            Source = "libsodium tv_hchacha20",
            KeyHex = "80a5f6272031e18bb9bcd84f3385da65e7731b7039f13f5e3d475364cd4d42f7",
            Nonce16Hex = "c0eccc384b44c88e92c57eb2d5ca4dfa",
            SubkeyHex = "6ed11741f724009a640a44fce7320954c46e18e0d7ae063bdbc8d7cf372709df",
        },
    ];

    // ── draft-irtf-cfrg-xchacha Appendix A.2 — XChaCha20 stream cipher ────────────────────────
    //
    // The first 304 keystream bytes for counter 0 and counter 1.
    // Source: draft-irtf-cfrg-xchacha Appendix A.2.1 / A.2.2.
    private static readonly XChaCha20KeystreamKat[] KeystreamKnownAnswers =
    [
        new XChaCha20KeystreamKat
        {
            Name = "draft-irtf-cfrg-xchacha A.2.1 XChaCha20 keystream counter 0",
            Source = "draft-irtf-cfrg-xchacha Appendix A.2.1",
            KeyHex = "808182838485868788898a8b8c8d8e8f909192939495969798999a9b9c9d9e9f",
            Nonce24Hex = "404142434445464748494a4b4c4d4e4f5051525354555658",
            Counter = 0,
            KeystreamHex =
                "1131ce9a2a20ae0d67c8935c7789fa1025c9e5bb720fb96f11354fb97af0bd9a" +
                "adec0863ba60cac8582c48f86cdfc48edd46a48642c5de62ccf11c7b21bf337d" +
                "29624b4b1b140ace53740e405b2168540fd7d630c1f536fecd722fc3cddba7f4" +
                "cca98cf9e47e5e64d115450f9b125b54449ff76141ca620a1f9cfcab2a1a8a25" +
                "5e766a5266b878846120ea64ad99aa479471e63befcbd37cd1c22a221fe46221" +
                "5cf32c74895bf505863ccddd48f62916dc6521f1ec50a5ae08903aa259d9bf60" +
                "7cd8026fba548604f1b6072d91bc91243a5b845f7fd171b02edc5a0a84cf28dd" +
                "241146bc376e3f48df5e7fee1d11048c190a3d3deb0feb64b42d9c6fdeee290f" +
                "a0e6ae2c26c0249ea8c181f7e2ffd100cbe5fd3c4f8271d62b15330cb8fdcf00" +
                "b3df507ca8c924f7017b7e712d15a2eb",
        },
        new XChaCha20KeystreamKat
        {
            Name = "draft-irtf-cfrg-xchacha A.2.2 XChaCha20 keystream counter 1",
            Source = "draft-irtf-cfrg-xchacha Appendix A.2.2",
            KeyHex = "808182838485868788898a8b8c8d8e8f909192939495969798999a9b9c9d9e9f",
            Nonce24Hex = "404142434445464748494a4b4c4d4e4f5051525354555658",
            Counter = 1,
            KeystreamHex =
                "29624b4b1b140ace53740e405b2168540fd7d630c1f536fecd722fc3cddba7f4" +
                "cca98cf9e47e5e64d115450f9b125b54449ff76141ca620a1f9cfcab2a1a8a25" +
                "5e766a5266b878846120ea64ad99aa479471e63befcbd37cd1c22a221fe46221" +
                "5cf32c74895bf505863ccddd48f62916dc6521f1ec50a5ae08903aa259d9bf60" +
                "7cd8026fba548604f1b6072d91bc91243a5b845f7fd171b02edc5a0a84cf28dd" +
                "241146bc376e3f48df5e7fee1d11048c190a3d3deb0feb64b42d9c6fdeee290f" +
                "a0e6ae2c26c0249ea8c181f7e2ffd100cbe5fd3c4f8271d62b15330cb8fdcf00" +
                "b3df507ca8c924f7017b7e712d15a2eb5c50484451e54e1b4b995bd8fdd94597" +
                "bb94d7af0b2c04df10ba0890899ed9293a0f55b8bafa999264035f1d4fbe7fe0" +
                "aafa109a62372027e50e10cdfecca127",
        },
    ];

    /// <summary>
    /// Yields the HChaCha20 known-answer vectors as <see cref="DynamicDataAttribute" /> rows.
    /// </summary>
    /// <returns>One row per vector.</returns>
    private static IEnumerable<object[]> HChaCha20KatData()
    {
        foreach (HChaCha20Kat kat in HChaCha20KnownAnswers)
            yield return new object[] { kat.KeyHex, kat.Nonce16Hex, kat.SubkeyHex, kat.Name };
    }

    /// <summary>
    /// Yields the XChaCha20 keystream known-answer vectors as <see cref="DynamicDataAttribute" /> rows.
    /// </summary>
    /// <returns>One row per vector.</returns>
    private static IEnumerable<object[]> XChaCha20KeystreamKatData()
    {
        foreach (XChaCha20KeystreamKat kat in KeystreamKnownAnswers)
            yield return new object[] { kat.KeyHex, kat.Nonce24Hex, kat.Counter, kat.KeystreamHex, kat.Name };
    }

    /// <summary>
    /// Produces a human-readable display name for a KAT row whose scenario label is the last element.
    /// </summary>
    /// <param name="testMethod">The executing test method.</param>
    /// <param name="data">The row data; the final element carries the scenario name.</param>
    /// <returns>The test method name followed by the row's scenario label.</returns>
    public static string GetKatDisplayName(MethodInfo testMethod, object[] data) =>
        $"{testMethod.Name} — {data[^1]}";

    /// <summary>
    /// Verifies that the internal HChaCha20 subkey-derivation core reproduces each published subkey vector.
    /// </summary>
    /// <param name="keyHex">The 256-bit key, in hex.</param>
    /// <param name="nonce16Hex">The 128-bit HChaCha20 input nonce, in hex.</param>
    /// <param name="subkeyHex">The expected 256-bit subkey, in hex.</param>
    /// <param name="displayName">The scenario label.</param>
    [TestMethod]
    [DynamicData(nameof(HChaCha20KatData), DynamicDataDisplayName = nameof(GetKatDisplayName))]
    public void HChaCha20_WhenGivenKnownVector_ShouldDeriveExpectedSubkey(
        string keyHex, string nonce16Hex, string subkeyHex, string displayName)
    {
        byte[] key = Convert.FromHexString(keyHex);
        byte[] nonce = Convert.FromHexString(nonce16Hex);
        byte[] expected = Convert.FromHexString(subkeyHex);

        byte[] subkey = new byte[32];
        ChaCha20StreamCipher.HChaCha20(key, nonce, subkey);

        CollectionAssert.AreEqual(expected, subkey, $"HChaCha20 subkey mismatch for {displayName}.");
    }

    /// <summary>
    /// Verifies that <see cref="XChaCha20" /> reproduces each published keystream vector, recovered as the ciphertext
    /// of an all-zero plaintext.
    /// </summary>
    /// <param name="keyHex">The 256-bit key, in hex.</param>
    /// <param name="nonce24Hex">The 192-bit extended nonce, in hex.</param>
    /// <param name="counter">The initial block counter.</param>
    /// <param name="keystreamHex">The expected keystream, in hex.</param>
    /// <param name="displayName">The scenario label.</param>
    [TestMethod]
    [DynamicData(nameof(XChaCha20KeystreamKatData), DynamicDataDisplayName = nameof(GetKatDisplayName))]
    public void CreateEncryptor_WhenGivenDraftKeystreamVector_ShouldMatchExpected(
        string keyHex, string nonce24Hex, uint counter, string keystreamHex, string displayName)
    {
        byte[] key = Convert.FromHexString(keyHex);
        byte[] nonce = Convert.FromHexString(nonce24Hex);
        byte[] expected = Convert.FromHexString(keystreamHex);
        byte[] zeros = new byte[expected.Length];

        using var cipher = new XChaCha20 { InitialCounter = counter };
        using ICryptoTransform encryptor = cipher.CreateEncryptor(key, nonce);
        byte[] keystream = encryptor.TransformFinalBlock(zeros, 0, zeros.Length);

        CollectionAssert.AreEqual(expected, keystream, $"XChaCha20 keystream mismatch for {displayName}.");
    }

    /// <summary>
    /// Verifies that <see cref="XChaCha20" /> equals running <see cref="ChaCha20" /> under the HChaCha20-derived subkey
    /// and the trailing 64-bit nonce, confirming the extended-nonce construction is wired correctly.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenComparedToDerivedChaCha20_ShouldProduceSameKeystream()
    {
        byte[] key = RandomNumberGenerator.GetBytes(32);
        byte[] nonce = RandomNumberGenerator.GetBytes(24);
        byte[] zeros = new byte[256];

        // XChaCha20 directly.
        byte[] xActual;
        using (var x = new XChaCha20())
        using (ICryptoTransform e = x.CreateEncryptor(key, nonce))
            xActual = e.TransformFinalBlock(zeros, 0, zeros.Length);

        // ChaCha20 under the derived subkey with nonce = 0x00000000 || nonce[16..24].
        byte[] subkey = new byte[32];
        ChaCha20StreamCipher.HChaCha20(key, nonce.AsSpan(0, 16), subkey);
        byte[] chachaNonce = new byte[12];
        nonce.AsSpan(16, 8).CopyTo(chachaNonce.AsSpan(4));

        byte[] cActual;
        using (var c = new ChaCha20())
        using (ICryptoTransform e = c.CreateEncryptor(subkey, chachaNonce))
            cActual = e.TransformFinalBlock(zeros, 0, zeros.Length);

        CollectionAssert.AreEqual(cActual, xActual,
            "XChaCha20 keystream must equal ChaCha20 under the HChaCha20-derived subkey.");
    }

    /// <summary>
    /// Verifies that <see cref="XChaCha20.InitialCounter" /> is captured when the transform is created, so mutating it
    /// on the algorithm afterwards does not change an already-created transform's keystream.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenInitialCounterChangedAfterCreation_ShouldUseCapturedCounter()
    {
        byte[] key = new byte[32];
        byte[] nonce = new byte[24];
        byte[] plaintext = new byte[128];

        using var cipher = new XChaCha20 { InitialCounter = 5 };
        using ICryptoTransform transform = cipher.CreateEncryptor(key, nonce);
        cipher.InitialCounter = 9;
        byte[] actual = transform.TransformFinalBlock(plaintext, 0, plaintext.Length);

        using var reference = new XChaCha20 { InitialCounter = 5 };
        using ICryptoTransform referenceTransform = reference.CreateEncryptor(key, nonce);
        byte[] expected = referenceTransform.TransformFinalBlock(plaintext, 0, plaintext.Length);

        CollectionAssert.AreEqual(expected, actual,
            "The transform must use the counter captured at creation, not the algorithm's later value.");
    }
}

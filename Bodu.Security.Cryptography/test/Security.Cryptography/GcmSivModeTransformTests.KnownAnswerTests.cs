// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GcmSivModeTransformTests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Security.Cryptography.Infrastructure;
using Bodu.Test.Kat;
using static Bodu.Security.Cryptography.Infrastructure.KatBytes;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Known-answer tests for <see cref="GcmSivModeTransform" /> against RFC 8452 Appendix C.1 (AES-128-GCM-SIV).
/// </summary>
/// <remarks>
/// The complete C.1 vector set (24 vectors) is pinned, covering empty, block-aligned, and non-block-aligned
/// plaintext/AAD lengths and multiple key/nonce pairs. These verify the exact ciphertext and tag against the
/// published values — which a symmetric encrypt/decrypt round-trip cannot — and guard the POLYVAL (reflected-key
/// <c>mulX</c>) and CTR (first-32-bit little-endian counter) behaviour required by RFC 8452 Sections 3 and 4.
/// </remarks>
public sealed partial class GcmSivModeTransformTests
{
    // RFC 8452 Appendix C.1 — AES-128-GCM-SIV. The full vector set exercises empty, block-aligned, and
    // non-block-aligned plaintext/AAD lengths, verifying the partial-final-block POLYVAL padding against the
    // published ciphertext+tag (a symmetric round-trip cannot).
    private static AeadKnownAnswer C1(string name, string key, string nonce, string aad, string pt, string ct, string tag) =>
        new()
        {
            Name = "RFC 8452 " + name,
            Provenance = KatProvenance.Rfc("RFC 8452 Appendix C.1"),
            Key = Hex(key),
            Nonce = Hex(nonce),
            AssociatedData = aad.Length == 0 ? [] : Hex(aad),
            Plaintext = pt.Length == 0 ? [] : Hex(pt),
            Ciphertext = ct.Length == 0 ? [] : Hex(ct),
            Tag = Hex(tag),
            Layout = AeadKatOutputLayout.CiphertextThenTag,
        };

    private static readonly AeadKnownAnswer[] KnownAnswers =
    [
        C1("C.1-001", "01000000000000000000000000000000", "030000000000000000000000", "", "", "", "dc20e2d83f25705bb49e439eca56de25"),
        C1("C.1-002", "01000000000000000000000000000000", "030000000000000000000000", "", "0100000000000000", "b5d839330ac7b786", "578782fff6013b815b287c22493a364c"),
        C1("C.1-003", "01000000000000000000000000000000", "030000000000000000000000", "", "010000000000000000000000", "7323ea61d05932260047d942", "a4978db357391a0bc4fdec8b0d106639"),
        C1("C.1-004", "01000000000000000000000000000000", "030000000000000000000000", "", "01000000000000000000000000000000", "743f7c8077ab25f8624e2e948579cf77", "303aaf90f6fe21199c6068577437a0c4"),
        C1("C.1-005", "01000000000000000000000000000000", "030000000000000000000000", "", "0100000000000000000000000000000002000000000000000000000000000000", "84e07e62ba83a6585417245d7ec413a9fe427d6315c09b57ce45f2e3936a9445", "1a8e45dcd4578c667cd86847bf6155ff"),
        C1("C.1-006", "01000000000000000000000000000000", "030000000000000000000000", "", "010000000000000000000000000000000200000000000000000000000000000003000000000000000000000000000000", "3fd24ce1f5a67b75bf2351f181a475c7b800a5b4d3dcf70106b1eea82fa1d64df42bf7226122fa92e17a40eeaac1201b", "5e6e311dbf395d35b0fe39c2714388f8"),
        C1("C.1-007", "01000000000000000000000000000000", "030000000000000000000000", "", "01000000000000000000000000000000020000000000000000000000000000000300000000000000000000000000000004000000000000000000000000000000", "2433668f1058190f6d43e360f4f35cd8e475127cfca7028ea8ab5c20f7ab2af02516a2bdcbc08d521be37ff28c152bba36697f25b4cd169c6590d1dd39566d3f", "8a263dd317aa88d56bdf3936dba75bb8"),
        C1("C.1-008", "01000000000000000000000000000000", "030000000000000000000000", "01", "0200000000000000", "1e6daba35669f427", "3b0a1a2560969cdf790d99759abd1508"),
        C1("C.1-009", "01000000000000000000000000000000", "030000000000000000000000", "01", "020000000000000000000000", "296c7889fd99f41917f44620", "08299c5102745aaa3a0c469fad9e075a"),
        C1("C.1-010", "01000000000000000000000000000000", "030000000000000000000000", "01", "02000000000000000000000000000000", "e2b0c5da79a901c1745f700525cb335b", "8f8936ec039e4e4bb97ebd8c4457441f"),
        C1("C.1-011", "01000000000000000000000000000000", "030000000000000000000000", "01", "0200000000000000000000000000000003000000000000000000000000000000", "620048ef3c1e73e57e02bb8562c416a319e73e4caac8e96a1ecb2933145a1d71", "e6af6a7f87287da059a71684ed3498e1"),
        C1("C.1-012", "01000000000000000000000000000000", "030000000000000000000000", "01", "020000000000000000000000000000000300000000000000000000000000000004000000000000000000000000000000", "50c8303ea93925d64090d07bd109dfd9515a5a33431019c17d93465999a8b0053201d723120a8562b838cdff25bf9d1e", "6a8cc3865f76897c2e4b245cf31c51f2"),
        C1("C.1-013", "01000000000000000000000000000000", "030000000000000000000000", "01", "02000000000000000000000000000000030000000000000000000000000000000400000000000000000000000000000005000000000000000000000000000000", "2f5c64059db55ee0fb847ed513003746aca4e61c711b5de2e7a77ffd02da42feec601910d3467bb8b36ebbaebce5fba30d36c95f48a3e7980f0e7ac299332a80", "cdc46ae475563de037001ef84ae21744"),
        C1("C.1-014", "01000000000000000000000000000000", "030000000000000000000000", "010000000000000000000000", "02000000", "a8fe3e87", "07eb1f84fb28f8cb73de8e99e2f48a14"),
        C1("C.1-015", "01000000000000000000000000000000", "030000000000000000000000", "010000000000000000000000000000000200", "0300000000000000000000000000000004000000", "6bb0fecf5ded9b77f902c7d5da236a4391dd0297", "24afc9805e976f451e6d87f6fe106514"),
        C1("C.1-016", "01000000000000000000000000000000", "030000000000000000000000", "0100000000000000000000000000000002000000", "030000000000000000000000000000000400", "44d0aaf6fb2f1f34add5e8064e83e12a2ada", "bff9b2ef00fb47920cc72a0c0f13b9fd"),
        C1("C.1-017", "e66021d5eb8e4f4066d4adb9c33560e4", "f46e44bb3da0015c94f70887", "", "", "", "a4194b79071b01a87d65f706e3949578"),
        C1("C.1-018", "36864200e0eaf5284d884a0e77d31646", "bae8e37fc83441b16034566b", "46bb91c3c5", "7a806c", "af60eb", "711bd85bc1e4d3e0a462e074eea428a8"),
        C1("C.1-019", "aedb64a6c590bc84d1a5e269e4b47801", "afc0577e34699b9e671fdd4f", "fc880c94a95198874296", "bdc66f146545", "bb93a3e34d3c", "d6a9c45545cfc11f03ad743dba20f966"),
        C1("C.1-020", "d5cc1fd161320b6920ce07787f86743b", "275d1ab32f6d1f0434d8848c", "046787f3ea22c127aaf195d1894728", "1177441f195495860f", "4f37281f7ad12949d0", "1d02fd0cd174c84fc5dae2f60f52fd2b"),
        C1("C.1-021", "b3fed1473c528b8426a582995929a149", "9e9ad8780c8d63d0ab4149c0", "c9882e5386fd9f92ec489c8fde2be2cf97e74e93", "9f572c614b4745914474e7c7", "f54673c5ddf710c745641c8b", "c1dc2f871fb7561da1286e655e24b7b0"),
        C1("C.1-022", "2d4ed87da44102952ef94b02b805249b", "ac80e6f61455bfac8308a2d4", "2950a70d5a1db2316fd568378da107b52b0da55210cc1c1b0a", "0d8c8451178082355c9e940fea2f58", "c9ff545e07b88a015f05b274540aa1", "83b3449b9f39552de99dc214a1190b0b"),
        C1("C.1-023", "bde3b2f204d1e9f8b06bc47f9745b3d1", "ae06556fb6aa7890bebc18fe", "1860f762ebfbd08284e421702de0de18baa9c9596291b08466f37de21c7f", "6b3db4da3d57aa94842b9803a96e07fb6de7", "6298b296e24e8cc35dce0bed484b7f30d580", "3e377094f04709f64d7b985310a4db84"),
        C1("C.1-024", "f901cfe8a69615a93fdf7a98cad48179", "6245709fb18853f68d833640", "7576f7028ec6eb5ea7e298342a94d4b202b370ef9768ec6561c4fe6b7e7296fa859c21", "e42a3c02c25b64869e146d7b233987bddfc240871d", "391cc328d484a4f46406181bcd62efd9b3ee197d05", "2d15506c84a9edd65e13e9d24a2a6e70"),
    ];

    /// <summary>
    /// Yields the RFC 8452 AES-128-GCM-SIV known-answer vectors as <see cref="DynamicDataAttribute" /> rows.
    /// </summary>
    /// <returns>One row per vector.</returns>
    private static IEnumerable<object[]> GcmSivRfc8452Vectors()
    {
        foreach (AeadKnownAnswer kat in KnownAnswers)
            yield return new object[] { kat };
    }

    // ── Helper ─────────────────────────────────────────────────────────────────────────────────

    private static GcmSivModeTransform MakeGcmSiv(AeadKnownAnswer vector)
    {
        byte[] iv = new byte[16];
        vector.Nonce.CopyTo(iv, 0);

        var t = new GcmSivModeTransform(
            new AesBlockCipherFixture(vector.Key),
            k => new AesBlockCipherFixture(k),
            iv);
        if (vector.AssociatedData.Length > 0) t.ProcessAssociatedData(vector.AssociatedData);
        return t;
    }

    // ── KAT tests ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="GcmSivModeTransform.Encrypt" />, with Rfc8452 Vector, matches Expected.
    /// </summary>
    /// <param name="vector">The AES-128-GCM-SIV known-answer vector under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(GcmSivRfc8452Vectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Encrypt_WithRfc8452Vector_ShouldMatchExpected(AeadKnownAnswer vector)
    {
        byte[] expected = vector.CiphertextWithTag;

        GcmSivModeTransform transform = MakeGcmSiv(vector);
        byte[] output = new byte[vector.Plaintext.Length + (transform.TagSize / 8)];
        transform.Encrypt(vector.Plaintext, output);

        CollectionAssert.AreEqual(expected, output,
            $"GCM-SIV encrypt mismatch for {vector.Name}.");
    }

    /// <summary>
    /// Verifies that <see cref="GcmSivModeTransform.Decrypt" />, with Rfc8452Vector, returns the expected value.
    /// </summary>
    /// <param name="vector">The AES-128-GCM-SIV known-answer vector under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(GcmSivRfc8452Vectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Decrypt_WithRfc8452Vector_ShouldRecoverPlaintext(AeadKnownAnswer vector)
    {
        byte[] ciphertextTag = vector.CiphertextWithTag;

        GcmSivModeTransform transform = MakeGcmSiv(vector);
        int plaintextLength = ciphertextTag.Length - (transform.TagSize / 8);
        byte[] output = new byte[plaintextLength];
        int written = transform.Decrypt(ciphertextTag, output);

        Assert.AreEqual(plaintextLength, written);
        CollectionAssert.AreEqual(vector.Plaintext, output,
            $"GCM-SIV decrypt mismatch for {vector.Name}.");
    }

    // ── Structural tests ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="GcmSivModeTransform.Decrypt" />, when TagIsCorrupted, throws <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenTagIsCorrupted_ShouldThrowExactly()
    {
        byte[] masterKey = new byte[16];
        byte[] iv = new byte[16];

        var enc = new GcmSivModeTransform(
            new AesBlockCipherFixture(masterKey), k => new AesBlockCipherFixture(k), iv);
        byte[] pt = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        byte[] ct = new byte[pt.Length + (enc.TagSize / 8)];
        enc.Encrypt(pt, ct);
        ct[ct.Length - 1] ^= 0xFF; // corrupt last tag byte

        var dec = new GcmSivModeTransform(
            new AesBlockCipherFixture(masterKey), k => new AesBlockCipherFixture(k), iv);
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            dec.Decrypt(ct, new byte[pt.Length]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="GcmSivModeTransform.EncryptThenDecrypt" />, with RandomKey, returns the expected value.
    /// </summary>
    [TestMethod]
    public void EncryptThenDecrypt_WithRandomKey_ShouldRoundTrip()
    {
        var rng = RandomNumberGenerator.Create();
        byte[] key = new byte[16];
        byte[] nonce = new byte[12];
        byte[] iv = new byte[16];
        rng.GetBytes(key); rng.GetBytes(nonce); nonce.CopyTo(iv, 0);

        byte[] plaintext = new byte[60]; rng.GetBytes(plaintext);
        byte[] aad = new byte[20]; rng.GetBytes(aad);

        using var mc1 = new AesBlockCipherFixture(key);
        var enc = new GcmSivModeTransform(mc1, k => new AesBlockCipherFixture(k), iv);
        enc.ProcessAssociatedData(aad);
        byte[] ciphertext = new byte[plaintext.Length + (enc.TagSize / 8)];
        enc.Encrypt(plaintext, ciphertext);

        using var mc2 = new AesBlockCipherFixture(key);
        var dec = new GcmSivModeTransform(mc2, k => new AesBlockCipherFixture(k), iv);
        dec.ProcessAssociatedData(aad);
        byte[] recovered = new byte[plaintext.Length];
        dec.Decrypt(ciphertext, recovered);

        CollectionAssert.AreEqual(plaintext, recovered,
            "GCM-SIV round-trip must recover the original plaintext.");
    }

    /// <summary>
    /// Verifies that GCM-SIV encrypt/decrypt round-trips and authenticates for plaintext lengths that straddle the
    /// 16-byte POLYVAL block boundary — exercising the partial-final-block padding path — and that a single-bit tag
    /// tamper is rejected at each length. Deterministic (no RNG) so the boundary coverage is stable.
    /// </summary>
    /// <param name="plaintextLength">The plaintext length in bytes.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(7)]
    [DataRow(8)]
    [DataRow(15)]
    [DataRow(16)]
    [DataRow(17)]
    [DataRow(31)]
    [DataRow(33)]
    public void EncryptThenDecrypt_AtPolyvalBlockBoundaries_ShouldRoundTripAndAuthenticate(int plaintextLength)
    {
        byte[] key = new byte[16];
        byte[] iv = new byte[16];
        for (int i = 0; i < key.Length; i++) key[i] = (byte)(i + 1);
        for (int i = 0; i < 12; i++) iv[i] = (byte)(0x30 + i);

        byte[] plaintext = new byte[plaintextLength];
        for (int i = 0; i < plaintextLength; i++) plaintext[i] = (byte)i;

        // Use a partial (non-aligned) AAD block whenever the plaintext is block-aligned, so the partial-block padding
        // path is exercised on at least one of the two POLYVAL inputs at every length.
        byte[] aad = new byte[plaintextLength % 16 == 0 ? 5 : 0];
        for (int i = 0; i < aad.Length; i++) aad[i] = (byte)(0xA0 + i);

        var enc = new GcmSivModeTransform(new AesBlockCipherFixture(key), k => new AesBlockCipherFixture(k), iv);
        if (aad.Length > 0) enc.ProcessAssociatedData(aad);
        byte[] ciphertext = new byte[plaintextLength + (enc.TagSize / 8)];
        enc.Encrypt(plaintext, ciphertext);

        var dec = new GcmSivModeTransform(new AesBlockCipherFixture(key), k => new AesBlockCipherFixture(k), iv);
        if (aad.Length > 0) dec.ProcessAssociatedData(aad);
        byte[] recovered = new byte[plaintextLength];
        dec.Decrypt(ciphertext, recovered);

        CollectionAssert.AreEqual(plaintext, recovered, $"GCM-SIV round-trip failed at plaintext length {plaintextLength}.");

        // A single-bit tag tamper must fail authentication at every length.
        ciphertext[^1] ^= 0x01;
        var tampered = new GcmSivModeTransform(new AesBlockCipherFixture(key), k => new AesBlockCipherFixture(k), iv);
        if (aad.Length > 0) tampered.ProcessAssociatedData(aad);
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            tampered.Decrypt(ciphertext, new byte[plaintextLength]);
        });
    }
}

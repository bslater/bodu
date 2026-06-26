// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamAeadKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Security.Cryptography;
using Bodu.Security.Cryptography.Infrastructure;
using static Bodu.Security.Cryptography.Infrastructure.KatBytes;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Locks the public stream-cipher AEAD constructions against provenance-labelled known-answer vectors:
/// <see cref="XChaCha20Poly1305" /> against the draft-irtf-cfrg-xchacha Internet-Draft vector, and
/// <see cref="XSalsa20Poly1305" /> against the libsodium <c>crypto_secretbox</c> reference vector (including its native
/// <c>tag ‖ ciphertext</c> layout). The Bodu-defined <see cref="XSalsa20Poly1305Aead" /> construction has no published
/// external vector, so it is checked against a derived oracle composed from the independently-tested public
/// <see cref="XSalsa20" /> keystream and <see cref="Poly1305" /> MAC, and against a frozen value locked into the test
/// source. The RFC 8439 framing that all of these reuse is anchored separately in <see cref="Poly1305AeadCoreTests" />.
/// </summary>
[TestClass]
public class StreamAeadKnownAnswerTests
{
    private const string AlgorithmXChaCha20Poly1305 = "XChaCha20-Poly1305";
    private const string AlgorithmXSalsa20Poly1305Secretbox = "XSalsa20-Poly1305 secretbox";

    // RFC 7539 / RFC 8439 "Ladies and Gentlemen..." plaintext, shared by the XChaCha vector and the derived oracle.
    private const string SunscreenPlaintextHex =
        "4c616469657320616e642047656e746c656d656e206f662074686520636c617373206f66202739393a2049662049" +
        "20636f756c64206f6666657220796f75206f6e6c79206f6e652074697020666f7220746865206675747572652c20" +
        "73756e73637265656e20776f756c642062652069742e";

    private const string SunscreenKeyHex = "808182838485868788898a8b8c8d8e8f909192939495969798999a9b9c9d9e9f";
    private const string SunscreenNonce24Hex = "404142434445464748494a4b4c4d4e4f5051525354555657";
    private const string SunscreenAadHex = "50515253c0c1c2c3c4c5c6c7";

    // Frozen XSalsa20Poly1305Aead output for the sunscreen key/nonce/AAD/plaintext above, generated once from the
    // construction itself and cross-checked against the derived oracle. This is NOT an external published vector; it
    // pins the Bodu-defined hybrid so a regression cannot pass simply because production and oracle drift together.
    private const string FrozenAeadCiphertextHex =
        "60779c35e39f91b373dd13218ebe366e3c7f17829b1fc4006ee44681d1a961e9efd1d51c54cde408d4f66ea28bb6c303" +
        "5575cc7de3b4604426e0a7a03e96e6ea87e108f60ad2bc7123d67455056588c5a8bbd275e9981cfebebfeba673f2cd1f" +
        "f3f3805c28d5cc5382859152ca2e0b33e1cb";

    private const string FrozenAeadTagHex = "c46bc8bc3951e8a05d1c752917e39bf4";

    /// <summary>
    /// Enumerates the externally published known-answer vectors, each wrapped as a single
    /// <see cref="DynamicDataAttribute" /> row.
    /// </summary>
    /// <returns>One row per external vector.</returns>
    public static IEnumerable<object[]> ExternalVectors()
    {
        yield return new object[]
        {
            new AeadKnownAnswer
            {
                Name = "XChaCha20-Poly1305 (draft-irtf-cfrg-xchacha-03 Appendix A.3.1)",
                Algorithm = AlgorithmXChaCha20Poly1305,
                Provenance = new KatProvenance(
                    KatSourceKind.InternetDraft,
                    "draft-irtf-cfrg-xchacha-03 Appendix A.3.1, AEAD_XCHACHA20_POLY1305",
                    "Internet-Draft vector (not an RFC). Matches libsodium crypto_aead_xchacha20poly1305_ietf."),
                Key = Hex(SunscreenKeyHex),
                Nonce = Hex(SunscreenNonce24Hex),
                AssociatedData = Hex(SunscreenAadHex),
                Plaintext = Hex(SunscreenPlaintextHex),
                Ciphertext = Hex(
                    "bd6d179d3e83d43b9576579493c0e939572a1700252bfaccbed2902c21396cbb731c7f1b0b4aa6440bf3a82f4eda7" +
                    "e39ae64c6708c54c216cb96b72e1213b4522f8c9ba40db5d945b11b69b982c1bb9e3f3fac2bc369488f76b238356" +
                    "5d3fff921f9664c97637da9768812f615c68b13b52e"),
                Tag = Hex("c0875924c1c7987947deafd8780acf49"),
                Layout = AeadKatOutputLayout.DetachedTag,
            },
        };

        yield return new object[]
        {
            new AeadKnownAnswer
            {
                Name = "XSalsa20-Poly1305 secretbox (libsodium reference)",
                Algorithm = AlgorithmXSalsa20Poly1305Secretbox,
                Provenance = new KatProvenance(
                    KatSourceKind.ReferenceImplementation,
                    "libsodium test/default/secretbox.c and secretbox.exp",
                    "Reference-implementation vector. libsodium emits tag || ciphertext; Bodu emits ciphertext || tag."),
                Key = Hex("1b27556473e985d462cd51197a9a46c76009549eac6474f206c4ee0844f68389"),
                Nonce = Hex("69696ee955b62b73cd62bda875fc73d68219e0036b7a0b37"),
                AssociatedData = [],
                Plaintext = Hex(
                    "be075fc53c81f2d5cf141316ebeb0c7b5228c52a4c62cbd44b66849b64244ffce5ecbaaf33bd751a1ac728d45e6c" +
                    "61296cdc3c01233561f41db66cce314adb310e3be8250c46f06dceea3a7fa1348057e2f6556ad6b1318a024a838f" +
                    "21af1fde048977eb48f59ffd4924ca1c60902e52f0a089bc76897040e082f937763848645e0705"),
                Ciphertext = Hex(
                    "8e993b9f48681273c29650ba32fc76ce48332ea7164d96a4476fb8c531a1186ac0dfc17c98dce87b4da7f011ec48" +
                    "c97271d2c20f9b928fe2270d6fb863d51738b48eeee314a7cc8ab932164548e526ae90224368517acfeabd6bb37" +
                    "32bc0e9da99832b61ca01b6de56244a9e88d5f9b37973f622a43d14a6599b1f654cb45a74e355a5"),
                Tag = Hex("f3ffc7703f9400e52a7dfb4b3d3305d9"),
                Layout = AeadKatOutputLayout.TagThenCiphertext,
            },
        };
    }

    /// <summary>
    /// Produces a provenance-preserving display name for a known-answer row.
    /// </summary>
    /// <param name="methodInfo">The test method's reflection info.</param>
    /// <param name="data">The row data (a single <see cref="AeadKnownAnswer" />).</param>
    /// <returns>A label that identifies the vector and its provenance.</returns>
    public static string GetVectorDisplayName(MethodInfo methodInfo, object[] data) =>
        data is [AeadKnownAnswer vector, ..]
            ? $"{vector.Name} [{vector.Provenance?.Kind}]"
            : methodInfo.Name;

    /// <summary>
    /// Verifies that the construction named by the vector reproduces the expected ciphertext and tag.
    /// </summary>
    /// <param name="vector">The known-answer vector under test.</param>
    [TestMethod]
    [DynamicData(nameof(ExternalVectors), DynamicDataDisplayName = nameof(GetVectorDisplayName))]
    public void Encrypt_WhenGivenExternalVector_ShouldMatchExpectedCiphertextAndTag(AeadKnownAnswer vector)
    {
        byte[] plaintext = vector.Plaintext;
        byte[] output = new byte[plaintext.Length + 16];

        using (IStreamAeadTransform enc = CreateTransform(vector))
        {
            int written = enc.Encrypt(plaintext, output, vector.AssociatedData);
            Assert.AreEqual(output.Length, written, $"{vector.Name}: unexpected written length.");
        }

        CollectionAssert.AreEqual(vector.Ciphertext, output.AsSpan(0, plaintext.Length).ToArray(),
            $"{vector.Name}: ciphertext mismatch.");
        CollectionAssert.AreEqual(vector.Tag, output.AsSpan(plaintext.Length).ToArray(),
            $"{vector.Name}: tag mismatch.");
    }

    /// <summary>
    /// Verifies that the construction named by the vector recovers the expected plaintext from the reference ciphertext
    /// and tag.
    /// </summary>
    /// <param name="vector">The known-answer vector under test.</param>
    [TestMethod]
    [DynamicData(nameof(ExternalVectors), DynamicDataDisplayName = nameof(GetVectorDisplayName))]
    public void Decrypt_WhenGivenExternalVector_ShouldRecoverPlaintext(AeadKnownAnswer vector)
    {
        byte[] ciphertextWithTag = vector.CiphertextWithTag;
        byte[] output = new byte[ciphertextWithTag.Length - 16];

        using (IStreamAeadTransform dec = CreateTransform(vector))
        {
            int written = dec.Decrypt(ciphertextWithTag, output, vector.AssociatedData);
            Assert.AreEqual(output.Length, written, $"{vector.Name}: unexpected written length.");
        }

        CollectionAssert.AreEqual(vector.Plaintext, output, $"{vector.Name}: recovered plaintext mismatch.");
    }

    /// <summary>
    /// Verifies that flipping a tag bit of an external vector causes decryption to throw
    /// <see cref="CryptographicException" />.
    /// </summary>
    /// <param name="vector">The known-answer vector under test.</param>
    [TestMethod]
    [DynamicData(nameof(ExternalVectors), DynamicDataDisplayName = nameof(GetVectorDisplayName))]
    public void Decrypt_WhenExternalVectorTagTampered_ShouldThrowCryptographicException(AeadKnownAnswer vector)
    {
        byte[] ciphertextWithTag = vector.CiphertextWithTag;
        ciphertextWithTag[^1] ^= 0x01;
        byte[] output = new byte[ciphertextWithTag.Length - 16];

        using IStreamAeadTransform dec = CreateTransform(vector);
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = dec.Decrypt(ciphertextWithTag, output, vector.AssociatedData);
        });
    }

    /// <summary>
    /// Verifies that the libsodium secretbox reference output (<c>tag ‖ ciphertext</c>) is reproduced by encrypting with
    /// <see cref="XSalsa20Poly1305" /> and converting the Bodu <c>ciphertext ‖ tag</c> output via
    /// <see cref="XSalsa20Poly1305.ToLibsodiumCombined" />.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenSecretboxVectorConvertedToLibsodiumLayout_ShouldMatchTagThenCiphertext()
    {
        AeadKnownAnswer vector = SecretboxVector();

        byte[] boduCombined = new byte[vector.Plaintext.Length + 16];
        using (var enc = new XSalsa20Poly1305(vector.Key, vector.Nonce))
            enc.Encrypt(vector.Plaintext, boduCombined);

        byte[] libsodiumCombined = new byte[boduCombined.Length];
        XSalsa20Poly1305.ToLibsodiumCombined(boduCombined, libsodiumCombined);

        // libsodium native layout for this vector is tag || ciphertext.
        byte[] expected = new byte[vector.Tag.Length + vector.Ciphertext.Length];
        vector.Tag.CopyTo(expected, 0);
        vector.Ciphertext.CopyTo(expected, vector.Tag.Length);

        Assert.AreEqual(AeadKatOutputLayout.TagThenCiphertext, vector.Layout);
        CollectionAssert.AreEqual(expected, libsodiumCombined);
    }

    /// <summary>
    /// Verifies that <see cref="XSalsa20Poly1305Aead" /> matches a derived oracle that composes the independently-tested
    /// public <see cref="XSalsa20" /> keystream with the public <see cref="Poly1305" /> MAC under RFC 8439 framing, and
    /// that the construction round-trips. No external standard vector exists for this hybrid; this is a derived-oracle
    /// check, not an authoritative KAT.
    /// </summary>
    [TestMethod]
    public void XSalsa20Poly1305Aead_WhenComparedToDerivedOracle_ShouldMatchAndRoundTrip()
    {
        byte[] key = Convert.FromHexString(SunscreenKeyHex);
        byte[] nonce = Convert.FromHexString(SunscreenNonce24Hex);
        byte[] aad = Convert.FromHexString(SunscreenAadHex);
        byte[] plaintext = Convert.FromHexString(SunscreenPlaintextHex);

        (byte[]? expectedCiphertext, byte[]? expectedTag) = DeriveAeadOracle(key, nonce, aad, plaintext);

        byte[] output = new byte[plaintext.Length + 16];
        using (var enc = new XSalsa20Poly1305Aead(key, nonce))
            enc.Encrypt(plaintext, output, aad);

        CollectionAssert.AreEqual(expectedCiphertext, output.AsSpan(0, plaintext.Length).ToArray(),
            "XSalsa20Poly1305Aead ciphertext must match the derived oracle.");
        CollectionAssert.AreEqual(expectedTag, output.AsSpan(plaintext.Length).ToArray(),
            "XSalsa20Poly1305Aead tag must match the derived oracle.");

        byte[] recovered = new byte[plaintext.Length];
        using (var dec = new XSalsa20Poly1305Aead(key, nonce))
            dec.Decrypt(output, recovered, aad);

        CollectionAssert.AreEqual(plaintext, recovered, "XSalsa20Poly1305Aead must round-trip.");
    }

    /// <summary>
    /// Verifies that <see cref="XSalsa20Poly1305Aead" /> reproduces the frozen derived vector locked into this test, so
    /// the construction cannot silently change even if the derived oracle changed in lock-step. This is a frozen
    /// derived-oracle value, not an external KAT.
    /// </summary>
    [TestMethod]
    public void XSalsa20Poly1305Aead_WhenGivenFrozenDerivedVector_ShouldRemainStable()
    {
        byte[] key = Convert.FromHexString(SunscreenKeyHex);
        byte[] nonce = Convert.FromHexString(SunscreenNonce24Hex);
        byte[] aad = Convert.FromHexString(SunscreenAadHex);
        byte[] plaintext = Convert.FromHexString(SunscreenPlaintextHex);

        byte[] output = new byte[plaintext.Length + 16];
        using (var enc = new XSalsa20Poly1305Aead(key, nonce))
            enc.Encrypt(plaintext, output, aad);

        CollectionAssert.AreEqual(Convert.FromHexString(FrozenAeadCiphertextHex),
            output.AsSpan(0, plaintext.Length).ToArray(), "Frozen ciphertext drift.");
        CollectionAssert.AreEqual(Convert.FromHexString(FrozenAeadTagHex),
            output.AsSpan(plaintext.Length).ToArray(), "Frozen tag drift.");
    }

    /// <summary>
    /// Creates the AEAD transform that an external vector targets.
    /// </summary>
    /// <param name="vector">The vector whose <see cref="AeadKnownAnswer.Algorithm" /> selects the type.</param>
    /// <returns>A transform bound to the vector's key and nonce.</returns>
    /// <exception cref="NotSupportedException">The vector names an unrecognised algorithm.</exception>
    private static IStreamAeadTransform CreateTransform(AeadKnownAnswer vector) =>
        vector.Algorithm switch
        {
            AlgorithmXChaCha20Poly1305 => new XChaCha20Poly1305(vector.Key, vector.Nonce),
            AlgorithmXSalsa20Poly1305Secretbox => new XSalsa20Poly1305(vector.Key, vector.Nonce),
            _ => throw new NotSupportedException(vector.Algorithm),
        };

    /// <summary>
    /// Returns the libsodium secretbox reference vector from <see cref="ExternalVectors" />.
    /// </summary>
    /// <returns>The secretbox known-answer vector.</returns>
    private static AeadKnownAnswer SecretboxVector() =>
        (AeadKnownAnswer)ExternalVectors()
            .Select(row => row[0])
            .First(v => ((AeadKnownAnswer)v).Algorithm == AlgorithmXSalsa20Poly1305Secretbox);

    /// <summary>
    /// Computes the expected XSalsa20-Poly1305 (RFC 8439 framing) ciphertext and tag by composing the public
    /// <see cref="XSalsa20" /> keystream with the public <see cref="Poly1305" /> MAC — an implementation path
    /// independent of <c>Poly1305AeadCore</c>.
    /// </summary>
    /// <param name="key">The 32-byte key.</param>
    /// <param name="nonce">The 24-byte nonce.</param>
    /// <param name="aad">The associated data.</param>
    /// <param name="plaintext">The plaintext.</param>
    /// <returns>The expected ciphertext and 16-byte tag.</returns>
    private static (byte[] Ciphertext, byte[] Tag) DeriveAeadOracle(
        byte[] key,
        byte[] nonce,
        byte[] aad,
        byte[] plaintext)
    {
        // The counter-0 block (64 bytes) yields the Poly1305 key in its first 32 bytes; the message is encrypted with
        // the keystream from counter 1 onward (byte offset 64). Encrypting zeros recovers the raw keystream.
        byte[] keystream;
        using (var xsalsa20 = new XSalsa20())
        using (ICryptoTransform encryptor = xsalsa20.CreateEncryptor(key, nonce))
            keystream = encryptor.TransformFinalBlock(new byte[64 + plaintext.Length], 0, 64 + plaintext.Length);

        byte[] ciphertext = new byte[plaintext.Length];
        for (int i = 0; i < plaintext.Length; i++)
            ciphertext[i] = (byte)(plaintext[i] ^ keystream[64 + i]);

        byte[] poly1305Key = keystream[..32];
        byte[] tag = ComputeRfc8439Poly1305(poly1305Key, aad, ciphertext);

        return (ciphertext, tag);
    }

    /// <summary>
    /// Computes a Poly1305 tag over the RFC 8439 framing
    /// <c>AAD ‖ pad16(AAD) ‖ ciphertext ‖ pad16(ciphertext) ‖ le64(|AAD|) ‖ le64(|ciphertext|)</c> using the public
    /// <see cref="Poly1305" /> MAC.
    /// </summary>
    /// <param name="poly1305Key">The 32-byte one-time Poly1305 key.</param>
    /// <param name="aad">The associated data.</param>
    /// <param name="ciphertext">The ciphertext to authenticate.</param>
    /// <returns>The 16-byte authentication tag.</returns>
    private static byte[] ComputeRfc8439Poly1305(byte[] poly1305Key, byte[] aad, byte[] ciphertext)
    {
        static int Pad16(int length) => (16 - (length & 15)) & 15;

        using var macData = new MemoryStream();
        macData.Write(aad);
        macData.Write(new byte[Pad16(aad.Length)]);
        macData.Write(ciphertext);
        macData.Write(new byte[Pad16(ciphertext.Length)]);

        Span<byte> lengths = stackalloc byte[16];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(lengths, (ulong)aad.Length);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(lengths[8..], (ulong)ciphertext.Length);
        macData.Write(lengths);

        using var poly1305 = new Poly1305 { Key = poly1305Key };
        return poly1305.ComputeHash(macData.ToArray());
    }
}

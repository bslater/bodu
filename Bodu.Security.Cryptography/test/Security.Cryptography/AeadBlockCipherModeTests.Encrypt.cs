// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AeadBlockCipherModeTests.Encrypt.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class AeadBlockCipherModeTests<TTest, TTransform>
{
    // ── Argument validation ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="IAeadBlockCipherModeTransform.Encrypt" /> throws
    /// <see cref="ArgumentException" /> when the output buffer is too small to hold both
    /// the ciphertext and the authentication tag.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenOutputIsTooSmall_ShouldThrowArgumentException()
    {
        var transform = MakeTransform();
        var plaintext = new byte[ExpectedBlockSize];
        var tooSmall = new byte[1]; // needs at least plaintext.Length + TagSize

        Assert.ThrowsExactly<ArgumentException>(() =>
            transform.Encrypt(plaintext, tooSmall));
    }

    // ── Output length ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="IAeadBlockCipherModeTransform.Encrypt" /> returns exactly
    /// <c>plaintext.Length + TagSize</c> — the ciphertext length plus the appended tag.
    /// </summary>
    [TestMethod]
    public void Encrypt_OutputLengthShouldEqualPlaintextLengthPlusTagSize()
    {
        var transform = MakeTransform();
        var plaintext = new byte[ExpectedBlockSize * 2];
        var buf = new byte[plaintext.Length + transform.TagSize];

        int written = transform.Encrypt(plaintext, buf);

        Assert.AreEqual(plaintext.Length + transform.TagSize, written,
            "Encrypt must return plaintext.Length + TagSize bytes written.");
    }

    /// <summary>
    /// Verifies that <see cref="IAeadBlockCipherModeTransform.Encrypt" /> writes exactly
    /// <see cref="IAeadBlockCipherModeTransform.TagSize" /> bytes when the plaintext is empty,
    /// confirming that the output consists of the authentication tag only with no ciphertext
    /// bytes preceding it.
    /// </summary>
    [TestMethod]
    public void Encrypt_WithEmptyPlaintext_ShouldProduceTagOnly()
    {
        var transform = MakeTransform();
        var output = new byte[transform.TagSize];

        int written = transform.Encrypt(ReadOnlySpan<byte>.Empty, output);

        Assert.AreEqual(transform.TagSize, written,
            "Encrypting empty plaintext must write exactly TagSize bytes.");
    }

    // ── Output content properties ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the authentication tag is non-zero for a non-trivial plaintext, confirming
    /// that the tag computation actually produces output dependent on the message.
    /// </summary>
    [TestMethod]
    public void Encrypt_ShouldProduceNonZeroTag()
    {
        var transform = MakeTransform();
        var plaintext = new byte[ExpectedBlockSize];
        plaintext[0] = 0x42;
        var output = new byte[plaintext.Length + transform.TagSize];

        transform.Encrypt(plaintext, output);

        bool tagIsAllZero = true;
        for (int i = plaintext.Length; i < output.Length; i++)
        {
            if (output[i] != 0)
            {
                tagIsAllZero = false;
                break;
            }
        }

        Assert.IsFalse(tagIsAllZero,
            $"{typeof(TTransform).Name} authentication tag must not be all-zero for non-trivial input.");
    }

    // ── Input sensitivity ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that two encryptions of the same plaintext under the same key but different
    /// nonces produce different ciphertext, confirming that the nonce is incorporated into
    /// the keystream. Skipped for modes whose ciphertext does not depend on the supplied IV
    /// (e.g. SIV, where the IV is ignored).
    /// </summary>
    /// <remarks>
    /// Uses a real AES backing cipher rather than <see cref="MonitoringBlockCipher" /> because
    /// OCB surrounds the cipher call with pre- and post-XOR operations that cancel under a
    /// linear cipher (RFC 7253 §2.6: <c>C_i = ENCIPHER(K, P_i ⊕ Offset_i) ⊕ Offset_i</c>),
    /// collapsing OCB ciphertext to <c>P ⊕ mask</c> independent of the nonce. Real AES breaks
    /// this linearity and exercises the genuine nonce-dependence path. Other AEAD modes
    /// (GCM, CCM, EAX, GCM-SIV) use the cipher as a CTR/PRF keystream and would observe a
    /// nonce-dependent ciphertext under either backing cipher.
    /// </remarks>
    [TestMethod]
    public void Encrypt_WithDifferentNonces_ShouldProduceDifferentCiphertext()
    {
        if (!NonceAffectsCiphertext)
        {
            Assert.Inconclusive($"{typeof(TTransform).Name} ciphertext does not depend on the supplied IV.");
            return;
        }

        var plaintext = new byte[ExpectedBlockSize];

        var iv1 = CreateInitializationVector();
        iv1[0] = 0x01;
        var iv2 = CreateInitializationVector();
        iv2[0] = 0x02;

        using var cipher1 = new AesBlockCipherFixture(new byte[16]);
        using var cipher2 = new AesBlockCipherFixture(new byte[16]);

        var t1 = CreateTransform(cipher1, iv1);
        var out1 = new byte[plaintext.Length + t1.TagSize];
        t1.Encrypt(plaintext, out1);

        var t2 = CreateTransform(cipher2, iv2);
        var out2 = new byte[plaintext.Length + t2.TagSize];
        t2.Encrypt(plaintext, out2);

        CollectionAssert.AreNotEqual(out1[..plaintext.Length], out2[..plaintext.Length],
            $"{typeof(TTransform).Name} must produce different ciphertext for different nonces.");
    }

    /// <summary>
    /// Verifies that two encryptions of the same plaintext and nonce but different associated
    /// data produce different authentication tags, confirming that the AAD is bound into the
    /// tag computation.
    /// </summary>
    [TestMethod]
    public void Encrypt_WithDifferentAad_ShouldProduceDifferentTag()
    {
        var iv = CreateInitializationVector();
        var plaintext = new byte[ExpectedBlockSize];

        var t1 = MakeTransform((byte[])iv.Clone());
        t1.ProcessAssociatedData(new byte[] { 0x01 });
        var out1 = new byte[plaintext.Length + t1.TagSize];
        t1.Encrypt(plaintext, out1);

        var t2 = MakeTransform((byte[])iv.Clone());
        t2.ProcessAssociatedData(new byte[] { 0xFF });
        var out2 = new byte[plaintext.Length + t2.TagSize];
        t2.Encrypt(plaintext, out2);

        CollectionAssert.AreNotEqual(out1[plaintext.Length..], out2[plaintext.Length..],
            $"{typeof(TTransform).Name} must produce different tags for different AAD values.");
    }

    /// <summary>
    /// Verifies that two encryptions under identical key, nonce, and AAD but different
    /// plaintext produce different authentication tags, confirming that the plaintext is
    /// bound into the tag computation.
    /// </summary>
    [TestMethod]
    public void Encrypt_WithDifferentPlaintext_ShouldProduceDifferentTag()
    {
        var iv = CreateInitializationVector();

        var pt1 = new byte[ExpectedBlockSize];
        pt1[0] = 0xAA;
        var t1 = MakeTransform((byte[])iv.Clone());
        var out1 = new byte[pt1.Length + t1.TagSize];
        t1.Encrypt(pt1, out1);

        var pt2 = new byte[ExpectedBlockSize];
        pt2[0] = 0xBB;
        var t2 = MakeTransform((byte[])iv.Clone());
        var out2 = new byte[pt2.Length + t2.TagSize];
        t2.Encrypt(pt2, out2);

        CollectionAssert.AreNotEqual(out1[pt1.Length..], out2[pt2.Length..],
            $"{typeof(TTransform).Name} must produce different tags for different plaintexts.");
    }

    // ── Determinism ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="IAeadBlockCipherModeTransform.Encrypt" /> is deterministic:
    /// two independent transform instances constructed from the same key, nonce, and AAD
    /// must produce identical ciphertext and tag for the same plaintext.
    /// </summary>
    /// <remarks>
    /// AEAD modes do not introduce internal randomness — determinism is required for
    /// correctness so that the receiver can reproduce the sender's tag exactly during
    /// verification.
    /// </remarks>
    [TestMethod]
    public void Encrypt_WithIdenticalInputs_ShouldAlwaysProduceSameOutput()
    {
        var iv = CreateInitializationVector();
        var aad = new byte[] { 0x01, 0x02, 0x03 };
        var plaintext = new byte[ExpectedBlockSize * 2];
        for (int i = 0; i < plaintext.Length; i++) plaintext[i] = (byte)i;

        var t1 = MakeTransform((byte[])iv.Clone());
        t1.ProcessAssociatedData(aad);
        var out1 = new byte[plaintext.Length + t1.TagSize];
        t1.Encrypt(plaintext, out1);

        var t2 = MakeTransform((byte[])iv.Clone());
        t2.ProcessAssociatedData(aad);
        var out2 = new byte[plaintext.Length + t2.TagSize];
        t2.Encrypt(plaintext, out2);

        CollectionAssert.AreEqual(out1, out2,
            $"{typeof(TTransform).Name} is deterministic: identical key, nonce, AAD, and plaintext " +
            "must always produce identical ciphertext and tag.");
    }

    // ── AAD equivalence ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that explicitly passing an empty span to
    /// <see cref="IAeadBlockCipherModeTransform.ProcessAssociatedData" /> produces the same
    /// ciphertext and tag as never calling <c>ProcessAssociatedData</c> at all.
    /// </summary>
    /// <remarks>
    /// Each AEAD mode treats the absent-AAD and empty-AAD cases identically — the tag
    /// computation reduces to the same value whether the caller explicitly supplies an empty
    /// span or omits the call entirely.
    /// </remarks>
    [TestMethod]
    public void Encrypt_WithExplicitEmptyAad_ShouldProduceSameOutputAsNoAadCall()
    {
        var iv = CreateInitializationVector();
        var plaintext = new byte[ExpectedBlockSize * 2];

        var withEmpty = MakeTransform((byte[])iv.Clone());
        withEmpty.ProcessAssociatedData(ReadOnlySpan<byte>.Empty);
        var ct1 = new byte[plaintext.Length + withEmpty.TagSize];
        withEmpty.Encrypt(plaintext, ct1);

        var withNone = MakeTransform((byte[])iv.Clone());
        var ct2 = new byte[plaintext.Length + withNone.TagSize];
        withNone.Encrypt(plaintext, ct2);

        CollectionAssert.AreEqual(ct1, ct2,
            "ProcessAssociatedData with an empty span must be equivalent to omitting the call.");
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AeadBlockCipherModeTests.Encrypt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
    public void Encrypt_WhenOutputIsTooSmall_ShouldThrowExactly()
    {
        TTransform transform = MakeTransform();
        byte[] plaintext = new byte[ExpectedBlockSize];
        byte[] tooSmall = new byte[1]; // needs at least plaintext.Length + TagSize

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
        TTransform transform = MakeTransform();
        byte[] plaintext = new byte[ExpectedBlockSize * 2];
        int tagBytes = transform.TagSize / 8;
        byte[] buf = new byte[plaintext.Length + tagBytes];

        int written = transform.Encrypt(plaintext, buf);

        Assert.AreEqual(plaintext.Length + tagBytes, written,
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
        TTransform transform = MakeTransform();
        int tagBytes = transform.TagSize / 8;
        byte[] output = new byte[tagBytes];

        int written = transform.Encrypt([], output);

        Assert.AreEqual(tagBytes, written,
            "Encrypting empty plaintext must write exactly TagSize / 8 bytes.");
    }

    // ── Output content properties ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the authentication tag is non-zero for a non-trivial plaintext, confirming
    /// that the tag computation actually produces output dependent on the message.
    /// </summary>
    [TestMethod]
    public void Encrypt_ShouldProduceNonZeroTag()
    {
        TTransform transform = MakeTransform();
        byte[] plaintext = new byte[ExpectedBlockSize];
        plaintext[0] = 0x42;
        byte[] output = new byte[plaintext.Length + (transform.TagSize / 8)];

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
    [TestMethod]
    public void Encrypt_WithDifferentNonces_ShouldProduceDifferentCiphertext()
    {
        if (!NonceAffectsCiphertext)
        {
            Assert.Inconclusive($"{typeof(TTransform).Name} ciphertext does not depend on the supplied IV.");
            return;
        }

        byte[] plaintext = new byte[ExpectedBlockSize];

        byte[] iv1 = CreateInitializationVector();
        iv1[0] = 0x01;
        byte[] iv2 = CreateInitializationVector();
        iv2[0] = 0x02;

        TTransform t1 = MakeTransform(iv1);
        byte[] out1 = new byte[plaintext.Length + (t1.TagSize / 8)];
        t1.Encrypt(plaintext, out1);

        TTransform t2 = MakeTransform(iv2);
        byte[] out2 = new byte[plaintext.Length + (t2.TagSize / 8)];
        t2.Encrypt(plaintext, out2);

        CollectionAssert.AreNotEqual(out1, out2,
            $"{typeof(TTransform).Name} must produce different output for different nonces.");
    }

    /// <summary>
    /// Verifies that two encryptions of the same plaintext and nonce but different associated
    /// data produce different authentication tags, confirming that the AAD is bound into the
    /// tag computation.
    /// </summary>
    [TestMethod]
    public void Encrypt_WithDifferentAad_ShouldProduceDifferentTag()
    {
        byte[] iv = CreateInitializationVector();
        byte[] plaintext = new byte[ExpectedBlockSize];

        TTransform t1 = MakeTransform((byte[])iv.Clone());
        t1.ProcessAssociatedData([0x01]);
        byte[] out1 = new byte[plaintext.Length + (t1.TagSize / 8)];
        t1.Encrypt(plaintext, out1);

        TTransform t2 = MakeTransform((byte[])iv.Clone());
        t2.ProcessAssociatedData([0xFF]);
        byte[] out2 = new byte[plaintext.Length + (t2.TagSize / 8)];
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
        byte[] iv = CreateInitializationVector();

        byte[] pt1 = new byte[ExpectedBlockSize];
        pt1[0] = 0xAA;
        TTransform t1 = MakeTransform((byte[])iv.Clone());
        byte[] out1 = new byte[pt1.Length + (t1.TagSize / 8)];
        t1.Encrypt(pt1, out1);

        byte[] pt2 = new byte[ExpectedBlockSize];
        pt2[0] = 0xBB;
        TTransform t2 = MakeTransform((byte[])iv.Clone());
        byte[] out2 = new byte[pt2.Length + (t2.TagSize / 8)];
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
        byte[] iv = CreateInitializationVector();
        byte[] aad = new byte[] { 0x01, 0x02, 0x03 };
        byte[] plaintext = new byte[ExpectedBlockSize * 2];
        for (int i = 0; i < plaintext.Length; i++) plaintext[i] = (byte)i;

        TTransform t1 = MakeTransform((byte[])iv.Clone());
        t1.ProcessAssociatedData(aad);
        byte[] out1 = new byte[plaintext.Length + (t1.TagSize / 8)];
        t1.Encrypt(plaintext, out1);

        TTransform t2 = MakeTransform((byte[])iv.Clone());
        t2.ProcessAssociatedData(aad);
        byte[] out2 = new byte[plaintext.Length + (t2.TagSize / 8)];
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
        byte[] iv = CreateInitializationVector();
        byte[] plaintext = new byte[ExpectedBlockSize * 2];

        TTransform withEmpty = MakeTransform((byte[])iv.Clone());
        withEmpty.ProcessAssociatedData([]);
        byte[] ct1 = new byte[plaintext.Length + (withEmpty.TagSize / 8)];
        withEmpty.Encrypt(plaintext, ct1);

        TTransform withNone = MakeTransform((byte[])iv.Clone());
        byte[] ct2 = new byte[plaintext.Length + (withNone.TagSize / 8)];
        withNone.Encrypt(plaintext, ct2);

        CollectionAssert.AreEqual(ct1, ct2,
            "ProcessAssociatedData with an empty span must be equivalent to omitting the call.");
    }
}

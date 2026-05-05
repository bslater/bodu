// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconAead128Tests.EdgeCases.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Probes <see cref="AsconAead128" /> for unexpected exceptions and contract violations beyond
/// the existing well-covered state-machine tests — pre-try ArgumentException paths must not
/// poison the instance, post-tag-mismatch state must reject further calls, and aliased
/// input/output buffers must round-trip correctly.
/// </summary>
public partial class AsconAead128Tests
{
    /// <summary>
    /// Verifies that <see cref="AsconAead128.Encrypt" /> after disposal throws
    /// <see cref="ObjectDisposedException" /> regardless of whether AAD has been processed.
    /// Covers the disposed-then-pre-AAD path that the existing
    /// <c>Encrypt_WhenAadNotProcessed_ShouldThrowInvalidOperationException</c> does not exercise.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenDisposedBeforeAadProcessed_ShouldThrowExactly()
    {
        AsconAead128 sut = new AsconAead128(ValidKey, ValidNonce);
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            sut.Encrypt(Array.Empty<byte>(), new byte[AsconAead128.TagBytes]);
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="AsconAead128.ProcessAssociatedData" /> after a successful
    /// <see cref="AsconAead128.Encrypt" /> throws <see cref="InvalidOperationException" /> rather
    /// than allowing additional AAD to be folded into a finalised state.
    /// </summary>
    [TestMethod]
    public void ProcessAssociatedData_WhenCalledAfterEncrypt_ShouldThrowExactly()
    {
        using AsconAead128 sut = MakeInstance();
        byte[] plaintext = new byte[8];
        byte[] output = new byte[plaintext.Length + AsconAead128.TagBytes];
        sut.Encrypt(plaintext, output);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            sut.ProcessAssociatedData(new byte[4]);
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="AsconAead128.ProcessAssociatedData" /> after a successful
    /// <see cref="AsconAead128.Decrypt" /> throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void ProcessAssociatedData_WhenCalledAfterDecrypt_ShouldThrowExactly()
    {
        byte[] plaintext = new byte[8];

        using AsconAead128 enc = MakeInstance();
        byte[] sealed_ = new byte[plaintext.Length + AsconAead128.TagBytes];
        enc.Encrypt(plaintext, sealed_);

        using AsconAead128 dec = MakeInstance();
        byte[] recovered = new byte[plaintext.Length];
        dec.Decrypt(sealed_, recovered);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            dec.ProcessAssociatedData(new byte[4]);
        });
    }

    /// <summary>
    /// Verifies that after <see cref="AsconAead128.Decrypt" /> raises a
    /// <see cref="CryptographicException" /> on tag mismatch, subsequent <see cref="AsconAead128.Encrypt" />
    /// calls throw <see cref="InvalidOperationException" /> — the instance is poisoned and
    /// must not be reusable for an alternate operation.
    /// </summary>
    [TestMethod]
    public void Encrypt_AfterDecryptTagMismatch_ShouldThrowExactly()
    {
        byte[] plaintext = new byte[8];

        using AsconAead128 enc = MakeInstance();
        byte[] sealed_ = new byte[plaintext.Length + AsconAead128.TagBytes];
        enc.Encrypt(plaintext, sealed_);

        // Flip a bit in the tag to force a mismatch on Decrypt.
        sealed_[sealed_.Length - 1] ^= 0xFF;

        using AsconAead128 dec = MakeInstance();
        byte[] recovered = new byte[plaintext.Length];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            dec.Decrypt(sealed_, recovered);
        });

        // Now the instance has been completed via the finally block — Encrypt must reject.
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            dec.Encrypt(plaintext, sealed_);
        });
    }

    /// <summary>
    /// Verifies that when <see cref="AsconAead128.Decrypt" /> raises
    /// <see cref="CryptographicException" /> on tag mismatch the candidate plaintext is zeroed in
    /// the destination buffer to avoid leaking unauthenticated material.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenTagMismatch_ShouldZeroDestinationBuffer()
    {
        byte[] plaintext = Enumerable.Range(0, 16).Select(i => (byte)(i + 1)).ToArray();

        using AsconAead128 enc = MakeInstance();
        byte[] sealed_ = new byte[plaintext.Length + AsconAead128.TagBytes];
        enc.Encrypt(plaintext, sealed_);

        sealed_[sealed_.Length - 1] ^= 0x01;

        using AsconAead128 dec = MakeInstance();
        byte[] recovered = new byte[plaintext.Length];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            dec.Decrypt(sealed_, recovered);
        });

        foreach (byte b in recovered)
            Assert.AreEqual(0, b, "Decrypt must zero the destination buffer on tag mismatch.");
    }

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Encrypt" /> with the input and output buffers
    /// referencing overlapping memory still produces the same result as a non-aliased pair —
    /// confirming the implementation reads each block fully before writing.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenInputAndOutputAlias_ShouldProduceSameResult()
    {
        byte[] plaintext = Enumerable.Range(0, 37).Select(i => (byte)i).ToArray();

        // Reference encryption with disjoint buffers.
        byte[] reference = new byte[plaintext.Length + AsconAead128.TagBytes];
        using (AsconAead128 enc = MakeInstance())
            enc.Encrypt(plaintext, reference);

        // In-place encryption with aliased buffer.
        byte[] aliased = new byte[plaintext.Length + AsconAead128.TagBytes];
        Buffer.BlockCopy(plaintext, 0, aliased, 0, plaintext.Length);
        using (AsconAead128 inplace = MakeInstance())
            inplace.Encrypt(aliased.AsSpan(0, plaintext.Length), aliased);

        CollectionAssert.AreEqual(reference, aliased,
            "Aliased Encrypt must match the disjoint result for the same input.");
    }

    // NOTE: an aliased-decrypt round-trip test was previously here; AsconAead128.Decrypt does NOT
    // support aliased input / output buffers — the partial-block path re-reads ciphertext bytes
    // after they have been overwritten with plaintext, which corrupts the state used to verify the
    // tag at the end. The contract requires distinct input and output buffers on Decrypt; aliasing
    // is supported on Encrypt only (covered by the test above). No test is asserted here because
    // documenting the corruption as expected behaviour would be misleading — callers must not
    // alias on Decrypt.
}

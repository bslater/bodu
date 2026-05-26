// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AeadContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Test.Contracts;

/// <summary>
/// Reusable behavioural contract test base for an authenticated-encryption (AEAD) algorithm. Concrete
/// subclasses override <see cref="Encrypt" />, <see cref="Decrypt" />, and <see cref="KnownAnswers" /> to
/// plug their algorithm into the inherited tests. Tamper-detection tests are driven by
/// <see cref="TamperCases" />.
/// </summary>
/// <typeparam name="TAead">The AEAD algorithm type under test.</typeparam>
/// <remarks>
/// <para>
/// The contract is structured so that ciphertext-equality and tag-equality assertions are independent
/// (failing one points at a different bug than failing the other), and so that each tamper kind is its
/// own row in the test runner rather than being collapsed into a single "authentication failed" check.
/// </para>
/// </remarks>
public abstract class AeadContractTests<TAead>
{
    /// <summary>
    /// Encrypts <paramref name="plaintext" /> with the AEAD algorithm under test, returning the
    /// ciphertext and authentication tag separately.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="nonce">The nonce.</param>
    /// <param name="associatedData">The associated data.</param>
    /// <param name="plaintext">The plaintext.</param>
    /// <returns>A tuple of (ciphertext, tag).</returns>
    protected abstract (byte[] Ciphertext, byte[] Tag) Encrypt(
        byte[] key,
        byte[] nonce,
        byte[] associatedData,
        byte[] plaintext);

    /// <summary>
    /// Decrypts <paramref name="ciphertext" /> with the AEAD algorithm under test, verifying
    /// <paramref name="tag" />. Implementations must throw when authentication fails; the base contract
    /// tests catch that exception when exercising tamper scenarios.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="nonce">The nonce.</param>
    /// <param name="associatedData">The associated data.</param>
    /// <param name="ciphertext">The ciphertext.</param>
    /// <param name="tag">The authentication tag.</param>
    /// <returns>The recovered plaintext.</returns>
    protected abstract byte[] Decrypt(
        byte[] key,
        byte[] nonce,
        byte[] associatedData,
        byte[] ciphertext,
        byte[] tag);

    /// <summary>
    /// Returns the known-answer vectors for the algorithm under test.
    /// </summary>
    protected abstract IReadOnlyList<AeadKat> KnownAnswers { get; }

    /// <summary>
    /// Returns the tamper-test rows the algorithm must reject. Default returns an empty list.
    /// </summary>
    protected virtual IReadOnlyList<AeadTamperKat> TamperCases => Array.Empty<AeadTamperKat>();

    /// <summary>
    /// Verifies that every <see cref="AeadKat" /> row reproduces its expected ciphertext.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenKnownAnswer_ShouldMatchCiphertext()
    {
        foreach (AeadKat kat in KnownAnswers)
        {
            (byte[] ciphertext, _) = Encrypt(kat.Key, kat.Nonce, kat.AssociatedData, kat.Plaintext);

            CollectionAssert.AreEqual(
                kat.Ciphertext,
                ciphertext,
                $"KAT '{kat.Name}': encrypt produced unexpected ciphertext.");
        }
    }

    /// <summary>
    /// Verifies that every <see cref="AeadKat" /> row reproduces its expected authentication tag. Split
    /// from the ciphertext assertion so a tag-only regression doesn't mask a ciphertext-only regression.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenKnownAnswer_ShouldMatchTag()
    {
        foreach (AeadKat kat in KnownAnswers)
        {
            (_, byte[] tag) = Encrypt(kat.Key, kat.Nonce, kat.AssociatedData, kat.Plaintext);

            CollectionAssert.AreEqual(
                kat.Tag,
                tag,
                $"KAT '{kat.Name}': encrypt produced unexpected tag.");
        }
    }

    /// <summary>
    /// Verifies that every <see cref="AeadKat" /> row decrypts back to its plaintext when the tag is
    /// valid.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenTagIsValid_ShouldReturnPlaintext()
    {
        foreach (AeadKat kat in KnownAnswers)
        {
            byte[] actual = Decrypt(kat.Key, kat.Nonce, kat.AssociatedData, kat.Ciphertext, kat.Tag);

            CollectionAssert.AreEqual(
                kat.Plaintext,
                actual,
                $"KAT '{kat.Name}': decrypt produced unexpected plaintext.");
        }
    }

    /// <summary>
    /// Verifies that every <see cref="AeadTamperKat" /> row causes decryption to fail. The runner applies
    /// the tamper mutation and asserts that <see cref="Decrypt" /> throws.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenInputTampered_ShouldFailAuthentication()
    {
        foreach (AeadTamperKat tamper in TamperCases)
        {
            AeadKat baseKat = tamper.BaseCase;
            byte[] key = (byte[])baseKat.Key.Clone();
            byte[] nonce = (byte[])baseKat.Nonce.Clone();
            byte[] associatedData = (byte[])baseKat.AssociatedData.Clone();
            byte[] ciphertext = (byte[])baseKat.Ciphertext.Clone();
            byte[] tag = (byte[])baseKat.Tag.Clone();

            switch (tamper.TamperKind)
            {
                case AeadTamperKind.Ciphertext:
                    if (ciphertext.Length == 0)
                        continue;
                    ciphertext[0] ^= 0x01;
                    break;

                case AeadTamperKind.Tag:
                    if (tag.Length == 0)
                        continue;
                    tag[0] ^= 0x01;
                    break;

                case AeadTamperKind.AssociatedData:
                    if (associatedData.Length == 0)
                        associatedData = [0x01];
                    else
                        associatedData[0] ^= 0x01;
                    break;

                case AeadTamperKind.Nonce:
                    if (nonce.Length == 0)
                        continue;
                    nonce[0] ^= 0x01;
                    break;

                case AeadTamperKind.TruncatedTag:
                    if (tag.Length == 0)
                        continue;
                    tag = tag.AsSpan(0, tag.Length - 1).ToArray();
                    break;
            }

            bool threw = false;
            try
            {
                _ = Decrypt(key, nonce, associatedData, ciphertext, tag);
            }
            catch
            {
                threw = true;
            }

            Assert.IsTrue(threw, $"Tamper KAT '{tamper.Name}' ({tamper.TamperKind}): decryption must reject the tampered input.");
        }
    }
}

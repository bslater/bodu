// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography.Contracts;

/// <summary>
/// Reusable behavioural contract test base for a single-block symmetric cipher. Concrete subclasses
/// override <see cref="EncryptBlock" />, <see cref="DecryptBlock" />, and <see cref="KnownAnswers" /> to
/// plug their cipher into the inherited tests.
/// </summary>
/// <typeparam name="TCipher">The cipher type under test.</typeparam>
/// <remarks>
/// <para>
/// The base targets the raw block layer (no mode, no padding): for each <see cref="BlockCipherKat" /> the
/// runner encrypts the plaintext using the supplied key and asserts the ciphertext matches, then decrypts
/// the ciphertext and asserts the plaintext matches. Encrypt and decrypt assertions are split into
/// separate <c>[TestMethod]</c>s so a failure points at exactly one direction.
/// </para>
/// <para>
/// Carried for documentation only — the contract is exercised through delegate adapter methods rather than the concrete API.
/// </para>
/// </remarks>
public abstract class BlockCipherContractTests<TCipher>
{
    /// <summary>
    /// Encrypts a single block using <paramref name="key" /> and the row's optional tweak.
    /// </summary>
    /// <param name="key">The cipher key.</param>
    /// <param name="plaintext">The plaintext block to encrypt.</param>
    /// <param name="tweak">The optional tweak; <see langword="null" /> for non-tweakable ciphers.</param>
    /// <returns>The resulting ciphertext block.</returns>
    protected abstract byte[] EncryptBlock(byte[] key, byte[] plaintext, byte[]? tweak);

    /// <summary>
    /// Decrypts a single block using <paramref name="key" /> and the row's optional tweak.
    /// </summary>
    /// <param name="key">The cipher key.</param>
    /// <param name="ciphertext">The ciphertext block to decrypt.</param>
    /// <param name="tweak">The optional tweak; <see langword="null" /> for non-tweakable ciphers.</param>
    /// <returns>The resulting plaintext block.</returns>
    protected abstract byte[] DecryptBlock(byte[] key, byte[] ciphertext, byte[]? tweak);

    /// <summary>
    /// Returns the known-answer vectors for the cipher under test.
    /// </summary>
    /// <returns>The ordered list of vectors.</returns>
    protected abstract IReadOnlyList<BlockCipherKat> KnownAnswers { get; }

    /// <summary>
    /// Verifies that every <see cref="BlockCipherKat" /> row reproduces its expected ciphertext when the
    /// plaintext is encrypted under the row's key.
    /// </summary>
    [TestMethod]
    public void EncryptBlock_WhenKnownAnswer_ShouldMatchCiphertext()
    {
        foreach (BlockCipherKat kat in KnownAnswers)
        {
            var actual = EncryptBlock(kat.Key, kat.Plaintext, kat.Tweak);

            CollectionAssert.AreEqual(
                kat.Ciphertext,
                actual,
                $"KAT '{kat.Name}': encrypt produced unexpected ciphertext.");
        }
    }

    /// <summary>
    /// Verifies that every <see cref="BlockCipherKat" /> row reproduces its expected plaintext when the
    /// ciphertext is decrypted under the row's key.
    /// </summary>
    [TestMethod]
    public void DecryptBlock_WhenKnownAnswer_ShouldMatchPlaintext()
    {
        foreach (BlockCipherKat kat in KnownAnswers)
        {
            var actual = DecryptBlock(kat.Key, kat.Ciphertext, kat.Tweak);

            CollectionAssert.AreEqual(
                kat.Plaintext,
                actual,
                $"KAT '{kat.Name}': decrypt produced unexpected plaintext.");
        }
    }
}

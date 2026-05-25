// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherModeContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Test.Kat;

namespace Bodu.Test.Contracts;

/// <summary>
/// Reusable behavioural contract test base for a symmetric block cipher operating in a chosen mode of
/// operation with a chosen padding scheme. Concrete subclasses override <see cref="Encrypt" />,
/// <see cref="Decrypt" />, and <see cref="KnownAnswers" /> to plug their cipher / mode combination into
/// the inherited tests.
/// </summary>
/// <typeparam name="TCipher">The cipher type under test.</typeparam>
public abstract class BlockCipherModeContractTests<TCipher>
{
    /// <summary>
    /// Encrypts <paramref name="plaintext" /> under the cipher and mode under test, using the supplied
    /// key, IV, mode, and padding.
    /// </summary>
    /// <param name="key">The cipher key.</param>
    /// <param name="iv">The initialisation vector.</param>
    /// <param name="mode">The mode of operation.</param>
    /// <param name="padding">The padding scheme.</param>
    /// <param name="plaintext">The plaintext bytes.</param>
    /// <returns>The resulting ciphertext bytes.</returns>
    protected abstract byte[] Encrypt(byte[] key, byte[] iv, CipherMode mode, PaddingMode padding, byte[] plaintext);

    /// <summary>
    /// Decrypts <paramref name="ciphertext" /> under the cipher and mode under test, using the supplied
    /// key, IV, mode, and padding.
    /// </summary>
    /// <param name="key">The cipher key.</param>
    /// <param name="iv">The initialisation vector.</param>
    /// <param name="mode">The mode of operation.</param>
    /// <param name="padding">The padding scheme.</param>
    /// <param name="ciphertext">The ciphertext bytes.</param>
    /// <returns>The resulting plaintext bytes.</returns>
    protected abstract byte[] Decrypt(byte[] key, byte[] iv, CipherMode mode, PaddingMode padding, byte[] ciphertext);

    /// <summary>
    /// Returns the known-answer vectors for the cipher / mode / padding combination under test.
    /// </summary>
    protected abstract IReadOnlyList<BlockCipherModeKat> KnownAnswers { get; }

    /// <summary>
    /// Verifies that every <see cref="BlockCipherModeKat" /> row reproduces its expected ciphertext.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenKnownAnswer_ShouldMatchCiphertext()
    {
        foreach (BlockCipherModeKat kat in KnownAnswers)
        {
            byte[] actual = Encrypt(kat.Key, kat.IV, kat.Mode, kat.Padding, kat.Plaintext);

            CollectionAssert.AreEqual(
                kat.Ciphertext,
                actual,
                $"KAT '{kat.Name}': encrypt produced unexpected ciphertext.");
        }
    }

    /// <summary>
    /// Verifies that every <see cref="BlockCipherModeKat" /> row decrypts back to its plaintext.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenKnownAnswer_ShouldMatchPlaintext()
    {
        foreach (BlockCipherModeKat kat in KnownAnswers)
        {
            byte[] actual = Decrypt(kat.Key, kat.IV, kat.Mode, kat.Padding, kat.Ciphertext);

            CollectionAssert.AreEqual(
                kat.Plaintext,
                actual,
                $"KAT '{kat.Name}': decrypt produced unexpected plaintext.");
        }
    }
}

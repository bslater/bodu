// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AeadAscon.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;

namespace Bodu.Security.Cryptography.Samples.SymmetricAndAead.Scenarios;

/// <summary>
/// Demonstrates the Ascon-AEAD128 authenticated cipher (NIST SP 800-232): encrypt with a fixed key, nonce,
/// and associated data to produce ciphertext plus a tag, decrypt to recover the plaintext, and show that
/// flipping a single ciphertext byte makes authentication fail on decryption.
/// </summary>
public static class AeadAscon
{
    // Ascon-AEAD128 uses a 128-bit (16-byte) key and a 128-bit (16-byte) nonce.
    private static readonly byte[] Key = Hex.Fill(16, 0x60);
    private static readonly byte[] Nonce = Hex.Fill(16, 0x70);
    private static readonly byte[] AssociatedData = Encoding.ASCII.GetBytes("v1|message-header");
    private static readonly byte[] Plaintext = Encoding.ASCII.GetBytes("attack at dawn");

    /// <summary>
    /// Seals the plaintext, opens it back, then tampers with the sealed data to trigger tag verification failure.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- AEAD: Ascon-AEAD128 (fixed key + nonce + AD) ---");
        Console.WriteLine();

        // Encrypt: the output is ciphertext (same length as plaintext) followed by the 16-byte tag.
        var sealedData = new byte[Plaintext.Length + 16];
        using (var encryptor = new AsconAead128(Key, Nonce))
        {
            encryptor.ProcessAssociatedData(AssociatedData);
            encryptor.Encrypt(Plaintext, sealedData);
        }

        Console.WriteLine($"  plaintext        : \"{Encoding.ASCII.GetString(Plaintext)}\"");
        Console.WriteLine($"  ciphertext + tag : {Hex.ToHex(sealedData)}");
        Console.WriteLine();

        // Decrypt with the matching key, nonce, and AD recovers the plaintext and verifies the tag.
        var recovered = new byte[Plaintext.Length];
        using (var decryptor = new AsconAead128(Key, Nonce))
        {
            decryptor.ProcessAssociatedData(AssociatedData);
            decryptor.Decrypt(sealedData, recovered);
        }

        Console.WriteLine($"  decrypt (intact) : \"{Encoding.ASCII.GetString(recovered)}\"  (matches: {recovered.AsSpan().SequenceEqual(Plaintext)})");

        // Flip one ciphertext byte; the tag no longer matches, so decryption must reject the message.
        var tampered = (byte[])sealedData.Clone();
        tampered[0] ^= 0xff;

        var tamperRejected = false;
        try
        {
            using var decryptor = new AsconAead128(Key, Nonce);
            decryptor.ProcessAssociatedData(AssociatedData);
            decryptor.Decrypt(tampered, new byte[Plaintext.Length]);
        }
        catch (CryptographicException)
        {
            // Authentication failed: the sponge state did not reproduce the expected tag.
            tamperRejected = true;
        }

        Console.WriteLine($"  decrypt (1 byte flipped) : rejected = {tamperRejected}");

        Console.WriteLine();
    }
}

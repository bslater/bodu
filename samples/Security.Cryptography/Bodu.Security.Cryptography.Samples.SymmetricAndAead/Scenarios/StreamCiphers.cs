// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamCiphers.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Security.Cryptography.Extensions;

namespace Bodu.Security.Cryptography.Samples.SymmetricAndAead.Scenarios;

/// <summary>
/// Demonstrates additive stream ciphers — ChaCha20, XChaCha20, and Salsa20 — each encrypting a fixed
/// message under a fixed key and nonce, then decrypting it back. Because these ciphers are self-inverse,
/// the same operation recovers the plaintext, and each cipher declares its own nonce width.
/// </summary>
public static class StreamCiphers
{
    // Fixed key and nonces keep the output reproducible; a (key, nonce) pair must never encrypt two different
    // messages in practice, because both would be XORed with the identical keystream.
    private static readonly byte[] Key = Hex.Fill(32, 0x00);
    private static readonly byte[] Plaintext = Encoding.ASCII.GetBytes("stream cipher keystream XOR demo");

    /// <summary>
    /// Round-trips the message through each stream cipher and prints the ciphertext and round-trip flag.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Stream ciphers (fixed key + nonce)",
            what: "Encrypts and decrypts one buffer with ChaCha20, XChaCha20 and Salsa20 under a fixed key and nonce, printing each cipher's nonce width.",
            why: "A stream cipher generates a keystream and XORs it over the data, so encrypting and decrypting are the same operation and the output is exactly as long as the input. The nonce width is the detail to take away: the X-variants take 24 bytes, wide enough to pick at random and never worry about a repeat, while 8- and 12-byte nonces have to be managed by a counter - reusing one under the same key loses all confidentiality.",
            expect: "All three round-trip True with no length change. None of them authenticates on its own; pairing one with Poly1305 - the last block of the Further ciphers scenario - is what adds tamper detection.");

        // Nonce widths differ per cipher: ChaCha20 96-bit, XChaCha20 192-bit, Salsa20 64-bit.
        RoundTrip("ChaCha20", () => new ChaCha20(), nonceBytes: 12);
        RoundTrip("XChaCha20", () => new XChaCha20(), nonceBytes: 24);
        RoundTrip("Salsa20", () => new Salsa20(), nonceBytes: 8);

        Console.WriteLine();
    }

    /// <summary>
    /// Encrypts and decrypts the fixed message with the supplied stream cipher.
    /// </summary>
    /// <param name="label">A short human-readable label for the cipher.</param>
    /// <param name="factory">Creates a fresh instance of the cipher.</param>
    /// <param name="nonceBytes">The nonce width in bytes.</param>
    private static void RoundTrip(string label, Func<SymmetricStreamAlgorithm> factory, int nonceBytes)
    {
        var nonce = Hex.Fill(nonceBytes, 0x40);

        byte[] ciphertext;
        using (var encryptor = factory())
        {
            encryptor.Key = Key;
            encryptor.Nonce = nonce;
            ciphertext = encryptor.Encrypt(Plaintext);
        }

        byte[] recovered;
        using (var decryptor = factory())
        {
            decryptor.Key = Key;
            decryptor.Nonce = nonce;
            recovered = decryptor.Decrypt(ciphertext);
        }

        var roundTrips = recovered.AsSpan().SequenceEqual(Plaintext);
        Console.WriteLine($"  {label,-10} (nonce {nonceBytes,2}B) round-trips: {roundTrips}");
        Console.WriteLine($"      ct: {Hex.ToHex(ciphertext)}");
    }
}

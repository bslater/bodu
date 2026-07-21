// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCiphers.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Samples.SymmetricAndAead.Scenarios;

/// <summary>
/// Encrypts and decrypts a single block with a spread of the library's block ciphers — Threefish (256 /
/// 512 / 1024), Twofish, Camellia, Serpent, Skipjack, and Blowfish — in ECB mode with no padding, using a
/// fixed key so each ciphertext is reproducible, and confirms the round trip recovers the plaintext.
/// </summary>
public static class BlockCiphers
{
    /// <summary>
    /// Runs the single-block round trip for each cipher and prints the ciphertext with a round-trip flag.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Block ciphers: single-block ECB round trip ---");
        Console.WriteLine();

        // Each row is (label, factory, key length, block length) in bytes.
        RoundTrip("Threefish-256", () => new Threefish256(), keyBytes: 32, blockBytes: 32);
        RoundTrip("Threefish-512", () => new Threefish512(), keyBytes: 64, blockBytes: 64);
        RoundTrip("Threefish-1024", () => new Threefish1024(), keyBytes: 128, blockBytes: 128);
        RoundTrip("Twofish", () => new Twofish(), keyBytes: 16, blockBytes: 16);
        RoundTrip("Camellia", () => new Camellia(), keyBytes: 16, blockBytes: 16);
        RoundTrip("Serpent-128", () => new Serpent128(), keyBytes: 16, blockBytes: 16);
        RoundTrip("Skipjack", () => new Skipjack(), keyBytes: 10, blockBytes: 8);
        RoundTrip("Blowfish", () => new Blowfish(), keyBytes: 16, blockBytes: 8);

        Console.WriteLine();
    }

    /// <summary>
    /// Encrypts one block of fixed plaintext under a fixed key, decrypts it, and prints the result.
    /// </summary>
    /// <param name="label">A short human-readable label for the cipher.</param>
    /// <param name="factory">Creates a fresh instance of the cipher.</param>
    /// <param name="keyBytes">The key length in bytes.</param>
    /// <param name="blockBytes">The block length in bytes.</param>
    private static void RoundTrip(string label, Func<SymmetricAlgorithm> factory, int keyBytes, int blockBytes)
    {
        var key = Hex.Fill(keyBytes, 0x10);
        var plaintext = Hex.Fill(blockBytes, 0x00);

        // Threefish is a *tweakable* cipher: a fresh instance generates a random 128-bit tweak and a random
        // IV, both of which would break the cross-instance round trip and make the output non-deterministic.
        // Pin both to fixed values so the ciphertext reproduces and encrypt/decrypt agree.
        var tweak = Hex.Fill(16, 0x30);
        var iv = Hex.Fill(blockBytes, 0x20);

        byte[] ciphertext;
        byte[] recovered;

        // ECB with no padding operates on exactly one block, which keeps this a pure block-cipher demo.
        using (var encryptorAlg = factory())
        {
            encryptorAlg.Key = key;
            encryptorAlg.Mode = CipherMode.ECB;
            encryptorAlg.Padding = PaddingMode.None;
            if (encryptorAlg is TweakableSymmetricAlgorithm tweakable)
            {
                encryptorAlg.IV = iv;
                tweakable.Tweak = tweak;
            }

            using var encryptor = encryptorAlg.CreateEncryptor();
            ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        }

        using (var decryptorAlg = factory())
        {
            decryptorAlg.Key = key;
            decryptorAlg.Mode = CipherMode.ECB;
            decryptorAlg.Padding = PaddingMode.None;
            if (decryptorAlg is TweakableSymmetricAlgorithm tweakable)
            {
                decryptorAlg.IV = iv;
                tweakable.Tweak = tweak;
            }

            using var decryptor = decryptorAlg.CreateDecryptor();
            recovered = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        }

        var roundTrips = recovered.AsSpan().SequenceEqual(plaintext);
        Console.WriteLine($"  {label,-14} (key {keyBytes,3}B / block {blockBytes,3}B) round-trips: {roundTrips}");
        Console.WriteLine($"      ct: {Hex.ToHex(ciphertext)}");
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AeadModes.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;
using Bodu.Security.Cryptography.Extensions;

namespace Bodu.Security.Cryptography.Samples.SymmetricAndAead.Scenarios;

/// <summary>
/// Runs AES through three authenticated-encryption modes — GCM, EAX, and OCB — over a fixed key, nonce, and
/// associated data. Each mode seals the plaintext (ciphertext plus tag), opens it back, and rejects a
/// tampered ciphertext, demonstrating the shared <c>IAeadBlockCipherModeTransform</c> surface.
/// </summary>
public static class AeadModes
{
    private static readonly byte[] Key = Hex.Fill(16, 0x20);
    private static readonly byte[] AssociatedData = Encoding.ASCII.GetBytes("aead-mode-header");
    private static readonly byte[] Plaintext = Encoding.ASCII.GetBytes("authenticated encryption");

    /// <summary>
    /// Seals, opens, and tamper-tests the plaintext under each AEAD mode.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- AEAD modes over AES (fixed key + nonce + AD) ---");
        Console.WriteLine();

        // GCM mandates a 12-byte nonce; EAX and OCB accept the block-sized 16-byte nonce used here.
        RunMode("AES-GCM", Hex.Fill(12, 0x50), nonce => new GcmModeTransform(new AesBlockCipher(Key), nonce));
        RunMode("AES-EAX", Hex.Fill(16, 0x50), nonce => new EaxModeTransform(new AesBlockCipher(Key), nonce));
        RunMode("AES-OCB", Hex.Fill(16, 0x50), nonce => new OcbModeTransform(new AesBlockCipher(Key), nonce));

        Console.WriteLine();
    }

    /// <summary>
    /// Seals and opens the plaintext under one AEAD mode, then confirms a tampered ciphertext is rejected.
    /// </summary>
    /// <param name="label">A short human-readable label for the mode.</param>
    /// <param name="nonce">The fixed nonce for this mode.</param>
    /// <param name="factory">Creates a fresh transform for the mode (each is single-use per message).</param>
    private static void RunMode(string label, byte[] nonce, Func<byte[], IAeadBlockCipherModeTransform> factory)
    {
        // The byte[]-returning Encrypt / Decrypt live on AeadBlockCipherModeTransformExtensions; call them
        // by their declaring type so the compiler does not bind to the span-writing instance overloads.
        byte[] sealedData;
        using (var encryptor = factory(nonce))
            sealedData = AeadBlockCipherModeTransformExtensions.Encrypt(encryptor, Plaintext, AssociatedData);

        byte[] recovered;
        using (var decryptor = factory(nonce))
            recovered = AeadBlockCipherModeTransformExtensions.Decrypt(decryptor, sealedData, AssociatedData);

        // Flip one ciphertext byte; a valid AEAD mode must fail authentication rather than return plaintext.
        var tampered = (byte[])sealedData.Clone();
        tampered[0] ^= 0xff;

        var tamperRejected = false;
        try
        {
            using var decryptor = factory(nonce);
            AeadBlockCipherModeTransformExtensions.Decrypt(decryptor, tampered, AssociatedData);
        }
        catch (CryptographicException)
        {
            tamperRejected = true;
        }

        Console.WriteLine($"  {label} : round-trips={recovered.AsSpan().SequenceEqual(Plaintext)} tamper-rejected={tamperRejected}");
        Console.WriteLine($"      sealed: {Hex.ToHex(sealedData)}");
    }
}

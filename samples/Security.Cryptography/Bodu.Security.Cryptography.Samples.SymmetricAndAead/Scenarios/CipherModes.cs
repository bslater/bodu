// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CipherModes.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Security.Cryptography.Extensions;

namespace Bodu.Security.Cryptography.Samples.SymmetricAndAead.Scenarios;

/// <summary>
/// Chains a block cipher across a multi-block message under two modes: CBC with PKCS#7 padding (which grows
/// the ciphertext to the next block boundary) and CTR (a streaming mode that leaves the length unchanged).
/// Both use Twofish with a fixed key and IV and round-trip encrypt then decrypt.
/// </summary>
public static class CipherModes
{
    private static readonly byte[] Key = Hex.Fill(16, 0x10);
    private static readonly byte[] Iv = Hex.Fill(16, 0x20);

    /// <summary>
    /// Encrypts and decrypts a message under CBC/PKCS7 and CTR, printing the ciphertext lengths and round trips.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Cipher modes over Twofish (fixed key + IV) ---");
        Console.WriteLine();

        // CBC pads to the block boundary, so an arbitrary-length message is accepted.
        var cbcMessage = Encoding.ASCII.GetBytes("thirty-one byte message here!!!"); // 31 bytes
        RunCbc(cbcMessage);

        Console.WriteLine();

        // CTR turns the block cipher into a keystream generator; input length is preserved, so it is used
        // here with a block-aligned message and no padding.
        var ctrMessage = Encoding.ASCII.GetBytes("exactly thirty-two byte message!"); // 32 bytes
        RunCtr(ctrMessage);

        Console.WriteLine();
    }

    /// <summary>
    /// Round-trips <paramref name="message" /> under CBC mode with PKCS#7 padding.
    /// </summary>
    /// <param name="message">The plaintext to encrypt.</param>
    private static void RunCbc(byte[] message)
    {
        using var encryptor = new Twofish
        {
            Key = Key,
            IV = Iv,
            BlockMode = CipherModeKind.CBC,
            BlockPadding = PaddingModeKind.PKCS7,
        };
        var ciphertext = encryptor.Encrypt(message);

        using var decryptor = new Twofish
        {
            Key = Key,
            IV = Iv,
            BlockMode = CipherModeKind.CBC,
            BlockPadding = PaddingModeKind.PKCS7,
        };
        var recovered = decryptor.Decrypt(ciphertext);

        Console.WriteLine($"  CBC/PKCS7 : plaintext {message.Length}B -> ciphertext {ciphertext.Length}B (padded)");
        Console.WriteLine($"      ct: {Hex.ToHex(ciphertext)}");
        Console.WriteLine($"      round-trips: {recovered.AsSpan().SequenceEqual(message)}");
    }

    /// <summary>
    /// Round-trips <paramref name="message" /> under CTR mode with no padding.
    /// </summary>
    /// <param name="message">The block-aligned plaintext to encrypt.</param>
    private static void RunCtr(byte[] message)
    {
        using var encryptor = new Twofish
        {
            Key = Key,
            IV = Iv,
            BlockMode = CipherModeKind.CTR,
            BlockPadding = PaddingModeKind.None,
        };
        var ciphertext = encryptor.Encrypt(message);

        using var decryptor = new Twofish
        {
            Key = Key,
            IV = Iv,
            BlockMode = CipherModeKind.CTR,
            BlockPadding = PaddingModeKind.None,
        };
        var recovered = decryptor.Decrypt(ciphertext);

        Console.WriteLine($"  CTR       : plaintext {message.Length}B -> ciphertext {ciphertext.Length}B (no growth)");
        Console.WriteLine($"      ct: {Hex.ToHex(ciphertext)}");
        Console.WriteLine($"      round-trips: {recovered.AsSpan().SequenceEqual(message)}");
    }
}

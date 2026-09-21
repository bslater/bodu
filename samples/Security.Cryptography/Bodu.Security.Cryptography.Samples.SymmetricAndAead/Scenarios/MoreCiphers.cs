// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoreCiphers.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;

using Bodu.Security.Cryptography.Extensions;

namespace Bodu.Security.Cryptography.Samples.SymmetricAndAead.Scenarios;

/// <summary>
/// Demonstrates the ciphers the other scenarios do not reach: the wide-block tweakable Serpent variants, the
/// remaining stream ciphers, and the two extended-nonce Poly1305 AEAD constructions.
/// </summary>
public static class MoreCiphers
{
    /// <summary>The fixed message every cipher round-trips.</summary>
    private static readonly byte[] Plaintext = Encoding.ASCII.GetBytes("sixty-four bytes of plaintext for the wide-block cipher demo !!!!");

    /// <summary>
    /// Round-trips the Serpent variants, the remaining stream ciphers, and the Poly1305 AEAD constructions.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Further ciphers ---");

        RunWideSerpent();
        RunStreamCiphers();
        RunPoly1305Aead();

        Console.WriteLine();
    }

    /// <summary>
    /// Shows the wide-block tweakable Serpent variants and what the tweak changes.
    /// </summary>
    private static void RunWideSerpent()
    {
        Console.WriteLine("  Wide-block tweakable Serpent:");

        // Serpent128 (covered by the BlockCiphers scenario) is the familiar 128-bit-block cipher. The 256/512/1024
        // variants widen the *block* as well as the key, and they are tweakable: each takes a tweak alongside the key
        // and IV, so the same key can be domain-separated per use without a separate key schedule.
        foreach (var factory in new Func<TweakableSymmetricAlgorithm>[]
        {
            () => new Serpent256(),
            () => new Serpent512(),
            () => new Serpent1024(),
        })
        {
            using var algorithm = factory();
            var blockBytes = algorithm.BlockSize / 8;

            algorithm.Key = Fill(algorithm.KeySize / 8, 0x10);
            algorithm.IV = Fill(blockBytes, 0x20);
            algorithm.Tweak = Fill(algorithm.TweakSize / 8, 0x30);
            algorithm.Mode = CipherMode.CBC;
            algorithm.Padding = PaddingMode.PKCS7;

            var ciphertext = Transform(algorithm, Plaintext, encrypt: true);
            var recovered = Transform(algorithm, ciphertext, encrypt: false);

            Console.WriteLine(
                $"    {algorithm.GetType().Name,-11}: block {algorithm.BlockSize,4} bits, key {algorithm.KeySize} bits, " +
                $"tweak {algorithm.TweakSize} bits -> {ciphertext.Length}B ciphertext, round-trip {recovered.SequenceEqual(Plaintext)}");
        }

        // Changing only the tweak changes the ciphertext, which is the point: one key, many independent domains.
        using var first = new Serpent256();
        using var second = new Serpent256();
        foreach (var algorithm in new[] { first, second })
        {
            algorithm.Key = Fill(algorithm.KeySize / 8, 0x10);
            algorithm.IV = Fill(algorithm.BlockSize / 8, 0x20);
            algorithm.Mode = CipherMode.CBC;
            algorithm.Padding = PaddingMode.PKCS7;
        }

        first.Tweak = Fill(first.TweakSize / 8, 0x30);
        second.Tweak = Fill(second.TweakSize / 8, 0x31);

        Console.WriteLine(
            $"    same key and IV, tweak 0x30 vs 0x31 differ: " +
            $"{Hex.ToHex(Transform(first, Plaintext, true)) != Hex.ToHex(Transform(second, Plaintext, true))}");

        Console.WriteLine();
    }

    /// <summary>
    /// Shows the remaining stream ciphers, reading each one's nonce width from the instance.
    /// </summary>
    private static void RunStreamCiphers()
    {
        Console.WriteLine("  Remaining stream ciphers:");

        // Rabbit and HC-128 are eSTREAM portfolio ciphers; XSalsa20 is Salsa20 with an extended 192-bit nonce (the
        // same widening XChaCha20 applies to ChaCha20), which makes random nonces safe without a counter.
        // The nonce width differs per cipher, so it is read from the instance rather than assumed.
        foreach (var factory in new Func<SymmetricStreamAlgorithm>[]
        {
            () => new Rabbit(),
            () => new Hc128(),
            () => new XSalsa20(),
        })
        {
            byte[] ciphertext;
            int nonceBits, keyBits;

            using (var encryptor = factory())
            {
                keyBits = encryptor.KeySize;
                nonceBits = encryptor.NonceSize;
                encryptor.Key = Fill(keyBits / 8, 0x10);
                encryptor.Nonce = Fill(nonceBits / 8, 0x40);
                ciphertext = encryptor.Encrypt(Plaintext);
            }

            byte[] recovered;
            using (var decryptor = factory())
            {
                decryptor.Key = Fill(keyBits / 8, 0x10);
                decryptor.Nonce = Fill(nonceBits / 8, 0x40);
                recovered = decryptor.Decrypt(ciphertext);
            }

            using var named = factory();
            Console.WriteLine(
                $"    {named.GetType().Name,-9}: key {keyBits} bits, nonce {nonceBits,3} bits -> {ciphertext.Length}B " +
                $"(no expansion), round-trip {recovered.SequenceEqual(Plaintext)}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Shows the two extended-nonce Poly1305 AEAD constructions and the libsodium tag ordering.
    /// </summary>
    private static void RunPoly1305Aead()
    {
        Console.WriteLine("  Extended-nonce Poly1305 AEAD:");

        var key = Fill(32, 0x10);
        var associatedData = Encoding.ASCII.GetBytes("header");

        // Both pair an extended-nonce stream cipher with Poly1305. The 192-bit nonce is the point: it is wide enough
        // to choose at random for every message without tracking a counter, which is what makes these the
        // "just use random nonces" options.
        //
        // They are not interchangeable, though. XChaCha20-Poly1305 is the IETF-style AEAD and authenticates
        // associated data; XSalsa20-Poly1305 is NaCl/libsodium's secretbox, which has no associated-data input at
        // all, so it requires an empty span and rejects anything else rather than silently ignoring it.
        RunAead("XChaCha20Poly1305", nonce => new XChaCha20Poly1305(key, nonce), XChaCha20Poly1305.NonceSize, associatedData);
        RunAead("XSalsa20Poly1305 ", nonce => new XSalsa20Poly1305(key, nonce), XSalsa20Poly1305.NonceSize, []);

        // The rejection is explicit, which is the right behaviour: a construction that quietly dropped the associated
        // data would leave the caller believing a header was authenticated when it was not.
        using (var secretbox = new XSalsa20Poly1305(key, Fill(XSalsa20Poly1305.NonceSize / 8, 0x40)))
        {
            try
            {
                _ = AeadTransformExtensions.Encrypt(secretbox, Plaintext, associatedData);
                Console.WriteLine("    secretbox + AAD  : accepted (unexpected)");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"    secretbox + AAD  : rejected ({ex.GetType().Name}) - secretbox has no AAD input");
            }
        }

        // This library appends the tag after the ciphertext; libsodium's combined format puts it first. XSalsa20Poly1305
        // ships the converters, because getting this wrong is a silent interoperability failure rather than an error.
        var nonce192 = Fill(XSalsa20Poly1305.NonceSize / 8, 0x40);
        using var sealer = new XSalsa20Poly1305(key, nonce192);
        var sealedBytes = AeadTransformExtensions.Encrypt(sealer, Plaintext, []);   // secretbox: no associated data

        var libsodiumOrder = new byte[sealedBytes.Length];
        XSalsa20Poly1305.ToLibsodiumCombined(sealedBytes, libsodiumOrder);

        var backAgain = new byte[sealedBytes.Length];
        XSalsa20Poly1305.FromLibsodiumCombined(libsodiumOrder, backAgain);

        Console.WriteLine($"    libsodium order : tag moves to the front: {Hex.ToHex(libsodiumOrder) != Hex.ToHex(sealedBytes)}");
        Console.WriteLine($"    converts back   : {Hex.ToHex(backAgain) == Hex.ToHex(sealedBytes)}");
    }

    /// <summary>
    /// Seals and opens the fixed message under one AEAD construction, then rejects a tampered copy.
    /// </summary>
    /// <param name="label">The construction's display name.</param>
    /// <param name="factory">Creates a transform for a nonce.</param>
    /// <param name="nonceBits">The construction's nonce width, in bits.</param>
    /// <param name="associatedData">The data to authenticate but not encrypt.</param>
    private static void RunAead(string label, Func<byte[], IAeadTransform> factory, int nonceBits, byte[] associatedData)
    {
        var nonce = Fill(nonceBits / 8, 0x40);

        using var encryptor = factory(nonce);
        var sealedBytes = AeadTransformExtensions.Encrypt(encryptor, Plaintext, associatedData);

        using var decryptor = factory(nonce);
        var opened = AeadTransformExtensions.Decrypt(decryptor, sealedBytes, associatedData);

        var tampered = sealedBytes.ToArray();
        tampered[^1] ^= 0x01;

        string tamperResult;
        using (var rejecting = factory(nonce))
        {
            try
            {
                _ = AeadTransformExtensions.Decrypt(rejecting, tampered, associatedData);
                tamperResult = "OPENED (unexpected)";
            }
            catch (Exception ex)
            {
                tamperResult = $"rejected ({ex.GetType().Name})";
            }
        }

        Console.WriteLine(
            $"    {label}: nonce {nonceBits} bits, {Plaintext.Length}B -> {sealedBytes.Length}B (+{sealedBytes.Length - Plaintext.Length} tag), " +
            $"round-trip {opened.SequenceEqual(Plaintext)}, tampered {tamperResult}");
    }

    /// <summary>
    /// Runs a configured algorithm over the whole input in one shot.
    /// </summary>
    /// <param name="algorithm">The configured algorithm.</param>
    /// <param name="input">The data to transform.</param>
    /// <param name="encrypt"><see langword="true" /> to encrypt; otherwise decrypt.</param>
    /// <returns>The transformed bytes.</returns>
    private static byte[] Transform(TweakableSymmetricAlgorithm algorithm, byte[] input, bool encrypt)
    {
        using var transform = encrypt
            ? algorithm.CreateEncryptor(algorithm.Key, algorithm.IV, algorithm.Tweak)
            : algorithm.CreateDecryptor(algorithm.Key, algorithm.IV, algorithm.Tweak);

        return transform.TransformFinalBlock(input, 0, input.Length);
    }

    /// <summary>
    /// Returns an array of <paramref name="length" /> bytes all set to <paramref name="value" />.
    /// </summary>
    /// <param name="length">The array length.</param>
    /// <param name="value">The byte to repeat.</param>
    /// <returns>The filled array.</returns>
    private static byte[] Fill(int length, byte value) =>
        Enumerable.Repeat(value, length).ToArray();
}

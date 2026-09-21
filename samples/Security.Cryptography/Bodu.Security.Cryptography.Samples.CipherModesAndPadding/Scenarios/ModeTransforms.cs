// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ModeTransforms.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Samples.CipherModesAndPadding.Scenarios;

/// <summary>
/// Demonstrates driving <see cref="IBlockCipherModeTransform" /> directly over an <see cref="IBlockCipher" />, the
/// layer beneath <c>SymmetricAlgorithm.Mode</c>: composing a padding strategy with a chaining mode, and the difference
/// between the block-oriented and keystream-like modes.
/// </summary>
public static class ModeTransforms
{
    /// <summary>The fixed 128-bit key. Fixed only so the sample is reproducible.</summary>
    private static readonly byte[] Key = Hex.Fill(16, 0x10);

    /// <summary>The fixed IV / initial counter. In real use this must be fresh and never repeat under one key.</summary>
    private static readonly byte[] Iv = Hex.Fill(16, 0x20);

    /// <summary>
    /// Round-trips the same message under each confidentiality mode and reports the ciphertext.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Mode transforms over a block cipher ---");

        // TwofishBlockCipher is a public IBlockCipher with a plain constructor, which is what makes the mode
        // infrastructure usable directly. Most callers should configure a mode through SymmetricAlgorithm.Mode
        // instead; this layer is for wiring a custom cipher in, or building a higher-level construction on top.
        using var cipher = new TwofishBlockCipher(Key);

        // IBlockCipher.BlockSize is in *bits*, matching SymmetricAlgorithm.BlockSize, so every byte-array operation
        // converts at the call site as BlockSize / 8. Treating it as a byte count is a silent off-by-eight.
        var blockBytes = cipher.BlockSize / 8;
        Console.WriteLine($"  cipher        : Twofish, block size {cipher.BlockSize} bits = {blockBytes} bytes, 128-bit key");

        // Deliberately block-aligned (32 bytes = two blocks) so every mode can be compared on identical input
        // without padding differences confusing the picture. The two halves are *identical* - which ECB will betray.
        var plaintext = Hex.Fill(16, 0x41).Concat(Hex.Fill(16, 0x41)).ToArray();
        Console.WriteLine($"  plaintext     : {Hex.ToBlocks(plaintext, blockBytes)} (two identical blocks)");
        Console.WriteLine();

        foreach (var (name, note) in new[]
        {
            (CipherModeKind.ECB, "each block encrypted independently"),
            (CipherModeKind.CBC, "each block XORed with the previous ciphertext"),
            (CipherModeKind.CFB, "ciphertext fed back into the cipher"),
            (CipherModeKind.OFB, "keystream generated independently of the data"),
            (CipherModeKind.CTR, "keystream from an incrementing counter"),
        })
        {
            // BlockCipherModeFactory maps the mode kind onto a transform, validating the IV length for the modes that
            // need one. Transforms are stateful - the evolving IV, feedback register or counter lives inside - so a
            // fresh one is constructed for each direction rather than reused.
            byte[] ciphertext;
            using (var encryptor = BlockCipherModeFactory.Create(name, cipher, (byte[])Iv.Clone()))
            {
                ciphertext = new byte[plaintext.Length];
                encryptor.Transform(plaintext, ciphertext, encrypt: true);
            }

            byte[] recovered;
            using (var decryptor = BlockCipherModeFactory.Create(name, cipher, (byte[])Iv.Clone()))
            {
                recovered = new byte[ciphertext.Length];
                decryptor.Transform(ciphertext, recovered, encrypt: false);
            }

            var repeats = Hex.ToHex(ciphertext.AsSpan(0, blockBytes)) == Hex.ToHex(ciphertext.AsSpan(blockBytes));
            Console.WriteLine($"  {name} - {note}");
            Console.WriteLine($"    ciphertext  : {Hex.ToBlocks(ciphertext, blockBytes)}");
            Console.WriteLine($"    round-trip  : {recovered.SequenceEqual(plaintext)}, identical blocks leak: {repeats}");
        }

        // ECB is the only mode above whose two ciphertext blocks match, because it has no chaining at all: identical
        // plaintext blocks always encrypt to identical ciphertext blocks. That is why ECB is unsuitable for anything
        // but single-block primitives - the structure of the plaintext survives encryption.
        Console.WriteLine();
        Console.WriteLine("  ^ only ECB leaks the repetition; that is what the chaining in the other modes buys.");

        // Now the composition the interface documents: padding is the caller's job. A misaligned message must be
        // padded before Transform, and unpadded after - the transform itself will reject anything not a whole
        // multiple of the block size.
        Console.WriteLine();
        Console.WriteLine("  Composing padding with a mode (the caller's job):");

        var message = Hex.Fill(21, 0x5A);   // 21 bytes - not a block multiple
        var padding = new Pkcs7Padding();
        var padded = padding.Pad(message, cipher.BlockSize);   // bits, as IPaddingStrategy expects

        using var cbcEncrypt = new CbcModeTransform(cipher, (byte[])Iv.Clone());
        var sealedBytes = new byte[padded.Length];
        cbcEncrypt.Transform(padded, sealedBytes, encrypt: true);

        using var cbcDecrypt = new CbcModeTransform(cipher, (byte[])Iv.Clone());
        var openedPadded = new byte[sealedBytes.Length];
        cbcDecrypt.Transform(sealedBytes, openedPadded, encrypt: false);
        var opened = padding.Unpad(openedPadded, cipher.BlockSize);

        Console.WriteLine($"    {message.Length}B message -> {padded.Length}B padded -> {sealedBytes.Length}B ciphertext -> {opened.Length}B recovered");
        Console.WriteLine($"    round-trip  : {opened.SequenceEqual(message)}");

        // And the transform's own contract: misaligned input is refused rather than partially processed.
        using var strict = new CbcModeTransform(cipher, (byte[])Iv.Clone());
        Console.WriteLine($"    unpadded 21B: {Throws(() => strict.Transform(message, new byte[32], encrypt: true))}");

        // CTR needs no padding at all, because it is a keystream: the ciphertext is exactly as long as the plaintext,
        // which is why the streaming modes are the usual choice for arbitrary-length data.
        using var ctr = new CtrModeTransform(cipher, (byte[])Iv.Clone());
        var ctrOut = new byte[message.Length];
        ctr.Transform(message, ctrOut, encrypt: true);
        Console.WriteLine($"    CTR on 21B  : {ctrOut.Length}B ciphertext - no padding, no length expansion");

        Console.WriteLine();
    }

    /// <summary>
    /// Invokes an operation expected to fail and names the exception type it raised.
    /// </summary>
    /// <param name="action">The operation to invoke.</param>
    /// <returns>The exception's type name, or a marker when the operation unexpectedly succeeded.</returns>
    private static string Throws(Action action)
    {
        try
        {
            action();
            return "(did not throw)";
        }
        catch (Exception ex)
        {
            return $"rejected ({ex.GetType().Name})";
        }
    }
}

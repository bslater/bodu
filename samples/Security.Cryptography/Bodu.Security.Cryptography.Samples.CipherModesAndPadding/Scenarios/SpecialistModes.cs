// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SpecialistModes.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Extensions;

namespace Bodu.Security.Cryptography.Samples.CipherModesAndPadding.Scenarios;

/// <summary>
/// Demonstrates the modes that exist for a specific job rather than as general-purpose confidentiality:
/// <see cref="CtsModeTransform" /> (arbitrary length without padding), <see cref="XtsModeTransform" /> (disk sectors),
/// and the nonce-misuse-resistant authenticated modes <see cref="SivModeTransform" /> and
/// <see cref="GcmSivModeTransform" /> alongside <see cref="CcmModeTransform" />.
/// </summary>
public static class SpecialistModes
{
    /// <summary>The fixed primary key.</summary>
    private static readonly byte[] Key = Hex.Fill(16, 0x10);

    /// <summary>A second, independent key for the modes that require two.</summary>
    private static readonly byte[] SecondKey = Hex.Fill(16, 0x30);

    /// <summary>The fixed IV / tweak / nonce source.</summary>
    private static readonly byte[] Iv = Hex.Fill(16, 0x20);

    /// <summary>
    /// Runs ciphertext stealing, XTS, and the three authenticated modes.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Specialist modes ---");

        RunCiphertextStealing();
        RunXts();
        RunAuthenticated();

        Console.WriteLine();
    }

    /// <summary>
    /// Shows CTS transforming an arbitrary-length message without padding or length expansion.
    /// </summary>
    private static void RunCiphertextStealing()
    {
        Console.WriteLine("  CTS (ciphertext stealing):");

        using var cipher = new TwofishBlockCipher(Key);

        // CTS solves the same problem padding does - an input that is not a block multiple - but without expanding the
        // ciphertext: it borrows bytes from the second-to-last block to fill the final short one. That makes it the
        // choice when the ciphertext must be exactly as long as the plaintext and a keystream mode is unsuitable.
        // It needs at least one full block, so it cannot handle very short inputs.
        foreach (var length in new[] { 16, 21, 31, 32, 45 })
        {
            var plaintext = Hex.Fill(length, 0x61);

            using var encryptor = new CtsModeTransform(cipher, (byte[])Iv.Clone());
            var ciphertext = new byte[length];
            encryptor.Transform(plaintext, ciphertext, encrypt: true);

            using var decryptor = new CtsModeTransform(cipher, (byte[])Iv.Clone());
            var recovered = new byte[length];
            decryptor.Transform(ciphertext, recovered, encrypt: false);

            Console.WriteLine($"    {length,2}B -> {ciphertext.Length,2}B ciphertext (no expansion), round-trip {recovered.SequenceEqual(plaintext)}");
        }

        // Below one block there is nothing to steal from, so CTS refuses rather than degrading to ECB.
        using var tooShort = new CtsModeTransform(cipher, (byte[])Iv.Clone());
        Console.WriteLine($"    8B (under one block): {Throws(() => tooShort.Transform(Hex.Fill(8, 0x61), new byte[8], encrypt: true))}");

        Console.WriteLine();
    }

    /// <summary>
    /// Shows XTS encrypting two sectors under one key pair with different tweaks.
    /// </summary>
    private static void RunXts()
    {
        Console.WriteLine("  XTS (disk-sector encryption):");

        // XTS takes two independently keyed ciphers: one encrypts the data, one encrypts the tweak. The tweak
        // identifies *where* the data lives - a sector number - so identical plaintext at two different offsets
        // encrypts differently without any per-sector IV having to be stored.
        using var dataCipher = new TwofishBlockCipher(Key);
        using var tweakCipher = new TwofishBlockCipher(SecondKey);

        var sector = Hex.Fill(32, 0x77);

        var first = EncryptSector(dataCipher, tweakCipher, TweakFor(0), sector);
        var second = EncryptSector(dataCipher, tweakCipher, TweakFor(1), sector);

        Console.WriteLine($"    identical 32B plaintext at sector 0 and 1:");
        Console.WriteLine($"      sector 0    : {Hex.ToBlocks(first, 16)}");
        Console.WriteLine($"      sector 1    : {Hex.ToBlocks(second, 16)}");
        Console.WriteLine($"      differ      : {Hex.ToHex(first) != Hex.ToHex(second)} (the tweak is the sector number)");

        using var decryptor = new XtsModeTransform(dataCipher, tweakCipher, TweakFor(0));
        var recovered = new byte[first.Length];
        decryptor.Transform(first, recovered, encrypt: false);
        Console.WriteLine($"      round-trip  : {recovered.SequenceEqual(sector)}");

        Console.WriteLine();
    }

    /// <summary>
    /// Shows the three authenticated modes and the misuse-resistance property two of them have.
    /// </summary>
    private static void RunAuthenticated()
    {
        Console.WriteLine("  Authenticated modes:");

        var plaintext = Hex.Fill(24, 0x11);
        var associatedData = Hex.Fill(8, 0x99);

        // CCM: counter mode with CBC-MAC, a NIST mode widely used in constrained protocols. Like GCM, it is *not*
        // misuse resistant - reusing a nonce under one key is catastrophic.
        RunAead("CCM    ", () => new CcmModeTransform(new AesBlockCipher(Key), (byte[])Iv.Clone()), plaintext, associatedData);

        // SIV (RFC 5297): deterministic authenticated encryption. It requires two independently keyed ciphers, and it
        // inverts the usual AEAD order - the MAC runs first and its output becomes both the tag and the CTR counter.
        RunAead("SIV    ", () => new SivModeTransform(new AesBlockCipher(Key), new AesBlockCipher(SecondKey), (byte[])Iv.Clone()), plaintext, associatedData);

        // GCM-SIV (RFC 8452): the same misuse-resistant ordering with POLYVAL instead of GHASH, deriving per-message
        // keys from a master key and nonce.
        RunAead("GCM-SIV", () => new GcmSivModeTransform(new AesBlockCipher(Key), key => new AesBlockCipher(key), (byte[])Iv.Clone()), plaintext, associatedData);

        // Now the property that actually separates these modes, made concrete without assuming any wire layout.
        //
        // CCM builds its keystream from the nonce alone, so under a repeated nonce the keystream repeats: changing one
        // plaintext byte changes that one ciphertext byte and nothing else. An observer who sees two messages sealed
        // under the same (key, nonce) can XOR them and recover the XOR of the plaintexts.
        //
        // SIV and GCM-SIV derive their counter from a MAC over the plaintext itself, so any change to the plaintext
        // changes the whole keystream. That is what "nonce-misuse resistant" buys: repeating a nonce leaks only
        // whether two messages were identical, never a relationship between different ones.
        Console.WriteLine("    One plaintext byte changed, same key and nonce - how much of the output moves?");

        var original = Hex.Fill(32, 0x11);
        var nudged = original.ToArray();
        nudged[0] ^= 0x01;

        foreach (var (label, factory) in new (string, Func<IAeadBlockCipherModeTransform>)[]
        {
            ("CCM    ", () => new CcmModeTransform(new AesBlockCipher(Key), (byte[])Iv.Clone())),
            ("SIV    ", () => new SivModeTransform(new AesBlockCipher(Key), new AesBlockCipher(SecondKey), (byte[])Iv.Clone())),
            ("GCM-SIV", () => new GcmSivModeTransform(new AesBlockCipher(Key), key => new AesBlockCipher(key), (byte[])Iv.Clone())),
        })
        {
            byte[] first, second;
            using (var t = factory()) first = AeadBlockCipherModeTransformExtensions.Encrypt(t, original, associatedData);
            using (var t = factory()) second = AeadBlockCipherModeTransformExtensions.Encrypt(t, nudged, associatedData);

            var differing = first.Zip(second, (a, b) => a != b).Count(d => d);
            var verdict = differing <= 1 + (first.Length - original.Length)
                ? "keystream reused - the change is localised"
                : "keystream re-derived - everything moved";

            Console.WriteLine($"      {label}: {differing,2} of {first.Length} bytes differ  ({verdict})");
        }

        // Sealing the *same* plaintext twice is deterministic in all three, which is expected and is not the
        // distinguishing property - the guarantee to rely on is the one measured above.
        Console.WriteLine("    Identical plaintext sealed twice is deterministic in all three (by construction):");
        foreach (var (label, factory) in new (string, Func<IAeadBlockCipherModeTransform>)[]
        {
            ("CCM    ", () => new CcmModeTransform(new AesBlockCipher(Key), (byte[])Iv.Clone())),
            ("SIV    ", () => new SivModeTransform(new AesBlockCipher(Key), new AesBlockCipher(SecondKey), (byte[])Iv.Clone())),
            ("GCM-SIV", () => new GcmSivModeTransform(new AesBlockCipher(Key), key => new AesBlockCipher(key), (byte[])Iv.Clone())),
        })
        {
            byte[] once, twice;
            using (var t = factory()) once = AeadBlockCipherModeTransformExtensions.Encrypt(t, plaintext, associatedData);
            using (var t = factory()) twice = AeadBlockCipherModeTransformExtensions.Encrypt(t, plaintext, associatedData);

            Console.WriteLine($"      {label}: same output: {Hex.ToHex(once) == Hex.ToHex(twice)}");
        }
    }

    /// <summary>
    /// Seals and opens a message under one AEAD mode, then shows tamper rejection and the detached-tag surface.
    /// </summary>
    /// <param name="label">The mode's display name.</param>
    /// <param name="factory">Creates a fresh transform; AEAD transforms are single-use.</param>
    /// <param name="plaintext">The message to seal.</param>
    /// <param name="associatedData">The data to authenticate but not encrypt.</param>
    private static void RunAead(string label, Func<IAeadBlockCipherModeTransform> factory, byte[] plaintext, byte[] associatedData)
    {
        // Called as static methods, not extension syntax, and deliberately so: the interface declares its own
        // int Encrypt(ReadOnlySpan<byte>, Span<byte>) taking a caller-supplied output span, and an instance method
        // always beats an extension method. Writing encryptor.Encrypt(plaintext, associatedData) therefore binds to
        // the span overload - treating the associated data as the output buffer - and fails to compile on the return
        // type. These allocating byte[] helpers have to be invoked through their declaring class.
        byte[] sealedBytes;
        using (var encryptor = factory())
            sealedBytes = AeadBlockCipherModeTransformExtensions.Encrypt(encryptor, plaintext, associatedData);

        byte[] opened;
        using (var decryptor = factory())
            opened = AeadBlockCipherModeTransformExtensions.Decrypt(decryptor, sealedBytes, associatedData);

        // A flipped ciphertext byte must fail the tag check rather than return altered plaintext - that is the whole
        // difference between an AEAD mode and a confidentiality-only one.
        var tampered = sealedBytes.ToArray();
        tampered[^1] ^= 0x01;
        string tamperResult;
        using (var decryptor = factory())
            tamperResult = Throws(() => AeadBlockCipherModeTransformExtensions.Decrypt(decryptor, tampered, associatedData));

        // The detached surface returns the tag separately as an AuthenticationTag, for wire formats that carry it
        // out of band rather than appended.
        byte[] detachedCiphertext;
        AuthenticationTag tag;
        using (var encryptor = factory())
            (detachedCiphertext, tag) = encryptor.EncryptDetached(plaintext, associatedData);

        byte[] detachedOpened;
        using (var decryptor = factory())
            detachedOpened = decryptor.DecryptDetached(detachedCiphertext, tag, associatedData);

        Console.WriteLine(
            $"    {label}: {plaintext.Length}B -> {sealedBytes.Length}B (+{sealedBytes.Length - plaintext.Length} tag), " +
            $"round-trip {opened.SequenceEqual(plaintext)}, tampered {tamperResult}");
        Console.WriteLine(
            $"             detached: {detachedCiphertext.Length}B + {tag.Length}B tag, round-trip {detachedOpened.SequenceEqual(plaintext)}");
    }

    /// <summary>
    /// Encrypts one sector under XTS with the given tweak.
    /// </summary>
    /// <param name="dataCipher">The cipher that encrypts the data.</param>
    /// <param name="tweakCipher">The cipher that encrypts the tweak.</param>
    /// <param name="tweak">The tweak identifying the sector.</param>
    /// <param name="plaintext">The sector contents.</param>
    /// <returns>The encrypted sector.</returns>
    private static byte[] EncryptSector(IBlockCipher dataCipher, IBlockCipher tweakCipher, byte[] tweak, byte[] plaintext)
    {
        using var transform = new XtsModeTransform(dataCipher, tweakCipher, tweak);
        var ciphertext = new byte[plaintext.Length];
        transform.Transform(plaintext, ciphertext, encrypt: true);

        return ciphertext;
    }

    /// <summary>
    /// Returns the little-endian 16-byte tweak for a sector number.
    /// </summary>
    /// <param name="sector">The sector number.</param>
    /// <returns>The tweak value.</returns>
    private static byte[] TweakFor(long sector)
    {
        var tweak = new byte[16];
        BitConverter.TryWriteBytes(tweak.AsSpan(0, 8), sector);

        return tweak;
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

    /// <summary>
    /// Invokes a function expected to fail and names the exception type it raised.
    /// </summary>
    /// <param name="action">The function to invoke.</param>
    /// <returns>The exception's type name, or a marker when the call unexpectedly succeeded.</returns>
    private static string Throws(Func<byte[]> action) =>
        Throws(() => { _ = action(); });
}

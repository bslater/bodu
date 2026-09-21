// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SingleShot.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Samples.HybridEncryption.Scenarios;

/// <summary>
/// Demonstrates the single-shot base-mode API of RFC 9180 §6: <see cref="Hpke.Seal" /> encrypts one message to a
/// public key and <see cref="Hpke.Open" /> decrypts it, with no prior handshake and no shared state.
/// </summary>
public static class SingleShot
{
    /// <summary>
    /// Seals a message to the recipient's public key, opens it, then shows what tampering costs.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Single-shot base mode (Seal / Open)",
            what: "Seals one message to a recipient's public key with Hpke.Seal and opens it again, then re-runs Open against a tampered ciphertext, tampered AAD, a wrong info string, a tampered encapsulation and the wrong recipient key - and finally seals the same message twice.",
            why: "HPKE is the standard way to encrypt to a public key with no session and no round trip: the sender makes an ephemeral key pair, derives a shared secret against the recipient's public key, and ships that ephemeral public key - the encapsulation - alongside the ciphertext. info is bound into the key schedule and AAD into the tag, so a message cannot be lifted into another context and still open.",
            expect: "A 40-byte plaintext seals to 56 bytes, the 16-byte difference being the AEAD tag, and round-trips True. All five corruptions are rejected with CryptographicException rather than yielding wrong plaintext. Sealing twice gives a different encapsulation and a different ciphertext (True, True) because the ephemeral key is fresh each time - which is also why this sample prints sizes and booleans rather than fixed hex.");

        // The recipient's long-term key pair. Only its *public* key has to reach the sender, and it travels as a raw
        // 32-byte value - the on-the-wire form.
        using var recipient = Parties.CreateRecipient();
        var recipientPublicKey = recipient.ExportPublicKey();

        var suite = HpkeSuite.X25519_HkdfSha256_Aes128Gcm;
        var info = Parties.Utf8(Parties.Info);
        var associatedData = Parties.Utf8("message-id: 41");
        var plaintext = Parties.Utf8("transfer 250.00 AUD to account 0821-4417");

        Console.WriteLine($"  suite         : KEM={suite.Kem}, KDF={suite.Kdf}, AEAD={suite.Aead}");
        Console.WriteLine($"  recipient pk  : {recipientPublicKey.Length} bytes");

        // One call does the whole exchange: generate an ephemeral key pair, encapsulate a fresh shared secret to the
        // recipient's public key, run the key schedule, and seal the message. The sender needs no key of its own -
        // that asymmetry is what "public key encryption" buys, and it is why the base mode gives no sender
        // authentication whatsoever.
        var (encapsulation, ciphertext) = Hpke.Seal(suite, recipientPublicKey, info, associatedData, plaintext);

        Console.WriteLine($"  encapsulation : {encapsulation.Length} bytes (an ephemeral X25519 public key)");
        Console.WriteLine($"  plaintext     : {plaintext.Length} bytes");
        Console.WriteLine($"  ciphertext    : {ciphertext.Length} bytes (+{ciphertext.Length - plaintext.Length} = the AEAD tag)");

        // The recipient reverses it with its private key. Nothing was negotiated beforehand: the encapsulation is the
        // only thing the sender had to transmit besides the ciphertext.
        var opened = Hpke.Open(suite, recipient, encapsulation, info, associatedData, ciphertext);
        Console.WriteLine($"  round-trip    : {Parties.Text(opened) == Parties.Text(plaintext)}");
        Console.WriteLine($"  recovered     : \"{Parties.Text(opened)}\"");

        // Every input to the key schedule and the AEAD is authenticated. Changing any of them makes Open fail rather
        // than return wrong plaintext, because the tag check fails first.
        Console.WriteLine($"  tampered ciphertext: {Fails(() => Hpke.Open(suite, recipient, encapsulation, info, associatedData, Flip(ciphertext)))}");
        Console.WriteLine($"  tampered AAD       : {Fails(() => Hpke.Open(suite, recipient, encapsulation, info, Parties.Utf8("message-id: 42"), ciphertext))}");
        Console.WriteLine($"  wrong info         : {Fails(() => Hpke.Open(suite, recipient, encapsulation, info: Parties.Utf8("other-app/v1"), associatedData, ciphertext))}");
        Console.WriteLine($"  tampered encapsulation: {Fails(() => Hpke.Open(suite, recipient, Flip(encapsulation), info, associatedData, ciphertext))}");

        // A different recipient key cannot open it either - the shared secret was encapsulated to one public key.
        using var stranger = Parties.CreateSender();
        Console.WriteLine($"  wrong recipient key: {Fails(() => Hpke.Open(suite, stranger, encapsulation, info, associatedData, ciphertext))}");

        // Sealing the same plaintext twice produces different output, because each Seal draws a fresh ephemeral key.
        // That is the construction working as designed, not a nondeterminism bug - and it is why this sample prints
        // sizes and outcomes rather than ciphertext.
        var (secondEncapsulation, secondCiphertext) = Hpke.Seal(suite, recipientPublicKey, info, associatedData, plaintext);
        Console.WriteLine($"  re-seal differs    : encapsulation {Hex.ToHex(encapsulation) != Hex.ToHex(secondEncapsulation)}, ciphertext {Hex.ToHex(ciphertext) != Hex.ToHex(secondCiphertext)}");
        Console.WriteLine($"  but still opens    : {Parties.Text(Hpke.Open(suite, recipient, secondEncapsulation, info, associatedData, secondCiphertext)) == Parties.Text(plaintext)}");

        Console.WriteLine();
    }

    /// <summary>
    /// Returns a copy of a buffer with its final byte flipped.
    /// </summary>
    /// <param name="value">The buffer to corrupt.</param>
    /// <returns>A corrupted copy.</returns>
    internal static byte[] Flip(byte[] value)
    {
        var copy = value.ToArray();
        copy[^1] ^= 0x01;
        return copy;
    }

    /// <summary>
    /// Invokes an operation expected to fail and names the exception type it raised.
    /// </summary>
    /// <param name="action">The operation to invoke.</param>
    /// <returns>The exception's type name, or a marker when the operation unexpectedly succeeded.</returns>
    internal static string Fails(Func<byte[]> action)
    {
        try
        {
            _ = action();
            return "OPENED (unexpected)";
        }
        catch (Exception ex)
        {
            return $"rejected ({ex.GetType().Name})";
        }
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EstablishmentModes.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Samples.HybridEncryption.Scenarios;

/// <summary>
/// Demonstrates the four RFC 9180 establishment modes — <see cref="HpkeMode.Base" />, <see cref="HpkeMode.Psk" />,
/// <see cref="HpkeMode.Auth" /> and <see cref="HpkeMode.AuthPsk" /> — and what each one adds to the key schedule.
/// </summary>
public static class EstablishmentModes
{
    /// <summary>
    /// Runs one seal/open round trip per mode, then shows each mode's extra input being authenticated.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- The four establishment modes ---");

        using var recipient = Parties.CreateRecipient();
        using var sender = Parties.CreateSender();

        var recipientPublicKey = recipient.ExportPublicKey();
        var senderPublicKey = sender.ExportPublicKey();

        var suite = HpkeSuite.X25519_HkdfSha256_ChaCha20Poly1305;
        var info = Parties.Utf8(Parties.Info);
        var aad = Parties.Utf8("mode-demo");
        var psk = Hex.FromHex(Parties.PreSharedKey);
        var pskId = Parties.Utf8(Parties.PreSharedKeyId);

        Console.WriteLine($"  suite         : KEM={suite.Kem}, KDF={suite.Kdf}, AEAD={suite.Aead}");

        // Base: anyone holding the recipient's public key can send. No sender authentication at all.
        var plaintext = Parties.Utf8("mode: base");
        var (baseEnc, baseCt) = Hpke.Seal(suite, recipientPublicKey, info, aad, plaintext);
        Console.WriteLine($"  {HpkeMode.Base,-8} (0x{(int)HpkeMode.Base:X2}): round-trip {Parties.Text(Hpke.Open(suite, recipient, baseEnc, info, aad, baseCt)) == "mode: base"} - no sender authentication");

        // Psk: both parties already share a symmetric secret, which is mixed into the key schedule. The recipient
        // learns the sender belongs to the group that holds the PSK - not which member.
        plaintext = Parties.Utf8("mode: psk");
        var (pskEnc, pskCt) = Hpke.SealPsk(suite, recipientPublicKey, info, psk, pskId, aad, plaintext);
        Console.WriteLine($"  {HpkeMode.Psk,-8} (0x{(int)HpkeMode.Psk:X2}): round-trip {Parties.Text(Hpke.OpenPsk(suite, recipient, pskEnc, info, psk, pskId, aad, pskCt)) == "mode: psk"} - proves PSK possession");

        // Auth: the sender contributes its own long-term key, so the KEM derives the secret from *two* DH operations.
        // The recipient learns the message came from the holder of that specific static key.
        plaintext = Parties.Utf8("mode: auth");
        var (authEnc, authCt) = Hpke.SealAuth(suite, recipientPublicKey, info, sender, aad, plaintext);
        Console.WriteLine($"  {HpkeMode.Auth,-8} (0x{(int)HpkeMode.Auth:X2}): round-trip {Parties.Text(Hpke.OpenAuth(suite, recipient, authEnc, info, senderPublicKey, aad, authCt)) == "mode: auth"} - authenticates the sender's static key");

        // AuthPsk: both at once.
        plaintext = Parties.Utf8("mode: authpsk");
        var (bothEnc, bothCt) = Hpke.SealAuthPsk(suite, recipientPublicKey, info, sender, psk, pskId, aad, plaintext);
        Console.WriteLine($"  {HpkeMode.AuthPsk,-8} (0x{(int)HpkeMode.AuthPsk:X2}): round-trip {Parties.Text(Hpke.OpenAuthPsk(suite, recipient, bothEnc, info, senderPublicKey, psk, pskId, aad, bothCt)) == "mode: authpsk"} - both");

        Console.WriteLine("  Each mode's extra input is authenticated:");

        // A wrong PSK is not a decryption that yields garbage - the key schedule diverges, so the tag check fails.
        Console.WriteLine($"    psk: wrong key    : {SingleShot.Fails(() => Hpke.OpenPsk(suite, recipient, pskEnc, info, SingleShot.Flip(psk), pskId, aad, pskCt))}");
        Console.WriteLine($"    psk: wrong id     : {SingleShot.Fails(() => Hpke.OpenPsk(suite, recipient, pskEnc, info, psk, Parties.Utf8("bodu-sample/psk/2026-04"), aad, pskCt))}");

        // In auth mode the recipient must name the sender it expects. Naming the wrong one fails, which is exactly the
        // authentication property: a message from an unexpected sender does not decrypt.
        Console.WriteLine($"    auth: wrong sender: {SingleShot.Fails(() => Hpke.OpenAuth(suite, recipient, authEnc, info, recipientPublicKey, aad, authCt))}");

        // The modes are not interchangeable: an auth-mode ciphertext cannot be opened as base mode, because the mode
        // identifier itself is bound into the key schedule.
        Console.WriteLine($"    auth read as base : {SingleShot.Fails(() => Hpke.Open(suite, recipient, authEnc, info, aad, authCt))}");
        Console.WriteLine($"    base read as psk  : {SingleShot.Fails(() => Hpke.OpenPsk(suite, recipient, baseEnc, info, psk, pskId, aad, baseCt))}");

        Console.WriteLine();
    }
}

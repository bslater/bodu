// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SuitesAndExport.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Samples.HybridEncryption.Scenarios;

/// <summary>
/// Demonstrates <see cref="HpkeSuite" /> — the KEM/KDF/AEAD triple and the sizes it implies — and the secret-export
/// interface, including the <see cref="HpkeAead.ExportOnly" /> suite that derives keys without encrypting anything.
/// </summary>
public static class SuitesAndExport
{
    /// <summary>
    /// Prints the parameters of each suite, then exports shared secrets through a context pair.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Suites and secret export ---");

        using var recipient = Parties.CreateRecipient();
        var recipientPublicKey = recipient.ExportPublicKey();
        var info = Parties.Utf8(Parties.Info);

        // A suite is the wire-level identity of the algorithms in use. The KEM is fixed at X25519+HKDF-SHA256 in this
        // library; the KDF and AEAD vary. Sizes are derived from the choice, so a caller never hard-codes them.
        Console.WriteLine("  Preset suites:");
        foreach (var suite in new[]
        {
            HpkeSuite.X25519_HkdfSha256_Aes128Gcm,
            HpkeSuite.X25519_HkdfSha256_Aes256Gcm,
            HpkeSuite.X25519_HkdfSha256_ChaCha20Poly1305,
        })
        {
            Console.WriteLine(
                $"    {suite.Aead,-18}: key {suite.AeadKeySizeInBytes,2}B, nonce {suite.AeadNonceSizeInBytes}B, tag {suite.AeadTagSizeInBytes}B, " +
                $"encap {suite.EncapsulationSizeInBytes}B, shared secret {suite.SharedSecretSizeInBytes}B, exportOnly={suite.IsExportOnly}");
        }

        // The constructor takes any valid triple, so a stronger KDF can be paired with any AEAD.
        var sha512Suite = new HpkeSuite(HpkeKem.X25519HkdfSha256, HpkeKdf.HkdfSha512, HpkeAead.Aes256Gcm);
        Console.WriteLine($"  custom triple : KDF={sha512Suite.Kdf} with AEAD={sha512Suite.Aead} -> key {sha512Suite.AeadKeySizeInBytes}B");
        Console.WriteLine($"  round-trip    : {RoundTrips(sha512Suite, recipient, recipientPublicKey, info)}");

        // Export derives additional independent secrets from the same context, labelled by an exporter context string.
        // This is how HPKE bootstraps keys for something other than its own AEAD - a record layer, a MAC, a token.
        var suite2 = HpkeSuite.X25519_HkdfSha256_Aes128Gcm;
        using var sender = HpkeSender.SetupBase(suite2, recipientPublicKey, info, out var encapsulation);
        using var receiver = HpkeReceiver.SetupBase(suite2, recipient, encapsulation, info);

        var senderKey = sender.Export(Parties.Utf8("record-layer key"), 32);
        var receiverKey = receiver.Export(Parties.Utf8("record-layer key"), 32);

        Console.WriteLine($"  export agree  : {Hex.ToHex(senderKey) == Hex.ToHex(receiverKey)} (both sides derive the same 32 bytes)");

        // A different label gives an independent secret - that is the point of labelling, and it means one context can
        // safely key several unrelated things.
        var otherKey = sender.Export(Parties.Utf8("metrics key"), 32);
        Console.WriteLine($"  label matters : {Hex.ToHex(senderKey) != Hex.ToHex(otherKey)} (a different label is a different secret)");

        // Export is a KDF, so any output length can be requested. Note that a shorter request is *not* a truncation of
        // a longer one: RFC 9180's labeled expand binds the requested length into the derivation, so 16, 32 and 64
        // bytes under the same label are three independent secrets rather than prefixes of each other.
        var short16 = sender.Export(Parties.Utf8("record-layer key"), 16);
        var long64 = sender.Export(Parties.Utf8("record-layer key"), 64);
        Console.WriteLine($"  lengths       : {short16.Length}B, {senderKey.Length}B and {long64.Length}B all available under one label");
        Console.WriteLine($"  independent   : {Hex.ToHex(short16) != Hex.ToHex(senderKey)[..(short16.Length * 2)]} (16B is not a prefix of 32B - the length is bound in)");

        // Export is repeatable within a context - unlike Seal, it does not advance the sequence number.
        Console.WriteLine($"  repeatable    : {Hex.ToHex(sender.Export(Parties.Utf8("record-layer key"), 32)) == Hex.ToHex(senderKey)}");

        // The ExportOnly suite has no AEAD at all: it exists for the case where HPKE is used purely as a key-agreement
        // and key-derivation step, with the application doing its own encryption.
        var exportOnly = new HpkeSuite(HpkeKem.X25519HkdfSha256, HpkeKdf.HkdfSha256, HpkeAead.ExportOnly);
        Console.WriteLine($"  ExportOnly    : IsExportOnly={exportOnly.IsExportOnly}, key {exportOnly.AeadKeySizeInBytes}B, tag {exportOnly.AeadTagSizeInBytes}B");

        using var exportSender = HpkeSender.SetupBase(exportOnly, recipientPublicKey, info, out var exportEncapsulation);
        using var exportReceiver = HpkeReceiver.SetupBase(exportOnly, recipient, exportEncapsulation, info);

        Console.WriteLine($"  export works  : {Hex.ToHex(exportSender.Export(Parties.Utf8("app key"), 32)) == Hex.ToHex(exportReceiver.Export(Parties.Utf8("app key"), 32))}");
        Console.WriteLine($"  Seal refused  : {SingleShot.Fails(() => exportSender.Seal([], Parties.Utf8("nope")))}");

        Console.WriteLine();
    }

    /// <summary>
    /// Seals and opens one message under a suite to confirm the triple is usable end to end.
    /// </summary>
    /// <param name="suite">The suite to exercise.</param>
    /// <param name="recipient">The recipient's key pair.</param>
    /// <param name="recipientPublicKey">The recipient's raw public key.</param>
    /// <param name="info">The application-context string.</param>
    /// <returns><see langword="true" /> when the message round-trips.</returns>
    private static bool RoundTrips(HpkeSuite suite, X25519 recipient, byte[] recipientPublicKey, byte[] info)
    {
        var plaintext = Parties.Utf8("suite check");
        var (encapsulation, ciphertext) = Hpke.Seal(suite, recipientPublicKey, info, [], plaintext);

        return Parties.Text(Hpke.Open(suite, recipient, encapsulation, info, [], ciphertext)) == "suite check";
    }
}

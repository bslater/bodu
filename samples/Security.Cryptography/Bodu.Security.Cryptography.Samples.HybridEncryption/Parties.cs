// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Parties.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Security.Cryptography.Samples.HybridEncryption;

/// <summary>
/// The fixed key material and message inputs the scenarios share.
/// </summary>
/// <remarks>
/// <para>
/// The two long-term X25519 keys are the RFC 7748 §6.1 vectors, imported rather than generated, so the recipient and
/// sender identities are the same on every run and across machines.
/// </para>
/// <para>
/// What cannot be fixed is the <em>ephemeral</em> key HPKE generates inside every <c>Setup</c> call: that is the whole
/// point of the construction, and there is deliberately no API to inject it. So the encapsulation and the ciphertext
/// differ on every run, and the scenarios print only facts that do not — round-trip equality, sizes, suite
/// identifiers, and rejection outcomes. Printing a ciphertext here would produce a sample whose documented output
/// could never be reproduced.
/// </para>
/// </remarks>
public static class Parties
{
    /// <summary>The recipient's long-term private key (RFC 7748 §6.1 "Alice").</summary>
    private const string RecipientPrivateHex = "77076d0a7318a57d3c16c17251b26645df4c2f87ebc0992ab177fba51db92c2a";

    /// <summary>The sender's long-term private key, used only in the authenticated modes (RFC 7748 §6.1 "Bob").</summary>
    private const string SenderPrivateHex = "5dab087e624a8a4b79e17f8b83800ee66f3bb1292618b6fd1c2f8b27ff88e0eb";

    /// <summary>The application-context string bound into the key schedule.</summary>
    public const string Info = "bodu-sample/hpke/v1";

    /// <summary>The pre-shared key used by the PSK modes.</summary>
    public const string PreSharedKey = "0247fd33b913760fa1fa51e1892d9f307fbe65eb171e8132c2af18555a738b82";

    /// <summary>The identifier naming which pre-shared key is in use.</summary>
    public const string PreSharedKeyId = "bodu-sample/psk/2026-03";

    /// <summary>
    /// Creates the recipient's key pair from the fixed private key.
    /// </summary>
    /// <returns>A disposable <see cref="X25519" /> holding the recipient's private key.</returns>
    public static X25519 CreateRecipient()
    {
        var key = X25519.Create();
        key.ImportPrivateKey(Hex.FromHex(RecipientPrivateHex));
        return key;
    }

    /// <summary>
    /// Creates the sender's key pair from the fixed private key.
    /// </summary>
    /// <returns>A disposable <see cref="X25519" /> holding the sender's private key.</returns>
    public static X25519 CreateSender()
    {
        var key = X25519.Create();
        key.ImportPrivateKey(Hex.FromHex(SenderPrivateHex));
        return key;
    }

    /// <summary>
    /// Encodes text as UTF-8.
    /// </summary>
    /// <param name="text">The text to encode.</param>
    /// <returns>The encoded bytes.</returns>
    public static byte[] Utf8(string text) =>
        Encoding.UTF8.GetBytes(text);

    /// <summary>
    /// Decodes UTF-8 bytes back to text.
    /// </summary>
    /// <param name="bytes">The bytes to decode.</param>
    /// <returns>The decoded text.</returns>
    public static string Text(ReadOnlySpan<byte> bytes) =>
        Encoding.UTF8.GetString(bytes);
}

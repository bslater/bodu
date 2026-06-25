// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HpkeKnownAnswer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Represents a single RFC 9180 HPKE known-answer vector for the DHKEM(X25519, HKDF-SHA256) / HKDF-SHA256 suites,
/// carrying the recipient material, the sealed messages, and the exported secrets published in Appendix A.
/// </summary>
/// <param name="Name">The human-readable vector name surfaced in test output.</param>
/// <param name="Mode">The HPKE establishment mode.</param>
/// <param name="Aead">The AEAD function of the suite.</param>
/// <param name="Info">The application <c>info</c> binding the exchange.</param>
/// <param name="RecipientPrivateKey">The recipient's X25519 private key (<c>skRm</c>).</param>
/// <param name="Encapsulation">The encapsulated key (<c>enc</c>).</param>
/// <param name="Psk">The pre-shared key, or empty when the mode does not use one.</param>
/// <param name="PskId">The pre-shared key identifier, or empty when the mode does not use one.</param>
/// <param name="SenderPublicKey">The sender's X25519 public key (<c>pkSm</c>), or empty for non-auth modes.</param>
/// <param name="Encryptions">The sealed messages, in sequence order.</param>
/// <param name="Exports">The exported secrets.</param>
public sealed record HpkeKnownAnswer(
    string Name,
    HpkeMode Mode,
    HpkeAead Aead,
    byte[] Info,
    byte[] RecipientPrivateKey,
    byte[] Encapsulation,
    byte[] Psk,
    byte[] PskId,
    byte[] SenderPublicKey,
    IReadOnlyList<HpkeKnownAnswer.Encryption> Encryptions,
    IReadOnlyList<HpkeKnownAnswer.Export> Exports)
    : IKat
{
    /// <summary>
    /// Gets the cipher suite under test, which always uses the X25519 KEM and HKDF-SHA256 KDF.
    /// </summary>
    /// <value>The suite for this vector's <see cref="Aead" />.</value>
    public HpkeSuite Suite => new(HpkeKem.X25519HkdfSha256, HpkeKdf.HkdfSha256, Aead);

    /// <summary>
    /// Reads every HPKE known-answer vector from the supplied embedded-resource JSON stream.
    /// </summary>
    /// <param name="stream">The JSON stream produced from the RFC 9180 test-vector export.</param>
    /// <returns>The parsed vectors in file order.</returns>
    public static IEnumerable<HpkeKnownAnswer> Read(Stream stream)
    {
        using JsonDocument document = JsonDocument.Parse(stream);

        foreach (JsonElement vector in document.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var encryptions = vector.GetProperty("encryptions").EnumerateArray()
                .Select(e => new Encryption(Hex(e, "aad"), Hex(e, "pt"), Hex(e, "ct")))
                .ToList();

            var exports = vector.GetProperty("exports").EnumerateArray()
                .Select(x => new Export(Hex(x, "context"), x.GetProperty("L").GetInt32(), Hex(x, "value")))
                .ToList();

            yield return new HpkeKnownAnswer(
                vector.GetProperty("name").GetString()!,
                (HpkeMode)vector.GetProperty("mode").GetByte(),
                (HpkeAead)vector.GetProperty("aead").GetUInt16(),
                Hex(vector, "info"),
                Hex(vector, "skRm"),
                Hex(vector, "enc"),
                Hex(vector, "psk"),
                Hex(vector, "psk_id"),
                Hex(vector, "pkSm"),
                encryptions,
                exports);
        }
    }

    /// <summary>
    /// Decodes the named hex-string property of <paramref name="element" />, treating a missing or empty value as an
    /// empty array.
    /// </summary>
    /// <param name="element">The JSON object to read.</param>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The decoded bytes, or an empty array.</returns>
    private static byte[] Hex(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value))
            return [];

        string? text = value.GetString();
        return string.IsNullOrEmpty(text) ? [] : Convert.FromHexString(text);
    }

    /// <summary>
    /// Represents one sealed message of a known-answer vector.
    /// </summary>
    /// <param name="AssociatedData">The associated data.</param>
    /// <param name="Plaintext">The plaintext.</param>
    /// <param name="Ciphertext">The expected ciphertext followed by the authentication tag.</param>
    public sealed record Encryption(byte[] AssociatedData, byte[] Plaintext, byte[] Ciphertext);

    /// <summary>
    /// Represents one exported secret of a known-answer vector.
    /// </summary>
    /// <param name="Context">The exporter context.</param>
    /// <param name="Length">The requested export length, in bytes.</param>
    /// <param name="Value">The expected exported secret.</param>
    public sealed record Export(byte[] Context, int Length, byte[] Value);
}

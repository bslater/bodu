// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HpkeKnownAnswer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Represents a single RFC 9180 HPKE known-answer vector for the DHKEM(X25519, HKDF-SHA256) / HKDF-SHA256 suites,
/// carrying the recipient material, the sealed messages, and the exported secrets published in Appendix A.
/// </summary>
/// <remarks>
/// HPKE is a composite construction, so this leaf sits directly on the shared <see cref="CryptoKnownAnswer" /> root
/// rather than collapsing into one of the operation-shaped leaves: it gains the common
/// <see cref="CryptoKnownAnswer.Name" /> and <see cref="CryptoKnownAnswer.Provenance" /> while keeping its own suite,
/// key, sealed-message, and export fields.
/// </remarks>
public sealed record HpkeKnownAnswer
    : CryptoKnownAnswer
{
    /// <summary>
    /// Gets the HPKE establishment mode.
    /// </summary>
    public required HpkeMode Mode { get; init; }

    /// <summary>
    /// Gets the AEAD function of the suite.
    /// </summary>
    public required HpkeAead Aead { get; init; }

    /// <summary>
    /// Gets the application <c>info</c> binding the exchange.
    /// </summary>
    public required byte[] Info { get; init; }

    /// <summary>
    /// Gets the recipient's X25519 private key (<c>skRm</c>).
    /// </summary>
    public required byte[] RecipientPrivateKey { get; init; }

    /// <summary>
    /// Gets the encapsulated key (<c>enc</c>).
    /// </summary>
    public required byte[] Encapsulation { get; init; }

    /// <summary>
    /// Gets the pre-shared key, or empty when the mode does not use one.
    /// </summary>
    public required byte[] Psk { get; init; }

    /// <summary>
    /// Gets the pre-shared key identifier, or empty when the mode does not use one.
    /// </summary>
    public required byte[] PskId { get; init; }

    /// <summary>
    /// Gets the sender's X25519 public key (<c>pkSm</c>), or empty for non-auth modes.
    /// </summary>
    public required byte[] SenderPublicKey { get; init; }

    /// <summary>
    /// Gets the sealed messages, in sequence order.
    /// </summary>
    public required IReadOnlyList<Encryption> Encryptions { get; init; }

    /// <summary>
    /// Gets the exported secrets.
    /// </summary>
    public required IReadOnlyList<Export> Exports { get; init; }

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
        using var document = JsonDocument.Parse(stream);

        foreach (JsonElement vector in document.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var encryptions = vector.GetProperty("encryptions").EnumerateArray()
                .Select(e => new Encryption(Hex(e, "aad"), Hex(e, "pt"), Hex(e, "ct")))
                .ToList();

            var exports = vector.GetProperty("exports").EnumerateArray()
                .Select(x => new Export(Hex(x, "context"), x.GetProperty("L").GetInt32(), Hex(x, "value")))
                .ToList();

            yield return new HpkeKnownAnswer
            {
                Name = vector.GetProperty("name").GetString()!,
                Provenance = KatProvenance.Rfc("RFC 9180 Appendix A"),
                Mode = (HpkeMode)vector.GetProperty("mode").GetByte(),
                Aead = (HpkeAead)vector.GetProperty("aead").GetUInt16(),
                Info = Hex(vector, "info"),
                RecipientPrivateKey = Hex(vector, "skRm"),
                Encapsulation = Hex(vector, "enc"),
                Psk = Hex(vector, "psk"),
                PskId = Hex(vector, "psk_id"),
                SenderPublicKey = Hex(vector, "pkSm"),
                Encryptions = encryptions,
                Exports = exports,
            };
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

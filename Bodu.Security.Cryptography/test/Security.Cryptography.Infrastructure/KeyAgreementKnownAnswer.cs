// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeyAgreementKnownAnswer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Represents a single key-agreement known-answer test (KAT) vector — a private key, a peer public key, and either the
/// expected shared secret or the expectation that the derivation is rejected.
/// </summary>
public sealed record KeyAgreementKnownAnswer : AsymmetricKnownAnswer
{
    /// <summary>
    /// Gets the local party's private key.
    /// </summary>
    public required byte[] PrivateKey { get; init; }

    /// <summary>
    /// Gets the peer's public key supplied to the derivation.
    /// </summary>
    public required byte[] PeerPublicKey { get; init; }

    /// <summary>
    /// Gets the expected shared secret; all-zero for rows where <see cref="ExpectRejection" /> is set.
    /// </summary>
    public required byte[] ExpectedSharedSecret { get; init; }

    /// <summary>
    /// Gets a value indicating whether the derivation must be rejected (for X25519, the strict RFC 7748 §6.1 all-zero
    /// shared-secret check). When <see langword="false" /> the derivation must produce
    /// <see cref="ExpectedSharedSecret" />.
    /// </summary>
    public required bool ExpectRejection { get; init; }

    /// <summary>
    /// Reads all key-agreement vectors from a <c>Field = value</c> KAT stream with the fields <c>Name</c>,
    /// <c>Private</c>, <c>Public</c>, <c>Shared</c>, and <c>Reject</c>.
    /// </summary>
    /// <param name="stream">A readable text stream containing the KAT data.</param>
    /// <returns>The parsed vectors in source order.</returns>
    /// <exception cref="FormatException">A record is missing a required field or a value is malformed.</exception>
    public static IEnumerable<KeyAgreementKnownAnswer> Read(Stream stream)
    {
        foreach (Dictionary<string, string> record in HexFieldKatReader.Read(stream))
        {
            yield return new KeyAgreementKnownAnswer
            {
                Name = HexFieldKatReader.GetRequired(record, "Name"),
                PrivateKey = Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Private")),
                PeerPublicKey = Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Public")),
                ExpectedSharedSecret = Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Shared")),
                ExpectRejection = bool.Parse(HexFieldKatReader.GetRequired(record, "Reject")),
            };
        }
    }
}

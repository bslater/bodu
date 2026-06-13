// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeyAgreementKnownAnswer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Represents a single key-agreement known-answer test (KAT) vector — a private key, a peer public key, and either the
/// expected shared secret or the expectation that the derivation is rejected.
/// </summary>
/// <param name="Name">The human-readable label identifying this row in test output.</param>
/// <param name="PrivateKey">The local party's private key.</param>
/// <param name="PeerPublicKey">The peer's public key supplied to the derivation.</param>
/// <param name="ExpectedSharedSecret">
/// The expected shared secret; all-zero for rows where <paramref name="ExpectRejection" /> is set.
/// </param>
/// <param name="ExpectRejection">
/// <see langword="true" /> when the derivation must be rejected (for X25519, the strict RFC 7748 §6.1 all-zero
/// shared-secret check); <see langword="false" /> when it must produce <paramref name="ExpectedSharedSecret" />.
/// </param>
public sealed record KeyAgreementKnownAnswer(
    string Name,
    byte[] PrivateKey,
    byte[] PeerPublicKey,
    byte[] ExpectedSharedSecret,
    bool ExpectRejection) : IKat
{
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
            yield return new KeyAgreementKnownAnswer(
                HexFieldKatReader.GetRequired(record, "Name"),
                Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Private")),
                Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Public")),
                Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Shared")),
                bool.Parse(HexFieldKatReader.GetRequired(record, "Reject")));
        }
    }
}

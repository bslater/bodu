// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SignatureKnownAnswer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Represents a single signature known-answer test (KAT) vector, unifying signature generation and verification for the
/// Ed25519 and ML-DSA families. A vector carries the public key, message, and a signature that is either the expected
/// output (signing rows) or a candidate under test (verification rows), plus the verification verdict.
/// </summary>
/// <remarks>
/// For verification rows, <see cref="Signature" /> is the candidate and <see cref="ExpectedValid" /> is the verdict.
/// For signing rows, <see cref="Signature" /> is the expected output, <see cref="PrivateKey" /> is the signing key, and
/// <see cref="Deterministic" /> / <see cref="Rnd" /> select deterministic or hedged signing. <see cref="Context" /> is
/// the ML-DSA context string (empty or <see langword="null" /> for Ed25519).
/// </remarks>
public sealed record SignatureKnownAnswer : AsymmetricKnownAnswer
{
    /// <summary>
    /// Gets the signer's public key.
    /// </summary>
    public required byte[] PublicKey { get; init; }

    /// <summary>
    /// Gets the signer's private key when the vector is a signing KAT; otherwise <see langword="null" />.
    /// </summary>
    public byte[]? PrivateKey { get; init; }

    /// <summary>
    /// Gets the ML-DSA signature context string, or <see langword="null" /> when the scheme takes no context.
    /// </summary>
    public byte[]? Context { get; init; }

    /// <summary>
    /// Gets the message bytes; empty when the vector signs or verifies the empty message.
    /// </summary>
    public required byte[] Message { get; init; }

    /// <summary>
    /// Gets the signature — the expected output for a signing row, or the candidate under test for a verification row.
    /// </summary>
    public required byte[] Signature { get; init; }

    /// <summary>
    /// Gets a value indicating whether verification must accept the signature. Defaults to <see langword="true" />;
    /// signing rows leave it at the default.
    /// </summary>
    public bool ExpectedValid { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether a signing row uses deterministic signing (rnd = 0). Defaults to
    /// <see langword="false" />.
    /// </summary>
    public bool Deterministic { get; init; }

    /// <summary>
    /// Gets the explicit 32-byte signer randomness for a hedged signing row, or <see langword="null" /> otherwise.
    /// </summary>
    public byte[]? Rnd { get; init; }

    /// <summary>
    /// Reads all signature vectors from a <c>Field = value</c> KAT stream with the fields <c>Name</c>, <c>Public</c>,
    /// <c>Msg</c>, <c>Sig</c>, and <c>Valid</c>, plus an optional <c>Private</c> field. This is the Ed25519 / Wycheproof
    /// verification format; the ML-DSA ACVP loaders construct vectors of this type directly from their own field set.
    /// </summary>
    /// <param name="stream">A readable text stream containing the KAT data.</param>
    /// <returns>The parsed vectors in source order.</returns>
    /// <exception cref="FormatException">A record is missing a required field or a value is malformed.</exception>
    public static IEnumerable<SignatureKnownAnswer> Read(Stream stream)
    {
        foreach (Dictionary<string, string> record in HexFieldKatReader.Read(stream))
        {
            yield return new SignatureKnownAnswer
            {
                Name = HexFieldKatReader.GetRequired(record, "Name"),
                PublicKey = Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Public")),
                Message = Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Msg")),
                Signature = Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Sig")),
                ExpectedValid = bool.Parse(HexFieldKatReader.GetRequired(record, "Valid")),
                PrivateKey = record.TryGetValue("Private", out string? privateHex) ? Convert.FromHexString(privateHex) : null,
            };
        }
    }
}

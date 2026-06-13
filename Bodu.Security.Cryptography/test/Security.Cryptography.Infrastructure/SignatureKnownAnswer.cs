// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SignatureKnownAnswer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Represents a single signature known-answer test (KAT) vector — a public key, message, and candidate signature with
/// the expected verification outcome, optionally carrying the private key for deterministic signing checks.
/// </summary>
/// <param name="Name">The human-readable label identifying this row in test output.</param>
/// <param name="PublicKey">The signer's public key.</param>
/// <param name="Message">The message bytes; empty when the vector signs the empty message.</param>
/// <param name="Signature">The candidate signature under test.</param>
/// <param name="ExpectedValid">
/// <see langword="true" /> when verification must accept the signature; <see langword="false" /> when it must reject
/// it.
/// </param>
/// <param name="PrivateKey">
/// The signer's private key when the vector doubles as a deterministic signing KAT; otherwise <see langword="null" />.
/// </param>
public sealed record SignatureKnownAnswer(
    string Name,
    byte[] PublicKey,
    byte[] Message,
    byte[] Signature,
    bool ExpectedValid,
    byte[]? PrivateKey = null) : IKat
{
    /// <summary>
    /// Reads all signature vectors from a <c>Field = value</c> KAT stream with the fields <c>Name</c>, <c>Public</c>,
    /// <c>Msg</c>, <c>Sig</c>, and <c>Valid</c>, plus an optional <c>Private</c> field.
    /// </summary>
    /// <param name="stream">A readable text stream containing the KAT data.</param>
    /// <returns>The parsed vectors in source order.</returns>
    /// <exception cref="FormatException">A record is missing a required field or a value is malformed.</exception>
    public static IEnumerable<SignatureKnownAnswer> Read(Stream stream)
    {
        foreach (Dictionary<string, string> record in HexFieldKatReader.Read(stream))
        {
            yield return new SignatureKnownAnswer(
                HexFieldKatReader.GetRequired(record, "Name"),
                Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Public")),
                Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Msg")),
                Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Sig")),
                bool.Parse(HexFieldKatReader.GetRequired(record, "Valid")),
                record.TryGetValue("Private", out var privateHex) ? Convert.FromHexString(privateHex) : null);
        }
    }
}

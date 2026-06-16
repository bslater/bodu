// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DsaSigGenKnownAnswer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Represents a single signature-generation known-answer test (KAT) vector with an expected signature, covering both
/// deterministic rows and hedged rows that carry explicit signer randomness.
/// </summary>
/// <param name="Name">The human-readable label identifying this row in test output.</param>
/// <param name="ParameterSet">The parameter-set name, such as <c>"ML-DSA-65"</c>.</param>
/// <param name="Deterministic">
/// <see langword="true" /> for deterministic rows (rnd = 0); <see langword="false" /> for hedged rows.
/// </param>
/// <param name="PrivateKey">The encoded private key.</param>
/// <param name="PublicKey">The encoded public key matching <paramref name="PrivateKey" />.</param>
/// <param name="Context">The signature context string; empty when the row signs without context.</param>
/// <param name="Message">The message bytes.</param>
/// <param name="Rnd">The explicit 32-byte signer randomness for hedged rows; <see langword="null" /> otherwise.</param>
/// <param name="ExpectedSignature">The expected encoded signature.</param>
public sealed record DsaSigGenKnownAnswer(
    string Name,
    string ParameterSet,
    bool Deterministic,
    byte[] PrivateKey,
    byte[] PublicKey,
    byte[] Context,
    byte[] Message,
    byte[]? Rnd,
    byte[] ExpectedSignature) : IKat
{
    /// <summary>
    /// Reads all signature-generation vectors from a <c>Field = value</c> KAT stream with the fields <c>Name</c>,
    /// <c>Set</c>, <c>Deterministic</c>, <c>Sk</c>, <c>Pk</c>, <c>Context</c>, <c>Message</c>, optional <c>Rnd</c>, and
    /// <c>Signature</c>.
    /// </summary>
    /// <param name="stream">A readable text stream containing the KAT data.</param>
    /// <returns>The parsed vectors in source order.</returns>
    /// <exception cref="FormatException">A record is missing a required field or a value is malformed.</exception>
    public static IEnumerable<DsaSigGenKnownAnswer> Read(Stream stream)
    {
        foreach (Dictionary<string, string> record in HexFieldKatReader.Read(stream))
        {
            yield return new DsaSigGenKnownAnswer(
                HexFieldKatReader.GetRequired(record, "Name"),
                HexFieldKatReader.GetRequired(record, "Set"),
                bool.Parse(HexFieldKatReader.GetRequired(record, "Deterministic")),
                Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Sk")),
                Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Pk")),
                Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Context")),
                Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Message")),
                record.TryGetValue("Rnd", out string? rnd) ? Convert.FromHexString(rnd) : null,
                Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Signature")));
        }
    }
}

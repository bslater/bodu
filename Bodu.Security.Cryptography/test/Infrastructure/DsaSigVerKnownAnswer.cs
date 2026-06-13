// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DsaSigVerKnownAnswer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Represents a single signature-verification known-answer test (KAT) vector with the expected verdict.
/// </summary>
/// <param name="Name">The human-readable label identifying this row in test output.</param>
/// <param name="ParameterSet">The parameter-set name, such as <c>"ML-DSA-65"</c>.</param>
/// <param name="PublicKey">The encoded public key.</param>
/// <param name="Context">The signature context string; empty when the row verifies without context.</param>
/// <param name="Message">The message bytes.</param>
/// <param name="Signature">The candidate encoded signature.</param>
/// <param name="ExpectedValid"><see langword="true" /> when verification must accept.</param>
public sealed record DsaSigVerKnownAnswer(
    string Name,
    string ParameterSet,
    byte[] PublicKey,
    byte[] Context,
    byte[] Message,
    byte[] Signature,
    bool ExpectedValid) : IKat
{
    /// <summary>
    /// Reads all signature-verification vectors from a <c>Field = value</c> KAT stream with the fields <c>Name</c>,
    /// <c>Set</c>, <c>Pk</c>, <c>Context</c>, <c>Message</c>, <c>Signature</c>, and <c>Valid</c>.
    /// </summary>
    /// <param name="stream">A readable text stream containing the KAT data.</param>
    /// <returns>The parsed vectors in source order.</returns>
    /// <exception cref="FormatException">A record is missing a required field or a value is malformed.</exception>
    public static IEnumerable<DsaSigVerKnownAnswer> Read(Stream stream)
    {
        foreach (Dictionary<string, string> record in HexFieldKatReader.Read(stream))
        {
            yield return new DsaSigVerKnownAnswer(
                HexFieldKatReader.GetRequired(record, "Name"),
                HexFieldKatReader.GetRequired(record, "Set"),
                Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Pk")),
                Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Context")),
                Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Message")),
                Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Signature")),
                bool.Parse(HexFieldKatReader.GetRequired(record, "Valid")));
        }
    }
}

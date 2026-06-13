// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DsaKeyGenKnownAnswer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Represents a single signature-scheme key-generation known-answer test (KAT) vector: the seed and the expected
/// encoded key pair.
/// </summary>
/// <param name="Name">The human-readable label identifying this row in test output.</param>
/// <param name="ParameterSet">The parameter-set name, such as <c>"ML-DSA-65"</c>.</param>
/// <param name="Seed">The key-generation seed ξ.</param>
/// <param name="ExpectedPublicKey">The expected encoded public key.</param>
/// <param name="ExpectedPrivateKey">The expected encoded private key.</param>
public sealed record DsaKeyGenKnownAnswer(
    string Name,
    string ParameterSet,
    byte[] Seed,
    byte[] ExpectedPublicKey,
    byte[] ExpectedPrivateKey) : IKat
{
    /// <summary>
    /// Reads all key-generation vectors from a <c>Field = value</c> KAT stream with the fields <c>Name</c>,
    /// <c>Set</c>, <c>Seed</c>, <c>Pk</c>, and <c>Sk</c>.
    /// </summary>
    /// <param name="stream">A readable text stream containing the KAT data.</param>
    /// <returns>The parsed vectors in source order.</returns>
    /// <exception cref="FormatException">A record is missing a required field or a value is malformed.</exception>
    public static IEnumerable<DsaKeyGenKnownAnswer> Read(Stream stream)
    {
        foreach (Dictionary<string, string> record in HexFieldKatReader.Read(stream))
        {
            yield return new DsaKeyGenKnownAnswer(
                HexFieldKatReader.GetRequired(record, "Name"),
                HexFieldKatReader.GetRequired(record, "Set"),
                Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Seed")),
                Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Pk")),
                Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Sk")));
        }
    }
}

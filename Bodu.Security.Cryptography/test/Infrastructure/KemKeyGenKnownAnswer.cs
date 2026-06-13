// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KemKeyGenKnownAnswer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Represents a single KEM key-generation known-answer test (KAT) vector: the seeds and the expected encoded key
/// pair.
/// </summary>
/// <param name="Name">The human-readable label identifying this row in test output.</param>
/// <param name="ParameterSet">The parameter-set name, such as <c>"ML-KEM-768"</c>.</param>
/// <param name="D">The 32-byte key-derivation seed.</param>
/// <param name="Z">The 32-byte implicit-rejection seed.</param>
/// <param name="ExpectedEncapsulationKey">The expected encoded encapsulation key.</param>
/// <param name="ExpectedDecapsulationKey">The expected encoded decapsulation key.</param>
public sealed record KemKeyGenKnownAnswer(
    string Name,
    string ParameterSet,
    byte[] D,
    byte[] Z,
    byte[] ExpectedEncapsulationKey,
    byte[] ExpectedDecapsulationKey) : IKat
{
    /// <summary>
    /// Reads all key-generation vectors from a <c>Field = value</c> KAT stream with the fields <c>Name</c>,
    /// <c>Set</c>, <c>D</c>, <c>Z</c>, <c>Ek</c>, and <c>Dk</c>.
    /// </summary>
    /// <param name="stream">A readable text stream containing the KAT data.</param>
    /// <returns>The parsed vectors in source order.</returns>
    /// <exception cref="FormatException">A record is missing a required field or a value is malformed.</exception>
    public static IEnumerable<KemKeyGenKnownAnswer> Read(Stream stream)
    {
        foreach (Dictionary<string, string> record in HexFieldKatReader.Read(stream))
        {
            yield return new KemKeyGenKnownAnswer(
                HexFieldKatReader.GetRequired(record, "Name"),
                HexFieldKatReader.GetRequired(record, "Set"),
                Convert.FromHexString(HexFieldKatReader.GetRequired(record, "D")),
                Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Z")),
                Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Ek")),
                Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Dk")));
        }
    }
}

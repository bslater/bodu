// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KemKeyCheckKnownAnswer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Represents a single KEM key-import validation known-answer test (KAT) vector: a candidate encoded key and whether
/// import must accept it.
/// </summary>
/// <param name="Name">The human-readable label identifying this row in test output.</param>
/// <param name="ParameterSet">The parameter-set name, such as <c>"ML-KEM-768"</c>.</param>
/// <param name="Kind">Either <c>"encapsulation"</c> or <c>"decapsulation"</c>.</param>
/// <param name="Key">The candidate encoded key.</param>
/// <param name="ExpectedValid"><see langword="true" /> when the import must succeed.</param>
public sealed record KemKeyCheckKnownAnswer(
    string Name,
    string ParameterSet,
    string Kind,
    byte[] Key,
    bool ExpectedValid) : IKat
{
    /// <summary>
    /// Reads all key-check vectors from a <c>Field = value</c> KAT stream with the fields <c>Name</c>, <c>Set</c>,
    /// <c>Kind</c>, <c>Key</c>, and <c>Valid</c>.
    /// </summary>
    /// <param name="stream">A readable text stream containing the KAT data.</param>
    /// <returns>The parsed vectors in source order.</returns>
    /// <exception cref="FormatException">A record is missing a required field or a value is malformed.</exception>
    public static IEnumerable<KemKeyCheckKnownAnswer> Read(Stream stream)
    {
        foreach (Dictionary<string, string> record in HexFieldKatReader.Read(stream))
        {
            yield return new KemKeyCheckKnownAnswer(
                HexFieldKatReader.GetRequired(record, "Name"),
                HexFieldKatReader.GetRequired(record, "Set"),
                HexFieldKatReader.GetRequired(record, "Kind"),
                Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Key")),
                bool.Parse(HexFieldKatReader.GetRequired(record, "Valid")));
        }
    }
}

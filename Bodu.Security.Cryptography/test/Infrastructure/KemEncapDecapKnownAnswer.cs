// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KemEncapDecapKnownAnswer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Represents a single KEM encapsulation or decapsulation known-answer test (KAT) vector.
/// </summary>
/// <param name="Name">The human-readable label identifying this row in test output.</param>
/// <param name="ParameterSet">The parameter-set name, such as <c>"ML-KEM-768"</c>.</param>
/// <param name="Function">Either <c>"encapsulation"</c> or <c>"decapsulation"</c>.</param>
/// <param name="Key">The encoded encapsulation key (encapsulation rows) or decapsulation key (decapsulation rows).</param>
/// <param name="M">The fixed 32-byte encapsulation randomness, or <see langword="null" /> for decapsulation rows.</param>
/// <param name="Ciphertext">The expected (encapsulation) or candidate (decapsulation) ciphertext.</param>
/// <param name="SharedSecret">The expected shared secret, including the implicit-rejection key for tampered rows.</param>
public sealed record KemEncapDecapKnownAnswer(
    string Name,
    string ParameterSet,
    string Function,
    byte[] Key,
    byte[]? M,
    byte[] Ciphertext,
    byte[] SharedSecret) : IKat
{
    /// <summary>
    /// Reads all encapsulation/decapsulation vectors from a <c>Field = value</c> KAT stream with the fields
    /// <c>Name</c>, <c>Set</c>, <c>Function</c>, <c>Ek</c> or <c>Dk</c>, optional <c>M</c>, <c>C</c>, and <c>K</c>.
    /// </summary>
    /// <param name="stream">A readable text stream containing the KAT data.</param>
    /// <returns>The parsed vectors in source order.</returns>
    /// <exception cref="FormatException">A record is missing a required field or a value is malformed.</exception>
    public static IEnumerable<KemEncapDecapKnownAnswer> Read(Stream stream)
    {
        foreach (Dictionary<string, string> record in HexFieldKatReader.Read(stream))
        {
            var function = HexFieldKatReader.GetRequired(record, "Function");
            var keyField = function == "encapsulation" ? "Ek" : "Dk";

            yield return new KemEncapDecapKnownAnswer(
                HexFieldKatReader.GetRequired(record, "Name"),
                HexFieldKatReader.GetRequired(record, "Set"),
                function,
                Convert.FromHexString(HexFieldKatReader.GetRequired(record, keyField)),
                record.TryGetValue("M", out var m) ? Convert.FromHexString(m) : null,
                Convert.FromHexString(HexFieldKatReader.GetRequired(record, "C")),
                Convert.FromHexString(HexFieldKatReader.GetRequired(record, "K")));
        }
    }
}

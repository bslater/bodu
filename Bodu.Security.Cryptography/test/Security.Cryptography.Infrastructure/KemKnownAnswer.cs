// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KemKnownAnswer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Represents a single KEM encapsulation or decapsulation known-answer test (KAT) vector.
/// </summary>
public sealed record KemKnownAnswer : AsymmetricKnownAnswer
{
    /// <summary>
    /// Gets the function this vector exercises — either <c>"encapsulation"</c> or <c>"decapsulation"</c>.
    /// </summary>
    public required string Function { get; init; }

    /// <summary>
    /// Gets the encoded encapsulation key (encapsulation rows) or decapsulation key (decapsulation rows).
    /// </summary>
    public required byte[] Key { get; init; }

    /// <summary>
    /// Gets the fixed 32-byte encapsulation randomness, or <see langword="null" /> for decapsulation rows.
    /// </summary>
    public byte[]? M { get; init; }

    /// <summary>
    /// Gets the expected (encapsulation) or candidate (decapsulation) ciphertext.
    /// </summary>
    public required byte[] Ciphertext { get; init; }

    /// <summary>
    /// Gets the expected shared secret, including the implicit-rejection key for tampered decapsulation rows.
    /// </summary>
    public required byte[] SharedSecret { get; init; }

    /// <summary>
    /// Reads all encapsulation/decapsulation vectors from a <c>Field = value</c> KAT stream with the fields <c>Name</c>,
    /// <c>Set</c>, <c>Function</c>, <c>Ek</c> or <c>Dk</c>, optional <c>M</c>, <c>C</c>, and <c>K</c>.
    /// </summary>
    /// <param name="stream">A readable text stream containing the KAT data.</param>
    /// <returns>The parsed vectors in source order.</returns>
    /// <exception cref="FormatException">A record is missing a required field or a value is malformed.</exception>
    public static IEnumerable<KemKnownAnswer> Read(Stream stream)
    {
        foreach (Dictionary<string, string> record in HexFieldKatReader.Read(stream))
        {
            string function = HexFieldKatReader.GetRequired(record, "Function");
            string keyField = function == "encapsulation" ? "Ek" : "Dk";

            yield return new KemKnownAnswer
            {
                Name = HexFieldKatReader.GetRequired(record, "Name"),
                ParameterSet = HexFieldKatReader.GetRequired(record, "Set"),
                Function = function,
                Key = Convert.FromHexString(HexFieldKatReader.GetRequired(record, keyField)),
                M = record.TryGetValue("M", out string? m) ? Convert.FromHexString(m) : null,
                Ciphertext = Convert.FromHexString(HexFieldKatReader.GetRequired(record, "C")),
                SharedSecret = Convert.FromHexString(HexFieldKatReader.GetRequired(record, "K")),
            };
        }
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeyValidationKnownAnswer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Represents a single key-import validation known-answer test (KAT) vector: a candidate encoded key and whether import
/// must accept it.
/// </summary>
public sealed record KeyValidationKnownAnswer : AsymmetricKnownAnswer
{
    /// <summary>
    /// Gets the role of the candidate key — for KEMs, either <c>"encapsulation"</c> or <c>"decapsulation"</c>.
    /// </summary>
    public required string Kind { get; init; }

    /// <summary>
    /// Gets the candidate encoded key.
    /// </summary>
    public required byte[] Key { get; init; }

    /// <summary>
    /// Gets a value indicating whether the import must succeed.
    /// </summary>
    public required bool ExpectedValid { get; init; }

    /// <summary>
    /// Reads all key-validation vectors from a <c>Field = value</c> KAT stream with the fields <c>Name</c>, <c>Set</c>,
    /// <c>Kind</c>, <c>Key</c>, and <c>Valid</c>.
    /// </summary>
    /// <param name="stream">A readable text stream containing the KAT data.</param>
    /// <returns>The parsed vectors in source order.</returns>
    /// <exception cref="FormatException">A record is missing a required field or a value is malformed.</exception>
    public static IEnumerable<KeyValidationKnownAnswer> Read(Stream stream)
    {
        foreach (Dictionary<string, string> record in HexFieldKatReader.Read(stream))
        {
            yield return new KeyValidationKnownAnswer
            {
                Name = HexFieldKatReader.GetRequired(record, "Name"),
                ParameterSet = HexFieldKatReader.GetRequired(record, "Set"),
                Kind = HexFieldKatReader.GetRequired(record, "Kind"),
                Key = Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Key")),
                ExpectedValid = bool.Parse(HexFieldKatReader.GetRequired(record, "Valid")),
            };
        }
    }
}

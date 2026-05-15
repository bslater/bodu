// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingKnownAnswerVector.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Represents a positive Known Answer Test (KAT) vector sourced from a published specification or reference
/// implementation. A vector pairs a binary input with the canonical encoded form so that data-driven tests can
/// verify <see cref="Base16" />, <see cref="Base32" />, <see cref="Base64" />, <see cref="Base58" />, and
/// <see cref="Base85" /> implementations against authoritative references.
/// </summary>
/// <param name="Description">A short human-readable label identifying the vector.</param>
/// <param name="DecodedBytes">The binary input.</param>
/// <param name="Encoded">The expected encoded form (canonical case where the specification requires it).</param>
/// <param name="Source">The citation for the vector's origin (specification clause, reference implementation,
/// etc.).</param>
public sealed record EncodingKnownAnswerVector(
    string Description,
    byte[] DecodedBytes,
    string Encoded,
    string? Source = null)
{

    /// <summary>
    /// Builds a vector from an ASCII string input, used for the many RFC 4648 §10 examples that express the input
    /// as a readable ASCII fragment ("f", "fo", "foo", "foob", "fooba", "foobar").
    /// </summary>
    /// <param name="description">A short label.</param>
    /// <param name="ascii">The ASCII input.</param>
    /// <param name="encoded">The expected encoded form.</param>
    /// <param name="source">The citation.</param>
    /// <returns>A KAT vector with <paramref name="ascii" /> resolved to its byte representation.</returns>
    public static EncodingKnownAnswerVector FromAscii(string description, string ascii, string encoded, string? source = null) =>
        new(description, System.Text.Encoding.ASCII.GetBytes(ascii), encoded, source);

    /// <summary>
    /// Builds a vector from a hexadecimal string input, used by Bitcoin Core's <c>base58_encode_decode.json</c> and
    /// similar fixtures that document binary inputs in hex form.
    /// </summary>
    /// <param name="description">A short label.</param>
    /// <param name="decodedHex">The binary input expressed as hex characters. An empty string yields an empty byte
    /// array.</param>
    /// <param name="encoded">The expected encoded form.</param>
    /// <param name="source">The citation.</param>
    /// <returns>A KAT vector with <paramref name="decodedHex" /> resolved to its byte representation.</returns>
    public static EncodingKnownAnswerVector FromHex(string description, string decodedHex, string encoded, string? source = null) =>
        new(description, decodedHex.Length == 0 ? Array.Empty<byte>() : Convert.FromHexString(decodedHex), encoded, source);

    /// <summary>
    /// Returns the human-readable label, used by MSTest's <c>[DynamicData]</c> infrastructure when generating test
    /// names so each row is identifiable in failure output.
    /// </summary>
    /// <returns>The description.</returns>
    public override string ToString() =>
        Source is null ? Description : $"{Description} [{Source}]";

}

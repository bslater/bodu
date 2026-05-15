// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingNegativeDecodeVector.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Represents a negative Known Answer Test vector — a malformed encoded input that the decoder must reject with a
/// specific exception type. Negative vectors capture the contract that each encoding makes about which inputs are
/// invalid (excluded characters, wrong lengths, misplaced padding, mismatched variants, etc.).
/// </summary>
/// <param name="Description">A short human-readable label identifying the negative scenario.</param>
/// <param name="MalformedInput">The malformed encoded string.</param>
/// <param name="ExpectedExceptionType">The exception type the decoder must throw on this input.</param>
/// <param name="Source">Optional citation describing the rule the input violates.</param>
public sealed record EncodingNegativeDecodeVector(
    string Description,
    string MalformedInput,
    Type ExpectedExceptionType,
    string? Source = null)
{

    /// <summary>
    /// Returns the human-readable label, used by MSTest's <c>[DynamicData]</c> infrastructure when generating test
    /// names.
    /// </summary>
    /// <returns>The description.</returns>
    public override string ToString() =>
        Source is null ? Description : $"{Description} [{Source}]";

}

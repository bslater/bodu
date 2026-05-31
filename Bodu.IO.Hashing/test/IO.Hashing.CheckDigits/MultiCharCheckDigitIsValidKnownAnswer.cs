// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiCharCheckDigitIsValidKnownAnswer.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


using Bodu.Test.Kat;

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Represents a single known-answer test vector for the <c>IsValid</c> surface of a
/// <see cref="MultiCharCheckDigitAlgorithm" />, pairing a complete sequence (in its canonical form) with the
/// validity verdict the algorithm is expected to render.
/// </summary>
/// <remarks>
/// Concrete test classes use these vectors to exercise <c>IsValid</c> with inputs that may not match the simple
/// <c>Body + ExpectedCheck</c> concatenation used for streaming and <c>Compute</c> tests. For example, IBAN's
/// canonical text form interleaves the country code and check digits ahead of the BBAN, so positive vectors must
/// reflect that layout rather than the body order absorbed by <c>Append</c>.
/// </remarks>
public sealed record MultiCharCheckDigitIsValidKnownAnswer : IKat
{

    /// <summary>
    /// Gets the verdict the algorithm's <c>IsValid</c> helper is expected to return for <see cref="Value" />.
    /// </summary>
    /// <returns><see langword="true" /> if the sequence is valid under the algorithm; otherwise, <see langword="false" />.</returns>
    public required bool ExpectedIsValid { get; init; }
    /// <summary>
    /// Gets a short descriptive name used in test output to identify the vector.
    /// </summary>
    /// <returns>A non-empty descriptive label such as <c>"WikipediaGB"</c> or <c>"CheckDigitsTampered"</c>.</returns>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the complete sequence — body and check characters in their canonical form — passed to the
    /// algorithm's <c>IsValid</c> static helper.
    /// </summary>
    /// <returns>A non-null string drawn from the algorithm's declared input alphabet.</returns>
    public required string Value { get; init; }

}

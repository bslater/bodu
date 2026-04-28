// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AlphanumericCheckDigitAlgorithmSpecification.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AlphanumericCheckDigitAlgorithmSpecification.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Describes the expected observable properties of an <see cref="AlphanumericCheckDigitAlgorithm" />
/// implementation for use in behavioural and known-answer tests.
/// </summary>
/// <remarks>
/// Instances are supplied by derived test classes via
/// <see cref="AlphanumericCheckDigitAlgorithmTests{TTest, TAlgorithm}.GetSpecification" /> and drive assertions
/// common across every algorithm sharing the alphanumeric base.
/// </remarks>
public sealed record AlphanumericCheckDigitAlgorithmSpecification
{
    /// <summary>
    /// Gets the expected <see cref="AlphanumericCheckDigitAlgorithm.AlgorithmName" /> string.
    /// </summary>
    /// <returns>The canonical algorithm name, e.g. <c>"ISBN-10"</c>.</returns>
    public required string AlgorithmName { get; init; }

    /// <summary>
    /// Gets the expected <see cref="AlphanumericCheckDigitAlgorithm.InputAlphabet" /> value.
    /// </summary>
    /// <returns>The declared input alphabet for the algorithm under test.</returns>
    public required CheckDigitInputAlphabet InputAlphabet { get; init; }

    /// <summary>
    /// Gets the expected <see cref="AlphanumericCheckDigitAlgorithm.OutputAlphabet" /> value.
    /// </summary>
    /// <returns>The declared output alphabet for the algorithm under test.</returns>
    public required CheckDigitOutputAlphabet OutputAlphabet { get; init; }

    /// <summary>
    /// Gets the check character expected for an empty body, or <see langword="null" /> to indicate that the
    /// algorithm throws on empty input.
    /// </summary>
    /// <returns>An ASCII character, or <see langword="null" />. Defaults to <c>'0'</c>.</returns>
    public char? EmptyCheckDigit { get; init; } = '0';

    /// <summary>
    /// Gets the known-answer vectors to exercise.
    /// </summary>
    /// <returns>A non-empty list of <see cref="CheckDigitKnownAnswer" /> entries.</returns>
    public required IReadOnlyList<CheckDigitKnownAnswer> KnownAnswers { get; init; }
}

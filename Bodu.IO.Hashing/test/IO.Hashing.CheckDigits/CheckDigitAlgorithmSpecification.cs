// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CheckDigitAlgorithmSpecification.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CheckDigitAlgorithmSpecification.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Describes the expected observable properties of a <see cref="CheckDigitAlgorithm" /> implementation for use in
/// behavioural and known-answer tests.
/// </summary>
/// <remarks>
/// Instances are supplied by derived test classes via
/// <see cref="CheckDigitAlgorithmTests{TTest, TAlgorithm}.GetSpecification" /> and drive assertions common across every
/// check-digit algorithm under test — algorithm-name exposure, empty-body behaviour, and known-answer evaluation.
/// </remarks>
public sealed record CheckDigitAlgorithmSpecification
{

    /// <summary>
    /// Gets the expected <see cref="CheckDigitAlgorithm.AlgorithmName" /> string.
    /// </summary>
    /// <returns>The canonical algorithm name, e.g. <c>"Luhn"</c>.</returns>
    public required string AlgorithmName { get; init; }

    /// <summary>
    /// Gets the check digit expected for an empty body.
    /// </summary>
    /// <returns>An ASCII character in the range <c>'0'</c> to <c>'9'</c>. Defaults to <c>'0'</c>.</returns>
    public char EmptyCheckDigit { get; init; } = '0';

    /// <summary>
    /// Gets the known-answer vectors to exercise.
    /// </summary>
    /// <returns>A non-empty list of <see cref="CheckDigitKnownAnswer" /> entries.</returns>
    public required IReadOnlyList<CheckDigitKnownAnswer> KnownAnswers { get; init; }

}

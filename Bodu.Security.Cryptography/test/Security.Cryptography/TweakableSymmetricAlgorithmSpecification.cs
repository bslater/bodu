// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TweakableSymmetricAlgorithmSpecification.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Extends <see cref="SymmetricAlgorithmSpecification" /> with tweak-related metadata required to drive
/// tweak-validation tests in <see cref="TweakableSymmetricAlgorithmTests{TTest, TAlgorithm}" />.
/// </summary>
public sealed record TweakableSymmetricAlgorithmSpecification
    : SymmetricAlgorithmSpecification
{
    /// <summary>
    /// Gets the expected default <see cref="TweakableSymmetricAlgorithm.TweakSize" /> in bits on a freshly constructed
    /// instance.
    /// </summary>
    public required int DefaultTweakSizeBits { get; init; }

    /// <summary>
    /// Gets the set of tweak sizes in bits to exercise in parameterised tests. For algorithms with a fixed tweak size,
    /// this should list each discrete legal size. The data sources in
    /// <see cref="TweakableSymmetricAlgorithmTests{TTest, TAlgorithm}" /> use this set to exclude legal values from
    /// invalid-size candidates.
    /// </summary>
    public required IReadOnlyList<int> LegalTweakSizesBits { get; init; }
}

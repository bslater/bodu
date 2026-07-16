// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CityHashTests{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Provides the abstract test infrastructure shared across all <see cref="CityHash{T}" /> implementations.
/// </summary>
/// <typeparam name="TTest">The concrete test class, satisfying the CRTP pattern required by the base.</typeparam>
/// <typeparam name="TAlgorithm">The concrete CityHash algorithm under test.</typeparam>
public abstract partial class CityHashTests<TTest, TAlgorithm>
    : NonCryptographicHashAlgorithmTests<TTest, TAlgorithm, SingleTestVariant>
    where TTest : CityHashTests<TTest, TAlgorithm>, new()
    where TAlgorithm : CityHash, new()
{

    /// <inheritdoc />
    protected override TAlgorithm CreateAlgorithm(SingleTestVariant variant) => new();

}

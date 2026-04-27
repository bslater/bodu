// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XxHashTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Provides the abstract test infrastructure shared across all <see cref="XxHash{T}" /> implementations.
/// </summary>
/// <typeparam name="TTest">The concrete test class, satisfying the CRTP pattern required by the base.</typeparam>
/// <typeparam name="TAlgorithm">The concrete xxHash algorithm under test.</typeparam>
public abstract partial class XxHashTests<TTest, TAlgorithm>
    : NonCryptographicHashAlgorithmTests<TTest, TAlgorithm, SingleTestVariant>
    where TTest : XxHashTests<TTest, TAlgorithm>, new()
    where TAlgorithm : XxHash<TAlgorithm>, new()
{
    /// <inheritdoc />
    protected override TAlgorithm CreateAlgorithm(SingleTestVariant variant) => new();
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CityHashTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the abstract test infrastructure shared across all <see cref="CityHash{T}" /> implementations.
/// </summary>
/// <typeparam name="TTest">The concrete test class, satisfying the CRTP pattern required by the base.</typeparam>
/// <typeparam name="TAlgorithm">The concrete CityHash algorithm under test.</typeparam>
public abstract partial class CityHashTests<TTest, TAlgorithm>
    : HashAlgorithmTests<TTest, TAlgorithm, SingleTestVariant>
    where TTest : HashAlgorithmTests<TTest, TAlgorithm, SingleTestVariant>, new()
    where TAlgorithm : CityHash<TAlgorithm>, new()
{
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FnvTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Provides a reusable abstract base class for verifying the correctness of <see cref="Fnv{T}" />
/// implementations (FNV-1 / FNV-1a × 32-bit / 64-bit).
/// </summary>
/// <typeparam name="TTest">The concrete test type inheriting this class.</typeparam>
/// <typeparam name="TAlgorithm">The FNV variant under test.</typeparam>
public abstract partial class FnvTests<TTest, TAlgorithm>
    : NonCryptographicHashAlgorithmTests<TTest, TAlgorithm, SingleTestVariant>
    where TTest : FnvTests<TTest, TAlgorithm>, new()
    where TAlgorithm : Fnv<TAlgorithm>, new()
{

    /// <inheritdoc />
    protected override TAlgorithm CreateAlgorithm(SingleTestVariant variant) => new();

}

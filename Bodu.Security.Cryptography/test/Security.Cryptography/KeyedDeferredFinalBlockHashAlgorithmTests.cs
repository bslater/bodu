// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeyedDeferredFinalBlockHashAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a reusable base class for verifying the correctness of
/// <see cref="KeyedDeferredFinalBlockHashAlgorithm{T}" /> implementations.
/// </summary>
/// <typeparam name="TTest">The concrete test type inheriting this class.</typeparam>
/// <typeparam name="TAlgorithm">
/// The keyed deferred-final-block hash algorithm under test. Must derive from
/// <see cref="KeyedDeferredFinalBlockHashAlgorithm{TAlgorithm}" /> and expose a public parameterless constructor.
/// </typeparam>
/// <typeparam name="TVariant">The enumeration type used to represent algorithm configuration variants.</typeparam>
/// <remarks>
/// Extends <see cref="HashAlgorithmTests{TTest, TAlgorithm, TVariant}" /> with test logic specific to keyed
/// algorithms that follow the BLAKE-family deferred-finalisation shape — key retention, defensive copying,
/// legal key length boundaries, keyed-vs-unkeyed digest divergence, and disposal semantics.
/// </remarks>
public abstract partial class KeyedDeferredFinalBlockHashAlgorithmTests<TTest, TAlgorithm, TVariant>
    : HashAlgorithmTests<TTest, TAlgorithm, TVariant>
    where TTest : KeyedDeferredFinalBlockHashAlgorithmTests<TTest, TAlgorithm, TVariant>, new()
    where TAlgorithm : KeyedDeferredFinalBlockHashAlgorithm<TAlgorithm>, new()
    where TVariant : struct, Enum
{
}

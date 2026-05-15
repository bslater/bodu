// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MurmurHash3Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Provides the abstract test infrastructure shared across all <see cref="MurmurHash3{T}" /> implementations.
/// </summary>
/// <typeparam name="TTest">The concrete test class, satisfying the CRTP pattern required by the base.</typeparam>
/// <typeparam name="TAlgorithm">The concrete MurmurHash3 algorithm under test.</typeparam>
public abstract partial class MurmurHash3Tests<TTest, TAlgorithm>
    : NonCryptographicHashAlgorithmTests<TTest, TAlgorithm, SingleTestVariant>
    where TTest : MurmurHash3Tests<TTest, TAlgorithm>, new()
    where TAlgorithm : MurmurHash3<TAlgorithm>, new()
{
    /// <inheritdoc />
    protected override TAlgorithm CreateAlgorithm(SingleTestVariant variant) => new();

    /// <inheritdoc />
    /// <remarks>
    /// <see cref="MurmurHash3{T}.Seed" /> is an immutable construction parameter exposed by a get-only
    /// auto-property and intentionally remains readable after disposal.
    /// </remarks>
    protected override IReadOnlyCollection<string> ExcludedReadablePropertyNames =>
        [nameof(MurmurHash3<TAlgorithm>.Seed)];
}

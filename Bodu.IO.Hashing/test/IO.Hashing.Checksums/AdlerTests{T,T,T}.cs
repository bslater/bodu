// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdlerTests{T,T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Provides a reusable abstract base class for verifying the correctness of <see cref="Adler{T}" />
/// implementations.
/// </summary>
/// <typeparam name="TTest">The concrete test type inheriting this class.</typeparam>
/// <typeparam name="TAlgorithm">The Adler variant under test.</typeparam>
/// <typeparam name="TModulo">The underlying accumulator numeric type.</typeparam>
public abstract partial class AdlerTests<TTest, TAlgorithm, TModulo>
    : NonCryptographicHashAlgorithmTests<TTest, TAlgorithm, SingleTestVariant>
    where TTest : AdlerTests<TTest, TAlgorithm, TModulo>, new()
    where TAlgorithm : Adler<TModulo>, new()
    where TModulo : unmanaged, INumber<TModulo>
{

    /// <inheritdoc />
    protected override TAlgorithm CreateAlgorithm(SingleTestVariant variant) => new();

}

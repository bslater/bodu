// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SnefruTests{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="Snefru{TAlgorithm}" /> hash algorithm.
/// </summary>
[TestClass]
public abstract partial class SnefruTests<TTest, TAlgorithm>
    : Security.Cryptography.BlockHashAlgorithmTests<TTest, TAlgorithm, SingleTestVariant>
    where TTest : SnefruTests<TTest, TAlgorithm>, new()
    where TAlgorithm : Snefru<TAlgorithm>, new()
{
    /// <summary>The single-byte ASCII input <c>"a"</c>, used as a Snefru-specific extension vector.</summary>
    protected static readonly byte[] SnefruLetterAInput = Encoding.UTF8.GetBytes("a");

    /// <summary>The 80-character ASCII input used as a Snefru-specific extension vector.</summary>
    protected static readonly byte[] SnefruRepeatedDigitsInput =
        Encoding.UTF8.GetBytes("12345678901234567890123456789012345678901234567890123456789012345678901234567890");

    public override IEnumerable<SingleTestVariant> GetHashAlgorithmVariants() =>
    [
        SingleTestVariant.Default
    ];
}

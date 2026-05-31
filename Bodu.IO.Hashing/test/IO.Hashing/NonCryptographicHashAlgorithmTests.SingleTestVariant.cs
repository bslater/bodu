// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmTests.SingleTestVariant.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Identifies the lone variant used by hash algorithms that do not expose configurable variants, satisfying the
/// <c>TVariant</c> type parameter of <see cref="NonCryptographicHashAlgorithmTests{TTest, TAlgorithm, TVariant}" />.
/// </summary>
public enum SingleTestVariant
{
    /// <summary>The default (and only) configuration of the algorithm under test.</summary>
    Default,
}

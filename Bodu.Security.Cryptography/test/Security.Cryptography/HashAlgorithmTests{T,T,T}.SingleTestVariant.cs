// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmTests{T,T,T}.SingleTestVariant.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Identifies the lone variant used by hash algorithms that do not expose configurable variants, satisfying
/// the <c>TVariant</c> type parameter of <see cref="HashAlgorithmTests{TTest, TAlgorithm, TVariant}" />.
/// </summary>
public enum SingleTestVariant
{
    Default
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmTests.SingleTestVariant.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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

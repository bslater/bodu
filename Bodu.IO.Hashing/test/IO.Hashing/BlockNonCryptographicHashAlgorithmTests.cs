// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockNonCryptographicHashAlgorithmTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Contains unit tests for <see cref="BlockNonCryptographicHashAlgorithm{T}" /> that exercise branches not
/// reachable through the production <see cref="Bodu.IO.Hashing.Checksums.Fletcher{TSelf}" /> derivatives —
/// specifically the <c>blockSize &lt;= 0</c> constructor guard, the <c>CopyResidualStateFrom(null)</c> guard,
/// and both padded-finalisation code paths governed by
/// <see cref="BlockNonCryptographicHashAlgorithm{T}" />'s <c>ShouldPadFinalBlock</c> and
/// <c>AllowUnalignedFinalBlock</c> virtual members.
/// </summary>
[TestClass]
public partial class BlockNonCryptographicHashAlgorithmTests
{
}

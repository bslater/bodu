// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleBlocksTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Specialized;

/// <summary>
/// Unit tests for <see cref="MerkleBlocks" />.
/// </summary>
/// <remarks>
/// The arithmetic is trivial and the tests are not, because getting the final block's length wrong does not fail
/// loudly — it silently produces a different root. The one-mebibyte cases mirror the leaf size the FallbackPlan
/// repository format uses, where an offset beyond <see cref="int.MaxValue" /> is reachable.
/// </remarks>
[TestClass]
public partial class MerkleBlocksTests
{
    /// <summary>One mebibyte — the leaf size used by the FallbackPlan repository format.</summary>
    private const int OneMebibyte = 1024 * 1024;
}

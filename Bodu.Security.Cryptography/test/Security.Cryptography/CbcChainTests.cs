// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CbcChainTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="CbcChain" />, the CBC chaining behind CBC encryption, CMAC, and CCM, grouped into member-named
/// partial files. Each path - a cipher's own chained implementation, and single blocks - is held to the platform's CBC.
/// </summary>
[TestClass]
public sealed partial class CbcChainTests
{
    /// <summary>
    /// Returns the block-aligned lengths the tests use: on both sides of the chained-call threshold and of a 4 KiB chunk,
    /// and several chunks.
    /// </summary>
    private static readonly int[] s_lengths = [16, 32, 80, 96, 112, 4080, 4096, 4112, 20000];
}

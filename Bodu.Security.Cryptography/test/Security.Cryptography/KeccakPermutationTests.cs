// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeccakPermutationTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="KeccakPermutation" />, grouped into member-named partial files. The FIPS 202 digests that pin
/// the permutation end to end live with <see cref="KeccakSpongeTests" /> and <see cref="ShakeTests" />; these tests
/// hold the permutation itself to <see cref="KeccakReference" />, the implementation it replaced.
/// </summary>
[TestClass]
public sealed partial class KeccakPermutationTests
{
}

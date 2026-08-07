// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LinkedKatTestClasses.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

// The BLAKE2 and BLAKE3 reference-vector suites are compiled into this assembly from the main test project
// (see the Compile Include entries in the csproj). Each is a partial of a test class whose [TestClass]
// attribute sits on a different partial - one that drags in the whole HashAlgorithmTests<,,> harness and the
// hundreds of files behind it. Redeclaring the attribute here makes the linked vectors discoverable without
// importing any of that: this assembly runs the published vectors, and nothing else.

/// <summary>
/// Hosts the linked BLAKE2b reference vectors so they run with the SIMD opt-out engaged.
/// </summary>
[TestClass]
public partial class Blake2bTests
{
}

/// <summary>
/// Hosts the linked BLAKE2s reference vectors so they run with the SIMD opt-out engaged.
/// </summary>
[TestClass]
public partial class Blake2sTests
{
}

/// <summary>
/// Hosts the linked BLAKE3 reference vectors so they run with the SIMD opt-out engaged.
/// </summary>
[TestClass]
public partial class Blake3Tests
{
}

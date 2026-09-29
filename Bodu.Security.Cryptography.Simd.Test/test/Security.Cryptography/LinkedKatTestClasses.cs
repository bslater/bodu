// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LinkedKatTestClasses.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

// The BLAKE2, BLAKE3, Argon2, ML-KEM and ML-DSA reference-vector suites are compiled into this assembly from the main test project
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

/// <summary>
/// Hosts the linked Argon2 vectors — RFC 9106's, the reference implementation's, and the corpus recorded from 1.0.0 —
/// so the scalar compression kernel is held to them with the SIMD opt-out engaged.
/// </summary>
[TestClass]
public partial class Argon2Tests
{
}

/// <summary>
/// Hosts the linked NIST ACVP ML-KEM-512 vectors so the scalar transforms and one-stream-at-a-time sampling are held to
/// them with the SIMD opt-out engaged.
/// </summary>
[TestClass]
public partial class MLKem512Tests
{
}

/// <summary>
/// Hosts the linked NIST ACVP ML-KEM-768 vectors so the scalar transforms and one-stream-at-a-time sampling are held to
/// them with the SIMD opt-out engaged.
/// </summary>
[TestClass]
public partial class MLKem768Tests
{
}

/// <summary>
/// Hosts the linked NIST ACVP ML-KEM-1024 vectors so the scalar transforms and one-stream-at-a-time sampling are held to
/// them with the SIMD opt-out engaged.
/// </summary>
[TestClass]
public partial class MLKem1024Tests
{
}

/// <summary>
/// Hosts the linked NIST ACVP ML-DSA-44 vectors so the scalar transforms and one-stream-at-a-time expansion are held to
/// them with the SIMD opt-out engaged.
/// </summary>
[TestClass]
public partial class MLDsa44Tests
{
}

/// <summary>
/// Hosts the linked NIST ACVP ML-DSA-65 vectors so the scalar transforms and one-stream-at-a-time expansion are held to
/// them with the SIMD opt-out engaged.
/// </summary>
[TestClass]
public partial class MLDsa65Tests
{
}

/// <summary>
/// Hosts the linked NIST ACVP ML-DSA-87 vectors so the scalar transforms and one-stream-at-a-time expansion are held to
/// them with the SIMD opt-out engaged.
/// </summary>
[TestClass]
public partial class MLDsa87Tests
{
}

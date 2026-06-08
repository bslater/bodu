// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2EncodedHash.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Represents the fields parsed from an Argon2 PHC encoded-hash string.
/// </summary>
/// <param name="Type">The Argon2 variant named by the string.</param>
/// <param name="Version">The version code (16 for 0x10, 19 for 0x13).</param>
/// <param name="Memory">The memory cost <c>m</c>, in kibibytes.</param>
/// <param name="Iterations">The iteration count <c>t</c>.</param>
/// <param name="Parallelism">The degree of parallelism <c>p</c>.</param>
/// <param name="Salt">The decoded salt.</param>
/// <param name="Hash">The decoded expected tag.</param>
/// <param name="Data">The decoded associated data, or an empty array when the string carries no <c>data</c> field.</param>
internal sealed record Argon2EncodedHash(
    Argon2Type Type,
    int Version,
    int Memory,
    int Iterations,
    int Parallelism,
    byte[] Salt,
    byte[] Hash,
    byte[] Data);

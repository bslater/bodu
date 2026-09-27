// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Validates the <see cref="Argon2d" />, <see cref="Argon2i" />, and <see cref="Argon2id" /> implementations: the
/// constructors, the instance and static derivation surfaces, the PHC encoded-hash round trip, and the known-answer
/// vectors. Tests are grouped into member-named partial files; the vectors live in the <c>KnownAnswers</c> partial.
/// </summary>
[TestClass]
public partial class Argon2Tests
{
    /// <summary>
    /// Creates the cheapest parameters that satisfy RFC 9106, for tests that exercise the surface rather than the cost.
    /// </summary>
    /// <returns>Parameters of 64 KiB, one pass, and one lane.</returns>
    private static Argon2Parameters FastParameters() =>
        new() { MemoryKiB = 64, Iterations = 1, Parallelism = 1 };

    /// <summary>
    /// Creates the cheapest parameters whose lanes the library divides among threads by default.
    /// </summary>
    /// <returns>Parameters of 4 MiB, one pass, and four lanes: segments of 256 blocks, the default threshold.</returns>
    private static Argon2Parameters ThreadedParameters() =>
        new() { MemoryKiB = 4096, Iterations = 1, Parallelism = 4 };

    /// <summary>
    /// Creates an instance of the named variant with a bound on the threads each derivation may use.
    /// </summary>
    /// <param name="variant">The variant: <c>"d"</c>, <c>"i"</c>, or <c>"id"</c>.</param>
    /// <param name="parameters">The cost and auxiliary parameters.</param>
    /// <param name="maxDegreeOfParallelism">The bound on the threads each derivation may use.</param>
    /// <returns>The instance.</returns>
    private static Argon2 Create(string variant, Argon2Parameters parameters, int maxDegreeOfParallelism) => variant switch
    {
        "d" => new Argon2d(parameters, maxDegreeOfParallelism),
        "i" => new Argon2i(parameters, maxDegreeOfParallelism),
        _ => new Argon2id(parameters, maxDegreeOfParallelism),
    };

    /// <summary>
    /// Creates a byte array of <paramref name="count" /> copies of <paramref name="value" />.
    /// </summary>
    /// <param name="value">The byte to repeat.</param>
    /// <param name="count">The number of bytes.</param>
    /// <returns>The filled array.</returns>
    private static byte[] Repeat(byte value, int count)
    {
        byte[] result = new byte[count];
        Array.Fill(result, value);
        return result;
    }
}

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

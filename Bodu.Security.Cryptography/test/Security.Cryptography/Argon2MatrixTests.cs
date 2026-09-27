// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2MatrixTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="Argon2Matrix" />, the native-memory owner of one derivation's matrix, grouped into member-named
/// partial files.
/// </summary>
[TestClass]
public sealed partial class Argon2MatrixTests
{
    /// <summary>
    /// Creates a pool private to one test, retaining up to four buffers of up to 1 MiB.
    /// </summary>
    /// <returns>The pool.</returns>
    private static Argon2MatrixPool CreatePool() =>
        new(4, 1024 * 1024, TimeSpan.FromHours(1), TimeProvider.System);
}

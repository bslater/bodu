// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdlerTests{T,T,T}.Adler32Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Provides a reusable abstract base class for verifying the correctness of <see cref="Adler32Base" />
/// implementations (Adler-32, Adler-32C).
/// </summary>
/// <typeparam name="TTest">The concrete test type inheriting this class.</typeparam>
/// <typeparam name="TAlgorithm">The 32-bit Adler variant under test.</typeparam>
public abstract partial class Adler32BaseTests<TTest, TAlgorithm>
    : AdlerTests<TTest, TAlgorithm, uint>
    where TTest : Adler32BaseTests<TTest, TAlgorithm>, new()
    where TAlgorithm : Adler32Base, new()
{
}

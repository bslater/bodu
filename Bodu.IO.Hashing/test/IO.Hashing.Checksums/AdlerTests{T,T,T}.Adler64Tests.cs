// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdlerTests{T,T,T}.Adler64Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Provides a reusable abstract base class for verifying the correctness of <see cref="Adler64Base" />
/// implementations (Adler-64).
/// </summary>
/// <typeparam name="TTest">The concrete test type inheriting this class.</typeparam>
/// <typeparam name="TAlgorithm">The 64-bit Adler variant under test.</typeparam>
public abstract partial class Adler64BaseTests<TTest, TAlgorithm>
    : AdlerTests<TTest, TAlgorithm, ulong>
    where TTest : Adler64BaseTests<TTest, TAlgorithm>, new()
    where TAlgorithm : Adler64Base, new()
{
}

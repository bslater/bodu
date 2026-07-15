// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GaloisField128Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="GaloisField128" /> field-multiplication helper, verifying that the
/// carry-less <see cref="GaloisField128.MultiplyClmul" /> path is bit-identical to the scalar reference
/// <see cref="GaloisField128.MultiplyScalar" /> that the existing GCM / GCM-SIV known-answer suites already pin to the
/// published specification vectors.
/// </summary>
[TestClass]
public sealed partial class GaloisField128Tests
{
}

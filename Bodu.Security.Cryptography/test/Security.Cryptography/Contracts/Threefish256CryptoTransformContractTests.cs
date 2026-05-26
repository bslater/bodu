// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Threefish256CryptoTransformContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Test.Contracts;

namespace Bodu.Security.Cryptography.Contracts;

/// <summary>
/// Drives <see cref="CryptoTransformContractTests{TTransform}" /> against the
/// <see cref="ICryptoTransform" /> produced by <see cref="Threefish256" />. Asserts the standard
/// transform-lifecycle invariants for a tweakable cipher's encryptor. Bespoke Threefish transform
/// tests (round-trip, AVX-512 path, tweak handling) live in <c>Threefish256TransformTests.*</c>.
/// </summary>
[TestClass]
public sealed class Threefish256CryptoTransformContractTests
    : CryptoTransformContractTests<ICryptoTransform>
{
    /// <inheritdoc />
    protected override ICryptoTransform CreateEncryptor()
    {
        Threefish256 algorithm = new();
        return algorithm.CreateEncryptor(new byte[32], new byte[32], new byte[16]);
    }

    /// <inheritdoc />
    protected override ICryptoTransform CreateDecryptor()
    {
        Threefish256 algorithm = new();
        return algorithm.CreateDecryptor(new byte[32], new byte[32], new byte[16]);
    }
}

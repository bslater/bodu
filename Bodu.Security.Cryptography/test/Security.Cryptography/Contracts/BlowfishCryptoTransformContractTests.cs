// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlowfishCryptoTransformContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Contracts;

/// <summary>
/// Drives <see cref="CryptoTransformContractTests{TTransform}" /> against the
/// <see cref="ICryptoTransform" /> produced by <see cref="Blowfish" />. Asserts the four standard
/// transform-lifecycle invariants (positive input / output block size, non-throwing zero-length
/// finalisation, idempotent disposal). Bespoke Blowfish transform tests live in
/// <c>BlowfishTransformTests.*</c>.
/// </summary>
[TestClass]
public sealed class BlowfishCryptoTransformContractTests
    : CryptoTransformContractTests<ICryptoTransform>
{
    private static readonly byte[] s_referenceKey = Convert.FromHexString("0123456789ABCDEF");

    private static readonly byte[] s_referenceIV = Convert.FromHexString("0000000000000000");

    /// <inheritdoc />
    protected override ICryptoTransform CreateEncryptor()
    {
        Blowfish algorithm = new();
        return algorithm.CreateEncryptor(s_referenceKey, s_referenceIV);
    }

    /// <inheritdoc />
    protected override ICryptoTransform CreateDecryptor()
    {
        Blowfish algorithm = new();
        return algorithm.CreateDecryptor(s_referenceKey, s_referenceIV);
    }
}

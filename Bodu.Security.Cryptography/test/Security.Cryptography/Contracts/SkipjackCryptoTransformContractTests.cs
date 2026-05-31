// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkipjackCryptoTransformContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Contracts;

/// <summary>
/// Drives <see cref="CryptoTransformContractTests{TTransform}" /> against the
/// <see cref="ICryptoTransform" /> produced by <see cref="Skipjack" /> in CBC mode with PKCS7 padding.
/// Asserts the four standard transform-lifecycle invariants: positive input / output block size,
/// non-throwing zero-length finalisation, and idempotent disposal. Bespoke Skipjack
/// transform tests (round-trip, mode/padding matrix) live in <c>SkipjackTransformTests.*</c>.
/// </summary>
[TestClass]
public sealed class SkipjackCryptoTransformContractTests
    : CryptoTransformContractTests<ICryptoTransform>
{
    private static readonly byte[] s_referenceKey = Convert.FromHexString("00998877665544332211");

    private static readonly byte[] s_referenceIV = Convert.FromHexString("0000000000000000");

    /// <inheritdoc />
    protected override ICryptoTransform CreateEncryptor()
    {
        Skipjack algorithm = new();
        return algorithm.CreateEncryptor(s_referenceKey, s_referenceIV);
    }

    /// <inheritdoc />
    protected override ICryptoTransform CreateDecryptor()
    {
        Skipjack algorithm = new();
        return algorithm.CreateDecryptor(s_referenceKey, s_referenceIV);
    }
}

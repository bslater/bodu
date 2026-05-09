// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Base class for testing <see cref="BlockCipherTransform" /> implementations. Extends
/// <see cref="CryptoTransformTests{TCryptoTransform}" /> with tests specific to block cipher transforms,
/// including null-argument validation, property invariants, and data-driven known-answer coverage at the
/// <see cref="System.Security.Cryptography.ICryptoTransform" /> layer.
/// </summary>
/// <typeparam name="TTest">The concrete test class, used to resolve per-row known-answer vectors for
/// <see cref="DynamicDataAttribute" /> sources via the standard <c>new TTest()</c> dispatch idiom.</typeparam>
/// <typeparam name="TCryptoTransform">The concrete block cipher transform type under test.</typeparam>
[TestClass]
public abstract partial class BlockCipherTransformTests<TTest, TCryptoTransform>
    : CryptoTransformTests<TCryptoTransform>
    where TTest : BlockCipherTransformTests<TTest, TCryptoTransform>, new()
    where TCryptoTransform : BlockCipherTransform
{
    /// <summary>
    /// Creates a fully-configured <see cref="SymmetricAlgorithm" /> for the cipher under test, with
    /// the supplied <paramref name="padding" /> mode, <see cref="CipherMode.CBC" /> selected as the
    /// block cipher mode, and a freshly-generated key and IV. Concrete subclasses construct their
    /// own algorithm type and configure the standard padding+mode pair so the hoisted padding and
    /// empty-input regression tests can drive every cipher in the library through the same
    /// <see cref="CryptoStream" /> contract.
    /// </summary>
    /// <param name="padding">The <see cref="PaddingMode" /> to apply to the algorithm.</param>
    /// <returns>An owned <see cref="SymmetricAlgorithm" /> instance ready to encrypt or decrypt.</returns>
    protected abstract SymmetricAlgorithm CreateSymmetricAlgorithm(PaddingMode padding);
}

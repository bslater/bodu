// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTransformTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Base class for testing <see cref="BlockCipherTransform" /> implementations. Extends
/// <see cref="CryptoTransformTests{TCryptoTransform}" /> with tests specific to block cipher transforms,
/// including null-argument validation, property invariants, and behavioural contract coverage at the
/// <see cref="System.Security.Cryptography.ICryptoTransform" /> layer.
/// </summary>
/// <typeparam name="TTest">The concrete test class, used to resolve specification data for
/// <see cref="DynamicDataAttribute" /> sources via the standard <c>new TTest()</c> dispatch idiom.</typeparam>
/// <typeparam name="TCryptoTransform">The concrete block cipher transform type under test.</typeparam>
/// <remarks>
/// Concrete subclasses override <see cref="CreateEncryptor" /> and <see cref="CreateDecryptor" /> to produce
/// a paired encryptor and decryptor that share the same key, IV, and (where applicable) tweak — so an
/// encrypt-then-decrypt round trip via these transforms recovers the original plaintext. Crypto-correctness
/// is validated at the block-cipher tier through <see cref="BlockCipherTests{TTest, TCipher, TVariant}" />;
/// the transform tier validates the public <see cref="System.Security.Cryptography.ICryptoTransform" /> surface
/// (argument validation, block-size invariants, padding, reusability, disposal) plus generic round-trip recovery.
/// </remarks>
[TestClass]
public abstract partial class BlockCipherTransformTests<TTest, TCryptoTransform>
    : CryptoTransformTests<TCryptoTransform>
    where TTest : BlockCipherTransformTests<TTest, TCryptoTransform>, new()
    where TCryptoTransform : BlockCipherTransform
{
    /// <summary>
    /// Creates a fully configured encryptor for round-trip and contract testing. The returned encryptor
    /// must share key, IV, and (where applicable) tweak with <see cref="CreateDecryptor" /> on the same
    /// test instance so encrypt-then-decrypt recovers the original plaintext.
    /// </summary>
    /// <returns>A new <typeparamref name="TCryptoTransform" /> configured to encrypt.</returns>
    protected abstract TCryptoTransform CreateEncryptor();

    /// <summary>
    /// Creates a fully configured decryptor paired with <see cref="CreateEncryptor" />. The returned
    /// decryptor must share key, IV, and (where applicable) tweak with the encryptor on the same test
    /// instance so encrypt-then-decrypt recovers the original plaintext.
    /// </summary>
    /// <returns>A new <typeparamref name="TCryptoTransform" /> configured to decrypt.</returns>
    protected abstract TCryptoTransform CreateDecryptor();
}

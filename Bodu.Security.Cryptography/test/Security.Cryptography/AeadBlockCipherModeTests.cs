// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AeadBlockCipherModeTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Base test class for <see cref="IAeadBlockCipherModeTransform" /> implementations, providing
/// constructor validation via <see cref="CipherModeTestsBase{TTransform}" /> together with
/// per-method tests partitioned across the following partial files:
/// <list type="bullet">
/// <item><description><c>AeadBlockCipherModeTests.Encrypt.cs</c> — <see cref="IAeadBlockCipherModeTransform.Encrypt" /> argument validation and output-size tests.</description></item>
/// <item><description><c>AeadBlockCipherModeTests.Decrypt.cs</c> — <see cref="IAeadBlockCipherModeTransform.Decrypt" /> argument validation, tamper detection, and round-trip tests.</description></item>
/// <item><description><c>AeadBlockCipherModeTests.ProcessAssociatedData.cs</c> — <see cref="IAeadBlockCipherModeTransform.ProcessAssociatedData" /> lifecycle tests.</description></item>
/// <item><description><c>AeadBlockCipherModeTests.TagSize.cs</c> — <see cref="IAeadBlockCipherModeTransform.TagSize" /> property tests.</description></item>
/// </list>
/// </summary>
/// <remarks>
/// <para>
/// The constructor-validation tests and shared mode metadata
/// (<see cref="CipherModeTestsBase{TTransform}.ExpectedBlockSize" />,
/// <see cref="CipherModeTestsBase{TTransform}.IvParameterName" />,
/// <see cref="CipherModeTestsBase{TTransform}.UsesInitializationVector" />, and the expected initialisation-value
/// size) live in <see cref="CipherModeTestsBase{TTransform}" /> and are inherited here.
/// </para>
/// <para>
/// AEAD modes use a 128-bit block cipher, but their public initialisation value is mode-specific. For example,
/// GCM accepts a 96-bit nonce rather than a 128-bit block-sized IV. Concrete tests should override the inherited
/// initialisation-value metadata where required.
/// </para>
/// <para>
/// Concrete test classes, such as <c>GcmModeTransformTests</c>, must live in their own files and inherit from
/// this class directly. They must not be embedded here.
/// </para>
/// </remarks>
/// <typeparam name="TTransform">The <see cref="IAeadBlockCipherModeTransform" /> type under test.</typeparam>
[TestClass]
public abstract partial class AeadBlockCipherModeTests<TTransform>
    : CipherModeTestsBase<TTransform>
    where TTransform : IAeadBlockCipherModeTransform
{
    /// <summary>
    /// AEAD modes such as GCM, CCM, and GCM-SIV target 128-bit block ciphers.
    /// </summary>
    protected override int ExpectedBlockSize => 16;

    /// <summary>
    /// Gets a value indicating whether changing the initialisation vector (nonce) under a fixed key
    /// changes the produced ciphertext and tag. Defaults to <see langword="true" /> for nonce-based
    /// AEADs (GCM, CCM, EAX, OCB, GCM-SIV).
    /// </summary>
    /// <remarks>
    /// SIV (RFC 5297, as implemented here) overrides this to <see langword="false" />: the supplied
    /// IV is ignored and the synthetic IV is derived deterministically from the key, AAD, and
    /// plaintext, so the nonce-variation tests do not apply.
    /// </remarks>
    protected virtual bool NonceAffectsCiphertext => true;

    /// <summary>
    /// Creates a zero-filled initialisation value using the expected size for the transform under test.
    /// </summary>
    /// <returns>
    /// A new initialisation value suitable for constructing the transform under test.
    /// </returns>
    /// <remarks>
    /// Most AEAD modes use a block-sized initialisation value. GCM overrides the expected size to 12 bytes because
    /// its public constructor accepts a 96-bit nonce.
    /// </remarks>
    protected virtual byte[] CreateInitializationVector() =>
        new byte[ExpectedInitializationVectorSize];

    /// <summary>
    /// Returns a transform constructed with the default test cipher, using a correctly-sized all-zero
    /// initialisation value for the transform under test.
    /// </summary>
    /// <returns>A new transform instance.</returns>
    protected TTransform MakeTransform() =>
        CreateTransform(
            new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA),
            CreateInitializationVector());

    /// <summary>
    /// Returns a fresh transform constructed with the default test cipher and the supplied
    /// initialisation value, allowing concrete tests to share state between encrypting and
    /// decrypting instances built atop the same monitoring cipher and IV.
    /// </summary>
    /// <param name="iv">The initialisation value to seed the transform with.</param>
    /// <returns>A new transform instance.</returns>
    private TTransform MakeTransform(byte[] iv) =>
        CreateTransform(
            new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA),
            iv);
}
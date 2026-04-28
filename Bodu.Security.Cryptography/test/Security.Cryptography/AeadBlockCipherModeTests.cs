// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AeadBlockCipherModeTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Base test class for <see cref="IAeadBlockCipherModeTransform" /> implementations, providing
/// constructor validation (via <see cref="CipherModeTestsBase{TTransform}" />) together with
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
/// The three constructor-validation tests (<c>Ctor_*</c>) and the shared properties
/// (<see cref="CipherModeTestsBase{TTransform}.ExpectedBlockSize" />,
/// <see cref="CipherModeTestsBase{TTransform}.IvParameterName" />,
/// <see cref="CipherModeTestsBase{TTransform}.UsesInitializationVector" />) live in
/// <see cref="CipherModeTestsBase{TTransform}" /> and are inherited here.
/// </para>
/// <para>
/// Concrete test classes (e.g. <c>GcmModeTransformTests</c>) must live in their own files and
/// inherit from this class directly — they must not be embedded here.
/// </para>
/// </remarks>
/// <typeparam name="TTransform">The <see cref="IAeadBlockCipherModeTransform" /> type under test.</typeparam>
[TestClass]
public abstract partial class AeadBlockCipherModeTests<TTransform>
    : CipherModeTestsBase<TTransform>
    where TTransform : IAeadBlockCipherModeTransform
{
    /// <summary>
    /// AEAD modes (GCM, CCM, GCM-SIV) target 128-bit (16-byte) blocks. This overrides the
    /// base-class default of 8 bytes used by the generic block-cipher mode tests.
    /// </summary>
    protected override int ExpectedBlockSize => 16;

    // IvParameterName       → inherited as "iv"   — correct for all current AEAD modes
    // UsesInitializationVector → inherited as true — all AEAD modes require an IV

    /// <summary>
    /// Returns a transform constructed with the default test cipher (<see cref="MonitoringBlockCipher" />
    /// with <c>xorMask=0xAA</c>) and an all-zero IV. Used by tests that do not need to inspect
    /// the cipher or IV directly.
    /// </summary>
    private TTransform MakeTransform() =>
        CreateTransform(
            new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA),
            new byte[ExpectedBlockSize]);
}

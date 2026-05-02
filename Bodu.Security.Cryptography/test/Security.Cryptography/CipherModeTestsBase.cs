// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CipherModeTestsBase.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Shared base for all block cipher mode transform test classes. Provides the constructor parameter
/// and factory surface common to both <see cref="BlockCipherModeTests{TMode}" /> and
/// <see cref="AeadBlockCipherModeTests{TTest, TTransform}" />, and owns the three constructor-validation
/// tests that apply to every mode regardless of whether it uses <c>Transform</c> or <c>Encrypt</c>/<c>Decrypt</c>.
/// </summary>
/// <remarks>
/// <para>
/// Concrete test classes never inherit directly from this type. The two intermediate base classes
/// (<see cref="BlockCipherModeTests{TMode}" /> and <see cref="AeadBlockCipherModeTests{TTest, TTransform}" />)
/// partition the modes by interface and add the tests specific to their respective API shapes.
/// </para>
/// <para>
/// The inheritance hierarchy is:
/// <code>
///   CipherModeTestsBase&lt;T&gt;
///   ├── BlockCipherModeTests&lt;TMode&gt;    — IBlockCipherModeTransform modes (ECB, CBC, CFB, OFB, CTR, XTS, OCB, EAX, SIV, CTS)
///   │   └── CbcModeTransformTests, CtrModeTransformTests, … (concrete)
///   └── AeadBlockCipherModeTests&lt;TTest, TTransform&gt; — IAeadBlockCipherModeTransform modes (GCM, CCM, GCM-SIV)
///       └── GcmModeTransformTests, CcmModeTransformTests, … (concrete)
/// </code>
/// </para>
/// </remarks>
/// <typeparam name="TTransform">The transform type under test.</typeparam>
[TestClass]
public abstract partial class CipherModeTestsBase<TTransform>
{
    // ── Overridable properties ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the block size in bytes used when constructing <see cref="MonitoringBlockCipher" />
    /// instances in tests. Defaults to 8 (64-bit), matching the <see cref="BlockCipherModeTests{TMode}" />
    /// baseline. <see cref="AeadBlockCipherModeTests{TTest, TTransform}" /> overrides this to 16 because
    /// GCM, CCM, and GCM-SIV are defined only for 128-bit blocks.
    /// </summary>
    protected virtual int ExpectedBlockSize => 8;

    /// <summary>
    /// Gets the name of the constructor parameter that receives the initialisation vector (or
    /// equivalent seed). Defaults to <c>"iv"</c>. Override when a specific mode uses a different
    /// name — for example CTR uses <c>"initialCounter"</c>.
    /// </summary>
    protected virtual string IvParameterName => "iv";

    /// <summary>
    /// Gets a value indicating whether this mode accepts a caller-supplied initialisation vector
    /// and validates its length against the cipher block size at construction time. ECB overrides
    /// this to <see langword="false" />; all other modes accept an IV.
    /// </summary>
    protected virtual bool UsesInitializationVector => true;

    // ── Abstract factory ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a fresh transform instance wrapping <paramref name="cipher" /> and seeded with
    /// <paramref name="iv" />. Implementations whose mode does not use an IV (ECB) may ignore
    /// <paramref name="iv" />.
    /// </summary>
    protected abstract TTransform CreateTransform(IBlockCipher cipher, byte[] iv);
}

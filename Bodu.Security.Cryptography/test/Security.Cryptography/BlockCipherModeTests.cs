// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherModeTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Base test class for <see cref="IBlockCipherModeTransform" /> implementations, providing
/// constructor validation (via <see cref="CipherModeTestsBase{TTransform}" />) together with
/// <c>Transform</c>-level argument, round-trip, and pattern tests.
/// </summary>
/// <remarks>
/// The constructor-validation tests (<c>Ctor_*</c>) and the shared properties
/// (<see cref="CipherModeTestsBase{TTransform}.ExpectedBlockSize" />,
/// <see cref="CipherModeTestsBase{TTransform}.IvParameterName" />,
/// <see cref="CipherModeTestsBase{TTransform}.UsesInitializationVector" />) live in
/// <see cref="CipherModeTestsBase{TTransform}" /> and are inherited here.
/// <c>BlockCipherModeTests.Ctors.cs</c> is therefore superseded and should be removed.
/// </remarks>
/// <typeparam name="TMode">The <see cref="IBlockCipherModeTransform" /> type under test.</typeparam>
[TestClass]
public abstract partial class BlockCipherModeTests<TMode>
    : CipherModeTestsBase<TMode>
    where TMode : IBlockCipherModeTransform
{
    // ExpectedBlockSize  → inherited as 8  (correct for generic block-cipher mode tests)
    // IvParameterName    → inherited as "iv" (CTR overrides to "initialCounter")
    // UsesInitializationVector → inherited as true (ECB overrides to false)

    /// <summary>
    /// Gets a value indicating whether this mode alters the relationship between plaintext and
    /// ciphertext across block positions (via chaining, feedback, or a counter). ECB returns
    /// <see langword="false" /> because identical plaintext blocks always yield identical
    /// ciphertext blocks.
    /// </summary>
    protected virtual bool UsesChaining => true;

    /// <summary>
    /// Gets a value indicating whether this mode transform guards against counter overflow or
    /// keystream reuse. Only CTR currently implements this; all other modes return
    /// <see langword="false" />.
    /// </summary>
    protected virtual bool GuardsAgainstKeystreamReuse => false;
}

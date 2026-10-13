// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IBlockCipherModeTransform.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Defines a stateful block cipher mode transformation that applies a chaining strategy (such as ECB, CBC, CFB, OFB, or
/// CTR) over a block cipher primitive.
/// </summary>
/// <remarks>
/// <para>
/// Implementations wrap an <see cref="IBlockCipher" /> and own any mode state required between calls, such as the
/// evolving initialization vector, feedback register, or counter. For the chaining and stream-shaped modes, successive
/// calls to <see cref="Transform" /> continue the stream from the state left by the previous call (a CTR call that ends
/// in a partial block discards the rest of that block's keystream). <see cref="EcbModeTransform" /> is stateless, and
/// <see cref="XtsModeTransform" /> restarts at the first block of its data unit on every call, so one XTS call must
/// cover a whole data unit.
/// </para>
/// <para>
/// Padding is not handled by this interface. ECB, CBC, CFB, OFB, and XTS accept only whole blocks, so callers align
/// their input to the cipher block size using an <see cref="IPaddingStrategy" /> before invoking
/// <see cref="Transform" />. <see cref="CtrModeTransform" /> accepts any length, and <see cref="CtsModeTransform" />
/// any length of at least one block.
/// </para>
/// <para>
/// <strong>API surface.</strong> The library ships several implementations clustered into three groups:
/// </para>
/// <list type="bullet">
/// <item>
/// <term>Classic confidentiality-only modes</term>
/// <description>
/// <see cref="EcbModeTransform" />, <see cref="CbcModeTransform" />, <see cref="CfbModeTransform" />,
/// <see cref="OfbModeTransform" />, <see cref="CtrModeTransform" />, <see cref="CtsModeTransform" />.
/// </description>
/// </item>
/// <item>
/// <term>Disk-encryption mode</term>
/// <description><see cref="XtsModeTransform" />.</description>
/// </item>
/// <item>
/// <term>Authenticated modes (AEAD)</term>
/// <description>
/// Implement the richer <see cref="IAeadBlockCipherModeTransform" /> contract instead - <see cref="GcmModeTransform" />
/// , <see cref="CcmModeTransform" />, <see cref="EaxModeTransform" />, <see cref="GcmSivModeTransform" />,
/// <see cref="OcbModeTransform" />, <see cref="SivModeTransform" />.
/// </description>
/// </item>
/// </list>
/// <para>
/// Most callers configure a mode through the <c>BlockMode</c> property of a block-cipher
/// <see cref="System.Security.Cryptography.SymmetricAlgorithm" /> wrapper (a <see cref="CipherModeKind" /> value, which
/// the wrapper dispatches through <see cref="BlockCipherModeFactory" /> when <c>CreateEncryptor</c> or
/// <c>CreateDecryptor</c> is called) rather than constructing this interface directly. That route covers ECB, CBC, CFB,
/// OFB, and CTR; <see cref="CtsModeTransform" /> and <see cref="XtsModeTransform" /> exist only as direct transforms.
/// Direct use is also appropriate when wiring a custom block cipher into the existing mode infrastructure or building a
/// higher-level construction on top.
/// </para>
/// <para>
/// Implementations are stateful and not thread-safe; share behind explicit synchronization, or allocate one per
/// consumer. Most modes reset cleanly when constructed afresh - there is no in-place reset method on this interface.
/// </para>
/// </remarks>
public interface IBlockCipherModeTransform
    : System.IDisposable
{
    /// <summary>
    /// Transforms <paramref name="input" /> under the mode's chaining strategy and writes the result to
    /// <paramref name="output" />.
    /// </summary>
    /// <param name="input">The input data to transform.</param>
    /// <param name="output">
    /// The destination span. Its length must be greater than or equal to the length of <paramref name="input" />.
    /// </param>
    /// <param name="encrypt"><see langword="true" /> to encrypt the input; <see langword="false" /> to decrypt.</param>
    /// <returns>The number of bytes written to <paramref name="output" />.</returns>
    /// <remarks>
    /// The length rule depends on the mode: ECB, CBC, CFB, OFB, and XTS accept a whole number of blocks, zero included;
    /// <see cref="CtrModeTransform" /> accepts any length; <see cref="CtsModeTransform" /> accepts any length of at
    /// least one block.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="output" /> is shorter than <paramref name="input" />, or, for
    /// <see cref="CtsModeTransform" />, when <paramref name="input" /> is shorter than one block.
    /// </exception>
    /// <exception cref="System.Security.Cryptography.CryptographicException">
    /// Thrown when the mode accepts only whole blocks and the length of <paramref name="input" /> is not a multiple of
    /// the block size, or when <paramref name="input" /> and <paramref name="output" /> partially overlap.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when the transform has been disposed.</exception>
    int Transform(ReadOnlySpan<byte> input, Span<byte> output, bool encrypt);
}

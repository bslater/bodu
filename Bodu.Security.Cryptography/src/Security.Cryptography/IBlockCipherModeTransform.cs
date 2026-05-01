// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IBlockCipherModeTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Defines a stateful block cipher mode transformation that applies a chaining strategy (such as ECB, CBC, CFB, OFB, or CTR) over a
/// block cipher primitive.
/// </summary>
/// <remarks>
/// <para>
/// Implementations wrap an <see cref="IBlockCipher" /> and own any mode state required between calls, such as the evolving
/// initialisation vector, feedback register, or counter. Successive calls to <see cref="Transform" /> continue the stream from the
/// state left by the previous call.
/// </para>
/// <para>
/// Padding is not handled by this interface. Callers must align input to the cipher block size using an
/// <see cref="IPaddingStrategy" /> before invoking <see cref="Transform" />.
/// </para>
/// <para>
/// <strong>API surface.</strong> The library ships several implementations clustered into three groups:
/// </para>
/// <list type="bullet">
///   <item>
///     <term>Classic confidentiality-only modes</term>
///     <description><see cref="EcbModeTransform"/>, <see cref="CbcModeTransform"/>, <see cref="CfbModeTransform"/>,
///     <see cref="OfbModeTransform"/>, <see cref="CtrModeTransform"/>, <see cref="CtsModeTransform"/>.</description>
///   </item>
///   <item>
///     <term>Disk-encryption mode</term>
///     <description><see cref="XtsModeTransform"/>.</description>
///   </item>
///   <item>
///     <term>Authenticated modes (AEAD)</term>
///     <description>Implement the richer <see cref="IAeadBlockCipherModeTransform"/> contract instead —
///     <see cref="GcmModeTransform"/>, <see cref="CcmModeTransform"/>, <see cref="EaxModeTransform"/>,
///     <see cref="GcmSivModeTransform"/>, <see cref="OcbModeTransform"/>, <see cref="SivModeTransform"/>.</description>
///   </item>
/// </list>
/// <para>
/// Most callers should configure modes through <see cref="System.Security.Cryptography.SymmetricAlgorithm.Mode"/>
/// (which accepts a <see cref="CipherBlockMode"/> and dispatches via <see cref="BlockCipherModeFactory"/>) rather
/// than constructing this interface directly. Direct use is appropriate when wiring a custom block cipher into the
/// existing mode infrastructure or building a higher-level construction on top.
/// </para>
/// <para>
/// Implementations are stateful and not thread-safe; share behind explicit synchronisation, or allocate one per
/// consumer. Most modes reset cleanly when constructed afresh — there is no in-place reset method on this interface.
/// </para>
/// </remarks>
public interface IBlockCipherModeTransform
{
    /// <summary>
    /// Transforms <paramref name="input" /> under the mode's chaining strategy and writes the result to <paramref name="output" />.
    /// </summary>
    /// <param name="input">The input data to transform. Its length must be a positive multiple of the underlying cipher block size.</param>
    /// <param name="output">The destination span. Its length must be greater than or equal to the length of <paramref name="input" />.</param>
    /// <param name="encrypt"><see langword="true" /> to encrypt the input; <see langword="false" /> to decrypt.</param>
    /// <returns>The number of bytes written to <paramref name="output" />.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="input" />'s length is not a multiple of the block size or when <paramref name="output" /> is too small.
    /// </exception>
    int Transform(ReadOnlySpan<byte> input, Span<byte> output, bool encrypt);
}

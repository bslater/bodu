// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ICryptoTransformExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions;

/// <summary>
/// Extends <see cref="ICryptoTransform" /> with one-shot, span/memory-aware, stream-to-stream, and async transform
/// helpers — sized buffers, finalization, and stream pumping rolled into single calls.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ICryptoTransform" /> is the contract that every block-cipher encryptor or decryptor implements, but its
/// raw API forces callers to size output buffers, allocate result arrays, distinguish intermediate
/// <c>TransformBlock</c> calls from the final <c>TransformFinalBlock</c>, and reset the transform between messages.
/// This class wraps those operations behind two verb-led names — <c>Transform</c> for whole-input processing and
/// <c>TransformFinalBlock</c> for finalization — that hide the buffering arithmetic and give the caller back a
/// correctly sized result.
/// </para>
/// <para>
/// The API surface clusters into four groups:
/// </para>
/// <list type="bullet">
/// <item>
/// <term>One-shot in-memory</term>
/// <description>
/// <c>Transform</c> overloads that accept a <see cref="byte" /> array, an array slice (<c>offset</c>/<c>count</c>), a
/// <see cref="System.ReadOnlySpan{T}" />, or a <see cref="System.ReadOnlyMemory{T}" /> and return a freshly allocated
/// output array. Use these when the entire input fits in memory.
/// </description>
/// </item>
/// <item>
/// <term>Caller-supplied buffers</term>
/// <description>
/// <c>Transform(input, destination)</c> overloads for span and memory destinations — for hot paths that want to reuse a
/// pre-allocated output buffer and have <c>destination</c> written in place. The return value is the number of bytes
/// written.
/// </description>
/// </item>
/// <item>
/// <term>Stream pumping</term>
/// <description>
/// <c>Transform(sourceStream, targetStream, bufferSize)</c> and the awaitable <c>TransformAsync</c> overloads, for
/// cases where the input is a stream that should not be fully buffered. The extension drives the read/transform/write
/// loop with a tunable buffer.
/// </description>
/// </item>
/// <item>
/// <term>Finalization</term>
/// <description>
/// <c>TransformFinalBlock</c> overloads that mirror the same input shapes (no-arg, byte array, slice, span, memory) for
/// emitting the final, padded block.
/// </description>
/// </item>
/// </list>
/// <para>
/// <see cref="ICryptoTransform" /> instances are stateful and single-use within a message — once
/// <see cref="ICryptoTransform.TransformFinalBlock(byte[], int, int)" /> has run, the transform should be disposed and
/// a fresh one created for the next message. None of these helpers dispose the transform or the supplied streams; the
/// caller owns the lifetime. Methods that allocate a result array always return exactly the number of bytes produced.
/// Async overloads honor <see cref="System.Threading.CancellationToken" /> at every read boundary.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography.Extensions;
///
/// using Aes aes = Aes.Create();
/// aes.Key = key;
/// aes.IV = iv;
///
/// // 1. One-shot encrypt of a span into a new byte[].
/// using ICryptoTransform encryptor = aes.CreateEncryptor();
/// byte[] ciphertext = encryptor.Transform(plaintext.AsSpan());
///
/// // 2. Stream-encrypt a large file end to end with a 64 KiB buffer.
/// using FileStream src = File.OpenRead("plain.bin");
/// using FileStream dst = File.Create("cipher.bin");
/// using ICryptoTransform e2 = aes.CreateEncryptor();
/// int written = e2.Transform(src, dst, bufferSize: 64 * 1024);
///
/// // 3. Cancellable async stream transform, e.g. when piping HTTP content.
/// using ICryptoTransform e3 = aes.CreateEncryptor();
/// await e3.TransformAsync(networkStream, output, bufferSize: 16 * 1024, cancellationToken);
///]]>
/// </code>
/// </example>
/// </remarks>
public static partial class ICryptoTransformExtensions
{
}

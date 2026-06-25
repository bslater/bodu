// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringEncodingExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text;

/// <summary>
/// Provides encoding extension methods on <see cref="string" /> that mirror the span-receiver surface in
/// <see cref="EncodingExtensions" />.
/// </summary>
/// <remarks>
/// <para>
/// Members are split across partial files following the repository convention (see <c>CLAUDE.md</c>): one partial file
/// per method, named <c>StringEncodingExtensions.&lt;MethodName&gt;.cs</c>. Tests follow the same pattern in
/// <c>test/Text.Encoding/StringEncodingExtensionsTests.&lt;MethodName&gt;.cs</c>.
/// </para>
/// <para>
/// All overloads delegate to the canonical span- or encoding-receiver implementations in
/// <see cref="EncodingExtensions" /> so behaviour and performance match the rest of the encoding surface.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Fluent encoding of a string variable.
/// byte[] utf8       = "héllo".ToUtf8Bytes();
/// byte[] withBom    = "héllo".ToBytesWithPreamble(new System.Text.UTF8Encoding(true));
///
/// // Pool-backed encoding for a network send.
/// using PooledBufferBuilder<byte> pooled = jsonPayload.GetUtf8BytesPooled();
/// await socket.SendAsync(pooled.WrittenMemory, SocketFlags.None);
///
/// // Pipeline integration.
/// "key=value\n".WriteUtf8To(pipeWriter);
///]]>
/// </code>
/// </example>
public static partial class StringEncodingExtensions
{
}

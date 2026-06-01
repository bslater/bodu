// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Provides whole-payload read and write helpers for <see cref="System.IO.Stream" /> — synchronous and asynchronous
/// operations that consume or emit an entire byte buffer in a single call.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="System.IO.Stream" /> exposes only incremental, partial-progress primitives: a single
/// <see cref="System.IO.Stream.Read(byte[], int, int)" /> call may return fewer bytes than requested, so reading a
/// stream to its end correctly requires a loop. These helpers encapsulate that loop together with the seekable and
/// non-seekable buffering distinction, so callers can treat a stream as a single byte payload.
/// </para>
/// <para>
/// When the stream is seekable the exact remaining length is used to allocate a single right-sized buffer; when it is
/// not, the content is accumulated through a growable buffer. Every helper reads from or writes at the current position
/// and leaves the stream open.
/// </para>
/// </remarks>
public static partial class StreamExtensions
{
}

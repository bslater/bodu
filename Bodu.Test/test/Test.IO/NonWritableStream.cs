// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonWritableStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.IO;

/// <summary>
/// A <see cref="MemoryStream" /> wrapper that reports <see cref="CanWrite" /> as <see langword="false" /> while
/// otherwise behaving exactly as the base stream.
/// </summary>
/// <remarks>
/// <para>
/// Only <see cref="CanWrite" /> is overridden, so a consumer's "is this stream writable?" precondition fails
/// deterministically without changing any other observable stream behavior. Use it to verify that write-accepting entry
/// points reject a non-writable destination.
/// </para>
/// <para>
/// This class is intended exclusively for test harness use and must not appear in production code.
/// </para>
/// </remarks>
public sealed class NonWritableStream
    : System.IO.MemoryStream
{
    /// <inheritdoc />
    /// <returns>Always <see langword="false" />.</returns>
    public override bool CanWrite => false;
}

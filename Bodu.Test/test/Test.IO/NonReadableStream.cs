// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonReadableStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.IO;

/// <summary>
/// A <see cref="MemoryStream" /> wrapper that reports <see cref="CanRead" /> as <see langword="false" /> while
/// otherwise behaving exactly as the base stream.
/// </summary>
/// <remarks>
/// <para>
/// Only <see cref="CanRead" /> is overridden, so a consumer's "is this stream readable?" precondition fails
/// deterministically without changing any other observable stream behavior. Use it to verify that read-accepting entry
/// points reject a non-readable source.
/// </para>
/// <para>
/// This class is intended exclusively for test harness use and must not appear in production code.
/// </para>
/// </remarks>
public sealed class NonReadableStream
    : System.IO.MemoryStream
{
    /// <inheritdoc />
    /// <returns>Always <see langword="false" />.</returns>
    public override bool CanRead => false;
}

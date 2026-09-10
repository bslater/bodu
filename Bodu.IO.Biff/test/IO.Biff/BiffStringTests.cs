// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffStringTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Tests for <see cref="BiffString" />: the span-backed view over BIFF8 Unicode and BIFF5 byte strings. Member-specific
/// tests live in the sibling partial files.
/// </summary>
[TestClass]
public sealed partial class BiffStringTests
{
    /// <summary>
    /// Reads a BIFF8 Unicode string from the start of the supplied bytes.
    /// </summary>
    /// <param name="bytes">The encoded string.</param>
    /// <param name="wideLength">Whether the length prefix is 16 bits.</param>
    /// <returns>The view.</returns>
    private static BiffString Unicode(byte[] bytes, bool wideLength = true) =>
        BiffString.ReadUnicode(bytes, 0, wideLength, BiffRecordType.Label);

    /// <summary>
    /// Reads a BIFF5 byte string from the start of the supplied bytes.
    /// </summary>
    /// <param name="bytes">The encoded string.</param>
    /// <param name="codePage">The code page.</param>
    /// <param name="wideLength">Whether the length prefix is 16 bits.</param>
    /// <returns>The view.</returns>
    private static BiffString Bytes(byte[] bytes, int codePage = 1252, bool wideLength = true) =>
        BiffString.ReadByteString(bytes, 0, wideLength, codePage, BiffRecordType.Label);
}

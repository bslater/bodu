// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffWriterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.IO.Biff;

/// <summary>
/// Tests for <see cref="BiffWriter" />: raw and typed record emission under both versions, the per-version limits,
/// and the shared-string-table continuation rules. Member-specific tests live in the sibling partial files.
/// </summary>
[TestClass]
public sealed partial class BiffWriterTests
{
    /// <summary>
    /// Runs a writer callback against a fresh BIFF8 writer and returns the emitted bytes.
    /// </summary>
    /// <param name="write">The callback that writes records.</param>
    /// <returns>The emitted bytes.</returns>
    private static byte[] Emit8(WriteAction write) =>
        Emit(BiffVersion.Biff8, write);

    /// <summary>
    /// Runs a writer callback against a fresh BIFF5 writer and returns the emitted bytes.
    /// </summary>
    /// <param name="write">The callback that writes records.</param>
    /// <returns>The emitted bytes.</returns>
    private static byte[] Emit5(WriteAction write) =>
        Emit(BiffVersion.Biff5, write);

    /// <summary>
    /// Runs a writer callback against a fresh writer of the given version and returns the emitted bytes.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <param name="write">The callback that writes records.</param>
    /// <returns>The emitted bytes.</returns>
    private static byte[] Emit(BiffVersion version, WriteAction write)
    {
        var output = new ArrayBufferWriter<byte>();
        var writer = new BiffWriter(output, version);
        write(ref writer);
        return output.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Reads the single record from an emitted stream, asserting it is the only one.
    /// </summary>
    /// <param name="bytes">The emitted bytes.</param>
    /// <param name="version">The version to seed the reader with.</param>
    /// <returns>The reader positioned on the record.</returns>
    private static BiffReader Single(byte[] bytes, BiffVersion version = BiffVersion.Biff8)
    {
        var reader = new BiffReader(bytes, new BiffReaderOptions { Version = version });
        Assert.IsTrue(reader.Read());
        var probe = new BiffReader(bytes.AsSpan(reader.BytesConsumed), new BiffReaderOptions { Version = version });
        Assert.IsFalse(probe.Read(), "More than one record was emitted.");
        return reader;
    }

    /// <summary>
    /// A callback that writes records through a writer passed by reference.
    /// </summary>
    /// <param name="writer">The writer.</param>
    private delegate void WriteAction(ref BiffWriter writer);
}

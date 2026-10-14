// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailStoreTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;

namespace Bodu.Formats.Outlook;

/// <summary>
/// Verifies the behavior of <see cref="OutlookMailStore" />, the mail-store session, with shared suppliers for the
/// member partials.
/// </summary>
[TestClass]
public partial class OutlookMailStoreTests
{
    /// <summary>The primary Unicode reference fixture.</summary>
    internal const string Sample1 = "unicode/sample1.pst";

    /// <summary>The ANSI corpus store with one folder and one message.</summary>
    internal const string Sample2Ansi = "ansi/sample2.pst";

    /// <summary>The ANSI corpus store with one empty folder.</summary>
    internal const string TestAnsi = "ansi/test_ansi.pst";

    /// <summary>
    /// Opens the primary Unicode reference fixture as a mail store.
    /// </summary>
    /// <param name="validationLevel">The validation level to open with.</param>
    /// <returns>The open session.</returns>
    internal static OutlookMailStore OpenSample1(PstValidationLevel validationLevel = PstValidationLevel.Compatible) =>
        OutlookMailStore.Open(
            PstReferenceFixtures.OpenStream(Sample1),
            new OutlookMailStoreReaderOptions { ValidationLevel = validationLevel });

    /// <summary>
    /// Builds a store from a generator pipeline and inverts the heap signature of one of its nodes, so that reading that
    /// node's heap fails in the container at every validation level.
    /// </summary>
    /// <param name="input">The generator pipeline, <c>gen:</c> included, in the release-note catalog's notation.</param>
    /// <param name="nodePath">
    /// The node whose heap is corrupted: a node identifier, followed by the identifiers of the subnodes to descend
    /// through.
    /// </param>
    /// <returns>The corrupted container bytes.</returns>
    internal static byte[] GenerateWithCorruptHeap(string input, params uint[] nodePath)
    {
        byte[] file = PstReleaseNoteCorpusTests.GenerateInput(input);

        // bSig, the third byte of the heap header, must be 0xEC.
        return InvertByte(file, ReadNodePayload(file, nodePath), index: 2);
    }

    /// <summary>
    /// Reads a node's data payload, or that of a subnode beneath it, through the container.
    /// </summary>
    /// <param name="file">The container bytes.</param>
    /// <param name="nodePath">
    /// A node identifier, followed by the identifiers of the subnodes to descend through.
    /// </param>
    /// <returns>The payload bytes.</returns>
    internal static byte[] ReadNodePayload(byte[] file, params uint[] nodePath)
    {
        using PstFile container = PstFile.Open(new MemoryStream(file, writable: false), new PstFileOptions());

        PstNode node = container.GetNode(new PstNodeId(nodePath[0]));
        foreach (uint subnodeId in nodePath.AsSpan(1))
        {
            Assert.IsTrue(node.TryGetSubnode(new PstNodeId(subnodeId), out PstNode? subnode), $"Subnode 0x{subnodeId:X8} must exist.");
            node = subnode;
        }

        return node.ReadAllBytes();
    }

    /// <summary>
    /// Returns a copy of a container with one byte of a run of its bytes inverted.
    /// </summary>
    /// <param name="file">The container bytes.</param>
    /// <param name="run">A run of bytes that occurs exactly once in <paramref name="file" />.</param>
    /// <param name="index">The offset within <paramref name="run" /> of the byte to invert.</param>
    /// <returns>The corrupted copy.</returns>
    internal static byte[] InvertByte(byte[] file, ReadOnlySpan<byte> run, int index)
    {
        int offset = file.AsSpan().IndexOf(run);
        Assert.IsTrue(offset >= 0, "The run must occur in the file.");
        Assert.AreEqual(-1, file.AsSpan(offset + 1).IndexOf(run), "The run must occur once.");

        var corrupted = (byte[])file.Clone();
        corrupted[offset + index] ^= 0xFF;

        return corrupted;
    }

    /// <summary>
    /// Walks a folder subtree depth-first, yielding every folder including the root of the walk.
    /// </summary>
    /// <param name="folder">The folder to walk from.</param>
    /// <returns>The folders, parents before children.</returns>
    internal static IEnumerable<OutlookMailFolder> Walk(OutlookMailFolder folder)
    {
        yield return folder;

        foreach (OutlookMailFolder child in folder.EnumerateSubfolders())
        {
            foreach (OutlookMailFolder descendant in Walk(child))
                yield return descendant;
        }
    }
}

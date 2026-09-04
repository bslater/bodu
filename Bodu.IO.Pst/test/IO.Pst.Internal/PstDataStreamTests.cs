// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstDataStreamTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Tests for <see cref="PstDataStream" />, reached through <see cref="PstNode.OpenDataStream" />.
/// </summary>
[TestClass]
public partial class PstDataStreamTests
{
    /// <summary>The node identifier of the streamed node.</summary>
    private const uint NodeId = 0x21;

    /// <summary>
    /// Opens a stream over a node whose payload spans several leaf blocks.
    /// </summary>
    /// <param name="file">When this method returns, the owning session.</param>
    /// <param name="leaves">The leaf payloads, in order.</param>
    /// <returns>The data stream.</returns>
    private static Stream OpenStream(out PstFile file, params byte[][] leaves)
    {
        var builder = new PstFixtureBuilder();
        ulong[] ids = [.. leaves.Select(builder.AddDataBlock)];
        uint total = (uint)leaves.Sum(static l => l.Length);
        builder.AddNode(NodeId, ids.Length == 1 ? ids[0] : builder.AddXBlock(total, ids));

        file = PstFile.Open(builder.BuildStream(), new PstFileOptions());
        return file.GetNode(new PstNodeId(NodeId)).OpenDataStream();
    }

    /// <summary>
    /// Creates a payload whose bytes follow a seeded pattern.
    /// </summary>
    /// <param name="length">The payload length.</param>
    /// <param name="seed">The pattern seed.</param>
    /// <returns>The payload.</returns>
    private static byte[] Payload(int length, int seed) =>
        [.. Enumerable.Range(0, length).Select(i => (byte)((i * 17) + seed))];
}

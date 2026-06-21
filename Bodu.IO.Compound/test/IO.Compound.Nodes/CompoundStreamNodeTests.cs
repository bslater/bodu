// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStreamNodeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.IO.Compound.Nodes;

/// <summary>
/// Verifies the behavior of <see cref="CompoundStreamNode" />.
/// </summary>
[TestClass]
public class CompoundStreamNodeTests
{
    /// <summary>
    /// Verifies that creating a stream from text encodes the payload and reports its length.
    /// </summary>
    [TestMethod]
    public void Create_WhenFromText_ShouldEncodePayload()
    {
        CompoundStreamNode stream = CompoundStreamNode.Create("Data", "hello", Encoding.ASCII);

        CollectionAssert.AreEqual(Encoding.ASCII.GetBytes("hello"), stream.Content.ToArray());
        Assert.AreEqual(5, stream.Length);
    }

    /// <summary>
    /// Verifies that creating a stream from a source stream reads the entire payload.
    /// </summary>
    [TestMethod]
    public void Create_WhenFromStream_ShouldReadAllBytes()
    {
        using MemoryStream source = new(new byte[] { 9, 8, 7, 6 });

        CompoundStreamNode stream = CompoundStreamNode.Create("Data", source);

        CollectionAssert.AreEqual(new byte[] { 9, 8, 7, 6 }, stream.Content.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStreamNode.SetContent(ReadOnlySpan{byte})" /> replaces the payload.
    /// </summary>
    [TestMethod]
    public void SetContent_WhenCalled_ShouldReplacePayload()
    {
        CompoundStreamNode stream = CompoundStreamNode.Create("Data", new byte[] { 1 });

        stream.SetContent(new byte[] { 2, 3 });

        CollectionAssert.AreEqual(new byte[] { 2, 3 }, stream.Content.ToArray());
    }

    /// <summary>
    /// Verifies that a deferred node reports its declared length without opening its source.
    /// </summary>
    [TestMethod]
    public void Create_WhenDeferred_ShouldReportLengthWithoutOpening()
    {
        int opens = 0;
        CompoundStreamNode node = CompoundStreamNode.Create("Data", () =>
        {
            opens++;
            return new MemoryStream(new byte[] { 1, 2, 3 });
        }, 3);

        Assert.AreEqual(3, node.Length);
        Assert.AreEqual(0, opens);
    }

    /// <summary>
    /// Verifies that reading <see cref="CompoundStreamNode.Content" /> materializes a deferred node's source.
    /// </summary>
    [TestMethod]
    public void Content_WhenDeferred_ShouldMaterializeSource()
    {
        CompoundStreamNode node = CompoundStreamNode.Create("Data", () => new MemoryStream(new byte[] { 4, 5, 6 }), 3);

        CollectionAssert.AreEqual(new byte[] { 4, 5, 6 }, node.Content.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStreamNode.DeepClone" /> on a deferred node preserves the deferral.
    /// </summary>
    [TestMethod]
    public void DeepClone_WhenDeferred_ShouldRemainDeferred()
    {
        int opens = 0;
        CompoundStreamNode node = CompoundStreamNode.Create("Data", () =>
        {
            opens++;
            return new MemoryStream(new byte[] { 7, 8 });
        }, 2);

        var clone = (CompoundStreamNode)node.DeepClone();

        Assert.AreEqual(2, clone.Length);
        Assert.AreEqual(0, opens);
        CollectionAssert.AreEqual(new byte[] { 7, 8 }, clone.Content.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStreamNode.DeepClone" /> copies the payload and metadata into an independent,
    /// detached node.
    /// </summary>
    [TestMethod]
    public void DeepClone_WhenCalled_ShouldCopyContentAndMetadata()
    {
        CompoundStreamNode stream = CompoundStreamNode.Create("Data", new byte[] { 1, 2 });
        stream.ClassId = Guid.NewGuid();
        stream.StateBits = 7;

        var clone = (CompoundStreamNode)stream.DeepClone();

        Assert.AreNotSame(stream, clone);
        Assert.IsNull(clone.Parent);
        Assert.AreEqual(stream.ClassId, clone.ClassId);
        Assert.AreEqual(7, clone.StateBits);
        CollectionAssert.AreEqual(stream.Content.ToArray(), clone.Content.ToArray());
    }
}

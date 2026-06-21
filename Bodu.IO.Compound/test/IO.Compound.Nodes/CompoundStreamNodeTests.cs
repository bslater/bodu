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

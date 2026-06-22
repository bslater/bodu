// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileTests.Parent.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

public partial class CompoundFileTests
{
    /// <summary>
    /// Verifies that the root storage reports a <see langword="null" /> parent.
    /// </summary>
    [TestMethod]
    public void Parent_WhenRootStorage_ShouldBeNull()
    {
        using CompoundFile file = OpenSample();

        Assert.IsNull(file.RootStorage.Parent);
    }

    /// <summary>
    /// Verifies that a stream opened from the root storage reports that storage as its parent.
    /// </summary>
    [TestMethod]
    public void Parent_WhenStreamOpenedFromStorage_ShouldBeThatStorage()
    {
        using CompoundFile file = OpenSample();
        string name = file.RootStorage.EnumerateStreams().First().Name;

        using CompoundStream stream = file.RootStorage.OpenStream(name);

        Assert.AreSame(file.RootStorage, stream.Parent);
    }

    /// <summary>
    /// Verifies that a nested storage reports the enclosing storage as its parent.
    /// </summary>
    [TestMethod]
    public void Parent_WhenNestedStorage_ShouldBeEnclosingStorage()
    {
        var destination = new MemoryStream();
        using CompoundFile file = CompoundFile.Create(destination, leaveOpen: true);

        CompoundStorage child = file.RootStorage.CreateStorage("Storage 1");

        Assert.AreSame(file.RootStorage, child.Parent);
    }

    /// <summary>
    /// Verifies that a writable stream created under a storage reports that storage as its parent.
    /// </summary>
    [TestMethod]
    public void Parent_WhenWritableStreamCreated_ShouldBeOwningStorage()
    {
        var destination = new MemoryStream();
        using CompoundFile file = CompoundFile.Create(destination, leaveOpen: true);
        CompoundStorage child = file.RootStorage.CreateStorage("Storage 1");

        using CompoundStream stream = child.CreateStream("Nested");

        Assert.AreSame(child, stream.Parent);
    }
}

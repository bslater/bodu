// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgContainerTests.TryReadStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound;

namespace Bodu.Formats.Outlook.Msg;

public partial class MsgContainerTests
{
    /// <summary>
    /// Verifies that the mediated read returns the stream's bytes under both read strategies.
    /// </summary>
    [TestMethod]
    [DataRow(CompoundReadStrategy.Buffered)]
    [DataRow(CompoundReadStrategy.Streaming)]
    public void TryReadStream_WhenStreamExists_ShouldReturnItsBytes(CompoundReadStrategy strategy)
    {
        byte[] payload = MsgFixtureBuilder.CreatePatternedPayload(70_000);
        using MemoryStream container = new MsgFixtureBuilder().AddBinary(0x3701, payload).Build();
        using var compound = CompoundFile.Open(container, new CompoundFileOptions { ReadStrategy = strategy });

        Assert.IsTrue(MsgContainer.TryReadStream(compound.RootStorage, StreamName, out byte[]? bytes));
        CollectionAssert.AreEqual(payload, bytes);
    }

    /// <summary>
    /// Verifies that a missing stream reports <see langword="false" /> without throwing.
    /// </summary>
    [TestMethod]
    public void TryReadStream_WhenStreamAbsent_ShouldReturnFalse()
    {
        using MemoryStream container = MsgFixtureBuilder.CreateMinimal().Build();
        using var compound = CompoundFile.Open(container);

        Assert.IsFalse(MsgContainer.TryReadStream(compound.RootStorage, StreamName, out byte[]? bytes));
        Assert.IsNull(bytes);
    }

    /// <summary>
    /// Verifies that bytes read from a writable container are a private copy: a later write to the same stream does
    /// not alter them.
    /// </summary>
    [TestMethod]
    public void TryReadStream_WhenContainerIsWritable_ShouldReturnPrivateCopy()
    {
        byte[] payload = MsgFixtureBuilder.CreatePatternedPayload(256);
        using MemoryStream container = new MsgFixtureBuilder().AddBinary(0x3701, payload).Build();
        using var compound = CompoundFile.Open(container, FileMode.Open, FileAccess.ReadWrite, leaveOpen: true);

        Assert.IsTrue(MsgContainer.TryReadStream(compound.RootStorage, StreamName, out byte[]? bytes));
        using (CompoundStream writable = compound.RootStorage.OpenStream(StreamName, FileMode.Open, FileAccess.ReadWrite))
        {
            writable.Position = 0;
            writable.Write(new byte[16]);
        }

        CollectionAssert.AreEqual(payload, bytes);
    }
}

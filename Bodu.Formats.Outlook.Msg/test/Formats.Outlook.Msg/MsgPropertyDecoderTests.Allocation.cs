// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgPropertyDecoderTests.Allocation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound;
using Bodu.Test;

namespace Bodu.Formats.Outlook.Msg;

public partial class MsgPropertyDecoderTests
{
    /// <summary>
    /// Verifies that decoding a large binary value under the buffered read strategy allocates at most one and a half
    /// times the payload: the container's own materialization of the stream, not a second copy on top of it.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Decode_WhenLargeBinaryValueIsBuffered_ShouldNotCopyThePayloadTwice()
    {
        const int PayloadLength = 4 * 1024 * 1024;
        byte[] payload = MsgFixtureBuilder.CreatePatternedPayload(PayloadLength);
        using MemoryStream container = new MsgFixtureBuilder().AddBinary(0x3701, payload).Build();
        using var compound = CompoundFile.Open(container, new CompoundFileOptions { ReadStrategy = CompoundReadStrategy.Buffered });

        long before = GC.GetAllocatedBytesForCurrentThread();
        MapiPropertyCollection properties = MsgPropertyDecoder.Decode(
            compound.RootStorage, MsgPropertyStreamKind.Root, CompoundValidationLevel.Compatible, inheritedEncoding: null, out _);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(PayloadLength, properties.GetBinary(0x3701)!.Value.Length);
        Assert.IsTrue(
            allocated <= PayloadLength * 3 / 2,
            $"Decoding a {PayloadLength / (1024 * 1024)} MiB value allocated {allocated / (1024 * 1024.0):F1} MiB — the value stream is copied twice.");
    }
}

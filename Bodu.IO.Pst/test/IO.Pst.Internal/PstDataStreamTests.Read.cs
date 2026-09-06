// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstDataStreamTests.Read.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst.Internal;

public partial class PstDataStreamTests
{
    /// <summary>
    /// Verifies that a read spanning a leaf boundary copies from both leaves in one call.
    /// </summary>
    [TestMethod]
    public void Read_WhenBufferStraddlesLeafBoundary_ShouldCopyAcrossLeaves()
    {
        byte[] first = Payload(100, 1);
        byte[] second = Payload(100, 2);
        Stream stream = OpenStream(out PstFile file, first, second);
        using (file)
        using (stream)
        {
            stream.Position = 90;
            var buffer = new byte[20];

            Assert.AreEqual(20, stream.Read(buffer, 0, 20));
            CollectionAssert.AreEqual((byte[])[.. first.AsSpan(90).ToArray(), .. second.AsSpan(0, 10).ToArray()], buffer);
        }
    }

    /// <summary>
    /// Verifies that an empty buffer reads nothing and leaves the position unchanged.
    /// </summary>
    [TestMethod]
    public void Read_WhenBufferIsEmpty_ShouldReturnZero()
    {
        Stream stream = OpenStream(out PstFile file, Payload(10, 1));
        using (file)
        using (stream)
        {
            stream.Position = 3;

            Assert.AreEqual(0, stream.Read(Span<byte>.Empty));
            Assert.AreEqual(3L, stream.Position);
        }
    }

    /// <summary>
    /// Verifies that byte-wise reads walk the payload and report the end with -1.
    /// </summary>
    [TestMethod]
    public void ReadByte_WhenSequential_ShouldMatchPayloadThenReportEnd()
    {
        byte[] payload = Payload(5, 9);
        Stream stream = OpenStream(out PstFile file, payload);
        using (file)
        using (stream)
        {
            var read = new List<int>();
            int value;
            while ((value = stream.ReadByte()) >= 0)
                read.Add(value);

            CollectionAssert.AreEqual(payload.Select(static b => (int)b).ToList(), read);
            Assert.AreEqual(-1, stream.ReadByte());
        }
    }

    /// <summary>
    /// Verifies that the synchronous copy delivers the whole payload without an intermediate buffer of its own.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenCalled_ShouldCopyWholePayload()
    {
        byte[] first = Payload(3000, 1);
        byte[] second = Payload(3000, 2);
        Stream stream = OpenStream(out PstFile file, first, second);
        using (file)
        using (stream)
        {
            var target = new MemoryStream();
            stream.CopyTo(target, 1024);

            CollectionAssert.AreEqual((byte[])[.. first, .. second], target.ToArray());
        }
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstPropertyContextTests.TryOpenValueStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

public partial class PstPropertyContextTests
{
    /// <summary>
    /// Verifies that the stream over a heap-resident value yields the same bytes as the materialized value.
    /// </summary>
    [TestMethod]
    [DataRow(StringId)]
    [DataRow(BinaryId)]
    [DataRow(GuidId)]
    public void TryOpenValueStream_WhenValueIsHeapResident_ShouldMatchRawData(int propertyId)
    {
        (PstFile file, PstPropertyContext context) = OpenSharedContext();
        using (file)
        {
            Assert.IsTrue(context.TryOpenValueStream((ushort)propertyId, out Stream? stream));
            using (stream)
            {
                Assert.IsTrue(stream.CanRead);
                Assert.IsTrue(stream.CanSeek);
                Assert.IsFalse(stream.CanWrite);

                var buffer = new MemoryStream();
                stream.CopyTo(buffer);
                CollectionAssert.AreEqual(context.GetValue((ushort)propertyId).RawData.ToArray(), buffer.ToArray());
                Assert.AreEqual(buffer.Length, stream.Length);
            }
        }
    }

    /// <summary>
    /// Verifies that the stream over a subnode-resident value yields the same bytes as the materialized value.
    /// </summary>
    [TestMethod]
    public void TryOpenValueStream_WhenValueIsSubnodeResident_ShouldMatchRawData()
    {
        (PstFile file, PstPropertyContext context) = OpenSharedContext();
        using (file)
        {
            Assert.IsTrue(context.TryOpenValueStream(SubnodeStringId, out Stream? stream));
            using (stream)
            {
                var buffer = new MemoryStream();
                stream.CopyTo(buffer);
                CollectionAssert.AreEqual(context.GetValue(SubnodeStringId).RawData.ToArray(), buffer.ToArray());
            }
        }
    }

    /// <summary>
    /// Verifies that the stream over an inline value yields the inline bytes.
    /// </summary>
    [TestMethod]
    public void TryOpenValueStream_WhenValueIsInline_ShouldMatchRawData()
    {
        (PstFile file, PstPropertyContext context) = OpenSharedContext();
        using (file)
        {
            Assert.IsTrue(context.TryOpenValueStream(Int32Id, out Stream? stream));
            using (stream)
            {
                var buffer = new MemoryStream();
                stream.CopyTo(buffer);
                CollectionAssert.AreEqual(context.GetValue(Int32Id).RawData.ToArray(), buffer.ToArray());
            }
        }
    }

    /// <summary>
    /// Verifies that an absent property reports <see langword="false" /> and no stream.
    /// </summary>
    [TestMethod]
    public void TryOpenValueStream_WhenPropertyAbsent_ShouldReturnFalse()
    {
        (PstFile file, PstPropertyContext context) = OpenSharedContext();
        using (file)
        {
            Assert.IsFalse(context.TryOpenValueStream(0x0FFF, out Stream? stream));
            Assert.IsNull(stream);
        }
    }

    /// <summary>
    /// Verifies that a subnode-resident value above the materialization limit streams in full under a memory ceiling
    /// well below its logical size: the limit governs materialization, never streaming.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    public void TryOpenValueStream_WhenSubnodeTreeExceedsMaterializationLimit_ShouldStreamUnderCeiling()
    {
        const long CeilingBytes = 8L * 1024 * 1024;
        (PstFile file, PstPropertyContext context) = OpenLargeSubnodeContext(out long expectedLength);
        using (file)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            long baseline = GC.GetTotalMemory(forceFullCollection: true);

            long totalRead = 0;
            long maxDelta = 0;
            var chunk = new byte[64 * 1024];
            Assert.IsTrue(context.TryOpenValueStream(LargeBinaryId, out Stream? stream));
            using (stream)
            {
                Assert.AreEqual(expectedLength, stream.Length);

                int chunkIndex = 0;
                int read;
                while ((read = stream.Read(chunk, 0, chunk.Length)) > 0)
                {
                    totalRead += read;
                    if ((chunkIndex++ & 63) == 0)
                        maxDelta = Math.Max(maxDelta, GC.GetTotalMemory(forceFullCollection: false) - baseline);
                }
            }

            Assert.AreEqual(expectedLength, totalRead, "The stream must yield the full logical payload.");
            Assert.IsTrue(maxDelta < CeilingBytes,
                $"Streaming a {expectedLength / (1024 * 1024)} MB value must stay under the {CeilingBytes / (1024 * 1024)} MB ceiling; " +
                $"observed a {maxDelta / (1024 * 1024)} MB peak — the payload is being materialized instead of streamed.");
        }
    }
}

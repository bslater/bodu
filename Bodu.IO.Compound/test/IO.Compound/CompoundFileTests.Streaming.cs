// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileTests.Streaming.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Test;
using Bodu.Test.Kat;

namespace Bodu.IO.Compound;

public partial class CompoundFileTests
{
    /// <summary>
    /// Verifies that opening a fixture in streaming mode exposes the same stream paths and content (by SHA-256) as the
    /// buffered reader.
    /// </summary>
    /// <param name="kat">The reference fixture row.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(CompoundReferenceManifest.ValidFixtures), typeof(CompoundReferenceManifest),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Open_WhenStreaming_ShouldMatchBufferedStreamHashes(CompoundReferenceFixtureKat kat)
    {
        Dictionary<string, string> buffered;
        using (MemoryStream bufferedSource = CompoundFixtures.OpenReference(kat.RelativePath))
        using (CompoundFile file = CompoundFile.Open(bufferedSource))
            buffered = HashStreams(file.RootStorage);

        using MemoryStream streamingSource = CompoundFixtures.OpenReference(kat.RelativePath);
        using CompoundFile streaming = CompoundFile.Open(streamingSource, buffered: false);
        Dictionary<string, string> actual = HashStreams(streaming.RootStorage);

        CollectionAssert.AreEquivalent(buffered.Keys.ToList(), actual.Keys.ToList());
        foreach (KeyValuePair<string, string> expected in buffered)
            Assert.AreEqual(expected.Value, actual[expected.Key], expected.Key);
    }

    /// <summary>
    /// Verifies that a streaming stream read in odd-sized chunks, including after a seek, returns the same bytes as the
    /// buffered materialization.
    /// </summary>
    [TestMethod]
    public void Open_WhenStreamingAndReadInChunks_ShouldMatchMaterialized()
    {
        using MemoryStream source = CompoundFixtures.OpenReference("valid/clean.dat");
        using CompoundFile file = CompoundFile.Open(source, buffered: false);

        foreach ((CompoundStorage storage, CompoundEntryInfo info) in EnumerateAllStreams(file.RootStorage))
        {
            using CompoundStream full = storage.OpenStream(info.Name);
            byte[] expected = full.ReadAllBytes().ToArray();

            using CompoundStream stream = storage.OpenStream(info.Name);
            using MemoryStream sink = new();
            byte[] chunk = new byte[7];
            int read;
            while ((read = stream.Read(chunk, 0, chunk.Length)) > 0)
                sink.Write(chunk, 0, read);

            CollectionAssert.AreEqual(expected, sink.ToArray(), info.Name);

            if (expected.Length > 1)
            {
                _ = stream.Seek(expected.Length - 1, SeekOrigin.Begin);
                Assert.AreEqual(expected[^1], (byte)stream.ReadByte(), info.Name);
            }
        }
    }

    /// <summary>
    /// Verifies that requesting a streaming (non-buffered) open over a non-seekable stream throws
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Open_WhenStreamingAndNotSeekable_ShouldThrowArgumentException()
    {
        using var source = new NonSeekableReadStream(CompoundFixtures.OpenReference("valid/clean.dat").ToArray());

        _ = Assert.ThrowsExactly<ArgumentException>(() => CompoundFile.Open(source, buffered: false));
    }

    /// <summary>
    /// Verifies that disposing a streaming file disposes the source unless <c>leaveOpen</c> is set.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenStreaming_ShouldHonorLeaveOpen()
    {
        MemoryStream owned = CompoundFixtures.OpenReference("valid/clean.dat");
        CompoundFile.Open(owned, buffered: false).Dispose();
        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => owned.ReadByte());

        using MemoryStream kept = CompoundFixtures.OpenReference("valid/clean.dat");
        CompoundFile.Open(kept, buffered: false, leaveOpen: true).Dispose();
        Assert.IsTrue(kept.CanRead);
    }

    /// <summary>
    /// Records the SHA-256 of every stream in a storage hierarchy, keyed by storage-qualified path.
    /// </summary>
    /// <param name="storage">The storage to walk.</param>
    /// <returns>A map from stream path to lowercase hexadecimal SHA-256.</returns>
    private static Dictionary<string, string> HashStreams(CompoundStorage storage)
    {
        Dictionary<string, string> hashes = new(StringComparer.Ordinal);
        Walk(storage, string.Empty, hashes);
        return hashes;

        static void Walk(CompoundStorage current, string prefix, Dictionary<string, string> map)
        {
            foreach (CompoundEntryInfo info in current.EnumerateStreams())
            {
                string path = prefix.Length == 0 ? info.Name : prefix + "/" + info.Name;
                using CompoundStream stream = current.OpenStream(info.Name);
                map[path] = Convert.ToHexString(SHA256.HashData(stream.ReadAllBytes().Span)).ToLowerInvariant();
            }

            foreach (CompoundStorage child in current.EnumerateStorages())
                Walk(child, prefix.Length == 0 ? child.Name : prefix + "/" + child.Name, map);
        }
    }

    /// <summary>
    /// Enumerates every stream in a storage hierarchy.
    /// </summary>
    /// <param name="storage">The storage to walk.</param>
    /// <returns>The owning storage and metadata of each stream in the hierarchy.</returns>
    private static IEnumerable<(CompoundStorage Storage, CompoundEntryInfo Info)> EnumerateAllStreams(CompoundStorage storage)
    {
        foreach (CompoundEntryInfo info in storage.EnumerateStreams())
            yield return (storage, info);

        foreach (CompoundStorage child in storage.EnumerateStorages())
        {
            foreach ((CompoundStorage, CompoundEntryInfo) pair in EnumerateAllStreams(child))
                yield return pair;
        }
    }

    /// <summary>
    /// A read-only, non-seekable stream wrapper used to exercise the seekable-stream guard.
    /// </summary>
    private sealed class NonSeekableReadStream(byte[] data)
        : Stream
    {
        private readonly MemoryStream _inner = new(data);

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void Flush()
        {
        }

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _inner.Dispose();

            base.Dispose(disposing);
        }
    }
}

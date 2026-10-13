// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStreamTests.SetLength.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

public partial class CompoundStreamTests
{
    /// <summary>
    /// Verifies that setting a length beyond <see cref="int.MaxValue" /> throws
    /// <see cref="NotSupportedException" /> before allocating.
    /// </summary>
    [TestMethod]
    public void SetLength_WhenBeyondInt32MaxValue_ShouldThrowNotSupportedException()
    {
        using var destination = new MemoryStream();
        using var file = CompoundFile.Create(destination, leaveOpen: true);
        using CompoundStream cursor = file.RootStorage.CreateStream("Data");

        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            cursor.SetLength(int.MaxValue + 1L);
        });
    }

    /// <summary>
    /// Verifies that a truncating <see cref="CompoundStream.SetLength(long)" /> alone marks a pending change that
    /// the dispose-time flush persists.
    /// </summary>
    [TestMethod]
    public void SetLength_WhenShrinkingExistingPayload_ShouldPersistTruncationOnDispose()
    {
        using var destination = new MemoryStream();
        using var file = CompoundFile.Create(destination, leaveOpen: true);

        file.RootStorage.CreateStream("Data", new byte[] { 1, 2, 3, 4 });
        using (CompoundStream cursor = file.RootStorage.OpenStream("Data", FileMode.Open, FileAccess.ReadWrite))
            cursor.SetLength(2);

        file.Commit();

        using var reopened = CompoundFile.Open(new MemoryStream(destination.ToArray(), writable: false));
        using CompoundStream read = reopened.RootStorage.OpenStream("Data");
        CollectionAssert.AreEqual(new byte[] { 1, 2 }, read.ReadAllBytes());
    }

    /// <summary>
    /// Verifies that changing the length of a cursor keeps its position unless the new length cuts the stream short
    /// before it.
    /// </summary>
    [TestMethod]
    public void SetLength_WhenGrowing_ShouldKeepThePosition()
    {
        using var file = CompoundFile.Create(new MemoryStream());
        using CompoundStream cursor = file.RootStorage.CreateStream("Data");
        cursor.Write(CompoundContainerBytes.Pattern(10000));
        cursor.Seek(6000, SeekOrigin.Begin);

        cursor.SetLength(7000);
        Assert.AreEqual(6000, cursor.Position);

        cursor.SetLength(20000);
        Assert.AreEqual(6000, cursor.Position);

        cursor.SetLength(5000);
        Assert.IsLessThanOrEqualTo(5000L, cursor.Position);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStream.SetLength(long)" /> on a writable cursor throws
    /// <see cref="ObjectDisposedException" /> once the file that owns the cursor has been disposed, and leaves the
    /// cursor's length as it was.
    /// </summary>
    [TestMethod]
    public void SetLength_WhenOwningFileIsDisposed_ShouldThrowObjectDisposedException()
    {
        using CompoundStream cursor = OpenCursorOverDisposedFile();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            cursor.SetLength(1);
        });

        Assert.AreEqual(3L, cursor.Length);
    }
}

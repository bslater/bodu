// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstFileTests.OpenRead.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Test.Assertions;
using Bodu.Test.Kat;

namespace Bodu.IO.Pst;

public partial class PstFileTests
{
    /// <summary>
    /// Verifies that opening a real Unicode PST surfaces its header facts and node directory — the package's primary
    /// happy path.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void OpenRead_WhenUnicodeFixture_ShouldExposeHeaderFactsAndNodes()
    {
        using var file = PstFile.OpenRead(PstReferenceFixtures.OpenStream(Sample1));

        Assert.AreEqual(PstFileFormat.Unicode, file.Format);
        Assert.AreEqual(PstCryptMethod.Permute, file.CryptMethod);
        Assert.IsTrue(file.EnumerateNodes().Any());
    }

    /// <summary>
    /// Verifies that a file opens from a path.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenPath_ShouldOpenFile()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".pst");
        using (MemoryStream fixture = PstReferenceFixtures.OpenStream(Sample1))
            File.WriteAllBytes(path, fixture.ToArray());

        try
        {
            using var file = PstFile.OpenRead(path);

            Assert.AreEqual(PstFileFormat.Unicode, file.Format);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Verifies that a path whose contents are not a PST file is rejected, and that the file handle the overload
    /// opened is released rather than left to the finalizer - a caller that catches the exception and retries, or
    /// deletes the file, must not be blocked by a handle this method still holds.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenPathIsNotPstData_ShouldThrowAndReleaseTheFileHandle()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".pst");
        File.WriteAllBytes(path, new byte[1024]);

        try
        {
            _ = Assert.ThrowsExactly<PstFileFormatException>(() =>
            {
                _ = PstFile.OpenRead(path);
            });

            // Opening for exclusive write succeeds only if no handle from the failed open survives.
            using FileStream exclusive = File.Open(path, FileMode.Open, FileAccess.Write, FileShare.None);
            Assert.IsTrue(exclusive.CanWrite);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Verifies that <c>leaveOpen</c> governs whether disposing the session disposes the caller's stream.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenLeaveOpen_ShouldHonorStreamOwnership()
    {
        MemoryStream kept = PstReferenceFixtures.OpenStream(Sample1);
        using (PstFile.OpenRead(kept, leaveOpen: true))
        {
        }

        Assert.IsTrue(kept.CanRead);
        kept.Dispose();

        MemoryStream disposed = PstReferenceFixtures.OpenStream(Sample1);
        using (PstFile.OpenRead(disposed, leaveOpen: false))
        {
        }

        Assert.IsFalse(disposed.CanRead);
    }

    /// <summary>
    /// Verifies the argument guards of the open overloads.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenArgumentsNull_ShouldThrowArgumentNullException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = PstFile.OpenRead((string)null!);
            },
            "path");

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = PstFile.OpenRead((Stream)null!);
            },
            "stream");
    }

    /// <summary>
    /// Verifies that a stream that does not begin with the PST magics is rejected as structurally invalid.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenNotPstData_ShouldThrowPstFileFormatException()
    {
        using var stream = new MemoryStream(new byte[1024]);

        _ = Assert.ThrowsExactly<PstFileFormatException>(() =>
        {
            _ = PstFile.OpenRead(stream);
        });
    }

    /// <summary>
    /// Verifies that a stream carrying only the magics but truncated before the full header is rejected.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenHeaderTruncated_ShouldThrowPstFileFormatException()
    {
        byte[] prefix;
        using (MemoryStream fixture = PstReferenceFixtures.OpenStream(Sample1))
            prefix = fixture.ToArray().AsSpan(0, 64).ToArray();

        using var stream = new MemoryStream(prefix);

        _ = Assert.ThrowsExactly<PstFileFormatException>(() =>
        {
            _ = PstFile.OpenRead(stream);
        });
    }

    /// <summary>
    /// Verifies that a recognized ANSI-format file is rejected with the unsupported-format exception rather than a
    /// structural error, for each ANSI fixture in the reference corpus.
    /// </summary>
    /// <param name="fixture">The manifest row of the ANSI fixture.</param>
    [TestMethod]
    [DynamicData(
        nameof(AnsiFixtureRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void OpenRead_WhenAnsiFixture_ShouldOpenAndReportAnsiFormat(PstReferenceFixture fixture)
    {
        using MemoryStream stream = PstReferenceFixtures.OpenStream(fixture.File);

        using PstFile file = PstFile.OpenRead(stream);

        Assert.AreEqual(PstFileFormat.Ansi, file.Format);
        Assert.IsTrue(file.EnumerateNodes().Any(), "An ANSI store must enumerate its nodes.");
        Assert.IsTrue(file.TryGetNode(PstNodeId.MessageStore, out _), "The message store node must resolve.");
        Assert.IsTrue(file.TryGetNode(PstNodeId.RootFolder, out _), "The root folder node must resolve.");
    }
}

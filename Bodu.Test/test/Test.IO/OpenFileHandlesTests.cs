// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OpenFileHandlesTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.IO;

/// <summary>
/// Verifies <see cref="OpenFileHandles" />: that it lists a file while the process holds it open, and stops listing it
/// once the file is closed.
/// </summary>
[TestClass]
public sealed class OpenFileHandlesTests
{
    /// <summary>
    /// Verifies that a file the process holds open is listed, deleted or not, and is no longer listed once it is
    /// closed.
    /// </summary>
    [TestMethod]
    public void Find_WhenFileIsOpenThenClosed_ShouldListItOnlyWhileOpen()
    {
        if (!OpenFileHandles.IsSupported) Assert.Inconclusive("Listing open files needs /proc/self/fd (Linux).");

        string name = "bodu-open-file-handles-" + Guid.NewGuid().ToString("N");
        string path = Path.Combine(Path.GetTempPath(), name);

        using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete))
        {
            Assert.HasCount(1, OpenFileHandles.Find(name));

            File.Delete(path);

            string deleted = OpenFileHandles.Find(name).Single();
            Assert.EndsWith(" (deleted)", deleted);
        }

        Assert.IsEmpty(OpenFileHandles.Find(name));
    }

    /// <summary>
    /// Verifies that an empty fragment, which would match every open file, is rejected with
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Find_WhenFragmentIsEmpty_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = OpenFileHandles.Find(string.Empty);
        });

        Assert.AreEqual("pathFragment", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> fragment is rejected with <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Find_WhenFragmentIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = OpenFileHandles.Find(null!);
        });

        Assert.AreEqual("pathFragment", ex.ParamName);
    }

    /// <summary>
    /// Verifies that, on a platform without <c>/proc/self/fd</c>, a search throws
    /// <see cref="PlatformNotSupportedException" /> rather than reporting that nothing is open.
    /// </summary>
    [TestMethod]
    public void Find_WhenPlatformIsNotSupported_ShouldThrowPlatformNotSupportedException()
    {
        if (OpenFileHandles.IsSupported) Assert.Inconclusive("This platform can list open files.");

        _ = Assert.ThrowsExactly<PlatformNotSupportedException>(() =>
        {
            _ = OpenFileHandles.Find("bodu");
        });
    }
}

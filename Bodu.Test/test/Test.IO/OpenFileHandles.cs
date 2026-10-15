// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OpenFileHandles.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.IO;

/// <summary>
/// Lists the files the current process holds open, so a test can check that the code under test released every file
/// it opened.
/// </summary>
/// <remarks>
/// <para>
/// The list is read from <c>/proc/self/fd</c>, so it is available on Linux only, which <see cref="IsSupported" />
/// reports. That is the platform that needs it: Windows refuses to delete a file that is still open, so code that
/// leaks a handle and then deletes its file fails there on its own, while Linux lets the deletion succeed and the leak
/// go unseen.
/// </para>
/// <para>
/// This class is intended exclusively for test harness use and must not appear in production code.
/// </para>
/// </remarks>
public static class OpenFileHandles
{
    /// <summary>
    /// The directory that holds one symbolic link to its target for each open file descriptor of the current process.
    /// </summary>
    private const string DescriptorDirectory = "/proc/self/fd";

    /// <summary>
    /// Gets a value indicating whether the current platform can list the files the process holds open.
    /// </summary>
    /// <value><see langword="true" /> on Linux; otherwise, <see langword="false" />.</value>
    public static bool IsSupported =>
        OperatingSystem.IsLinux() && Directory.Exists(DescriptorDirectory);

    /// <summary>
    /// Returns the path of every file the current process holds open whose path contains
    /// <paramref name="pathFragment" />.
    /// </summary>
    /// <param name="pathFragment">The text to look for in each open file's path.</param>
    /// <returns>
    /// The matching paths, one for each open descriptor, so a file opened twice appears twice. A file deleted while
    /// still open keeps the <c> (deleted)</c> suffix the kernel adds to its path.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="pathFragment" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="pathFragment" /> is empty.</exception>
    /// <exception cref="PlatformNotSupportedException"><see cref="IsSupported" /> is <see langword="false" />.</exception>
    public static IReadOnlyList<string> Find(string pathFragment)
    {
        ArgumentException.ThrowIfNullOrEmpty(pathFragment);
        if (!IsSupported) throw new PlatformNotSupportedException();

        var matches = new List<string>();
        foreach (string descriptor in Directory.EnumerateFileSystemEntries(DescriptorDirectory))
        {
            string? target;
            try
            {
                target = new FileInfo(descriptor).LinkTarget;
            }
            catch (IOException)
            {
                // The descriptor was closed between listing the directory and reading its link.
                continue;
            }

            if (target is not null && target.Contains(pathFragment, StringComparison.Ordinal))
                matches.Add(target);
        }

        return matches;
    }
}

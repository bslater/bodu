// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMessageTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Msg;

namespace Bodu.Formats.Outlook;

/// <summary>
/// Verifies the behavior of <see cref="OutlookMessage" />, the <c>.msg</c> reading session.
/// </summary>
[TestClass]
public partial class OutlookMessageTests
{
    /// <summary>
    /// Builds the minimal well-formed message container shared by the member tests.
    /// </summary>
    /// <returns>A seekable stream positioned at the container start.</returns>
    internal static MemoryStream CreateMinimalContainer() =>
        MsgFixtureBuilder.CreateMinimal().Build();

    /// <summary>
    /// Writes a built container to a new file under the temporary directory.
    /// </summary>
    /// <param name="container">The built container.</param>
    /// <returns>The file's path; the caller deletes the file.</returns>
    private static string WriteTemporaryMessage(MemoryStream container)
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".msg");
        File.WriteAllBytes(path, container.ToArray());
        return path;
    }

    /// <summary>
    /// Asserts that no handle holds a file open, by opening it for reading and writing with no sharing.
    /// </summary>
    /// <param name="path">The file's path.</param>
    private static void AssertFileIsReleased(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException ex)
        {
            Assert.Fail($"The file is still open: {ex.Message}");
        }
    }
}

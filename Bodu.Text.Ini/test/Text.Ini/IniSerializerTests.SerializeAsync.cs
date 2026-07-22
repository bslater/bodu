// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniSerializerTests.SerializeAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Ini;

/// <summary>
/// Contains tests for
/// <see cref="IniSerializer.SerializeAsync{T}(Stream, T, IniSerializerOptions?, CancellationToken)" />.
/// </summary>
public partial class IniSerializerTests
{
    /// <summary>
    /// Verifies that the asynchronous stream overload writes the same UTF-8 bytes as the synchronous surface.
    /// </summary>
    [TestMethod]
    public async Task SerializeAsync_WhenStreamDestination_ShouldWriteUtf8Bytes()
    {
        var config = new ServerConfig { Name = "app", Database = new DatabaseSection { Host = "x", Port = 1 } };
        using var stream = new MemoryStream();

        await IniSerializer.SerializeAsync(stream, config);

        Assert.AreEqual(
            "Name=app\nRetries=0\n[Database]\nHost=x\nPort=1\n",
            Encoding.UTF8.GetString(stream.ToArray()));
    }
}

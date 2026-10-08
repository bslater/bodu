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

    /// <summary>
    /// Verifies that a member whose value INI cannot hold throws <see cref="IniSerializationException" /> naming the
    /// section and the key, with the writer's <see cref="ArgumentException" /> as the inner exception.
    /// </summary>
    [TestMethod]
    public async Task SerializeAsync_WhenValueCannotBeWritten_ShouldThrowIniSerializationException()
    {
        var config = new ServerConfig { Name = "app", Database = new DatabaseSection { Host = "db\r\nexample", Port = 1 } };
        using var stream = new MemoryStream();

        IniSerializationException ex = await Assert.ThrowsExactlyAsync<IniSerializationException>(async () =>
        {
            await IniSerializer.SerializeAsync(stream, config);
        });

        Assert.IsNotNull(ex.InnerException);
        Assert.AreEqual(typeof(ArgumentException), ex.InnerException.GetType());
        Assert.Contains("'Database'", ex.Message);
        Assert.Contains("'Host'", ex.Message);
    }
}

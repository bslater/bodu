// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniSerializerTests.DeserializeAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Ini;

/// <summary>
/// Contains tests for
/// <see cref="IniSerializer.DeserializeAsync{T}(Stream, IniSerializerOptions?, CancellationToken)" />.
/// </summary>
public partial class IniSerializerTests
{
    /// <summary>
    /// Verifies that the asynchronous stream overload deserializes the same value as the synchronous surface.
    /// </summary>
    [TestMethod]
    public async Task DeserializeAsync_WhenStreamSource_ShouldPopulatePoco()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Name=app\n[Database]\nHost=x\nPort=7\n"));

        ServerConfig config = await IniSerializer.DeserializeAsync<ServerConfig>(stream);

        Assert.AreEqual("app", config.Name);
        Assert.IsNotNull(config.Database);
        Assert.AreEqual(7, config.Database.Port);
    }

    /// <summary>
    /// Verifies that a type with a member INI cannot represent is rejected before the stream is read, leaving the
    /// stream at its start.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task DeserializeAsync_WhenTheTypeHasAMemberIniCannotRepresent_ShouldThrowBeforeReadingTheStream()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Name=a\n"));

        await Assert.ThrowsExactlyAsync<IniSerializationException>(async () =>
        {
            _ = await IniSerializer.DeserializeAsync<GetOnlyRootListConfig>(stream);
        });

        Assert.AreEqual(0, stream.Position);
    }
}

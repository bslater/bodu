// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvSerializerTests.DeserializeAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Text.DotEnv;

/// <summary>
/// Contains the <see cref="DotEnvSerializer.DeserializeAsync{T}(Stream, DotEnvSerializerOptions?, CancellationToken)" />
/// backbone tests.
/// </summary>
public partial class DotEnvSerializerTests
{
    /// <summary>
    /// Verifies that a type with a member whose type is not a scalar throws <see cref="DotEnvSerializationException" />
    /// before the stream is read, with the message <c>Serialize</c> gives, and leaves the stream at its start.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsync_WhenAMemberIsNotAScalar_ShouldThrowBeforeReadingTheStream()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Name=a\n"));

        DotEnvSerializationException ex = await Assert.ThrowsExactlyAsync<DotEnvSerializationException>(async () =>
        {
            _ = await DotEnvSerializer.DeserializeAsync<DictionaryMemberConfig>(stream);
        });

        Assert.AreEqual(string.Format(CultureInfo.CurrentCulture, DotEnvResourceStrings.Op_Invalid_DotEnvValueNotScalar, "Counts", typeof(Dictionary<string, int>)), ex.Message);
        Assert.AreEqual(0, stream.Position);
    }
}

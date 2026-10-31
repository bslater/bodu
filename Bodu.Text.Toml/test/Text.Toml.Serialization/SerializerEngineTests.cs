// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerializerEngineTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml.Serialization;

/// <summary>
/// Verifies the guard of the shared <see cref="SerializerEngine" /> that the public surface cannot reach: a reader
/// that yields no token is reported as an unexpected end of input rather than handed to a converter.
/// </summary>
[TestClass]
public class SerializerEngineTests
{
    /// <summary>
    /// Verifies that deserializing from a reader that has already consumed every token throws
    /// <see cref="TomlSerializationException" /> naming the target type, since no token is left to bind.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenReaderIsExhausted_ShouldThrowTomlSerializationException()
    {
        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = DeserializeFromExhaustedReader<int>("a = 1\n"u8);
        });

        Assert.IsTrue(ex.Message.Contains(typeof(int).ToString(), StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Reads every token of a document and then asks the engine to deserialize a value from the exhausted reader.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="utf8Toml">The document.</param>
    /// <returns>The value the engine returns, which it never does.</returns>
    private static T DeserializeFromExhaustedReader<T>(ReadOnlySpan<byte> utf8Toml)
    {
        var reader = new TomlDocumentReader(utf8Toml);
        while (reader.Read())
        {
        }

        return SerializerEngine.Deserialize<T>(ref reader, new TomlSerializerOptions());
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlConverterFactoryTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Serialization;

/// <summary>
/// Verifies that a <see cref="TomlConverterFactory" /> is never used as a converter in its own right: the engine's
/// object-typed read and write entry points, which the serializer calls only on the converter a factory creates,
/// refuse to run on the factory itself.
/// </summary>
[TestClass]
public class TomlConverterFactoryTests
{
    /// <summary>
    /// Verifies that reading through a factory's object-typed entry point throws
    /// <see cref="InvalidOperationException" />, since only the converter it creates reads a value.
    /// </summary>
    [TestMethod]
    public void ReadAsObject_WhenCalledOnFactory_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = ReadThroughFactory(new IntegerFactory(), "a = 1\n"u8);
        });
    }

    /// <summary>
    /// Verifies that writing through a factory's object-typed entry point throws
    /// <see cref="InvalidOperationException" />, since only the converter it creates writes a value.
    /// </summary>
    [TestMethod]
    public void WriteAsObject_WhenCalledOnFactory_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            WriteThroughFactory(new IntegerFactory(), 1);
        });
    }

    /// <summary>
    /// Positions a reader on the first token of a document and reads a value through the factory's object-typed entry
    /// point.
    /// </summary>
    /// <param name="factory">The factory.</param>
    /// <param name="utf8Toml">The document.</param>
    /// <returns>The value the factory returns, which it never does.</returns>
    private static object? ReadThroughFactory(TomlConverterFactory factory, ReadOnlySpan<byte> utf8Toml)
    {
        var reader = new TomlDocumentReader(utf8Toml);
        _ = reader.Read();

        return factory.ReadAsObject(ref reader, typeof(int), new TomlSerializerOptions());
    }

    /// <summary>
    /// Writes a value through the factory's object-typed entry point.
    /// </summary>
    /// <param name="factory">The factory.</param>
    /// <param name="value">The value.</param>
    private static void WriteThroughFactory(TomlConverterFactory factory, object? value)
    {
        var writer = new Utf8TomlWriter(new ArrayBufferWriter<byte>());

        factory.WriteAsObject(writer, value, new TomlSerializerOptions());
    }

    /// <summary>
    /// A factory that creates the options' own converter for <see cref="int" />.
    /// </summary>
    private sealed class IntegerFactory
        : TomlConverterFactory
    {
        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert) =>
            typeToConvert == typeof(int);

        /// <inheritdoc />
        public override TomlConverter CreateConverter(Type typeToConvert, TomlSerializerOptions options) =>
            options.GetConverter(typeToConvert);
    }
}

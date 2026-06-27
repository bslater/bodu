// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlWriterTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Test.Assertions;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the <see cref="Utf8TomlWriter" /> constructors, including destination validation.
/// </summary>
public sealed partial class Utf8TomlWriterTests
{
    /// <summary>
    /// Verifies that constructing a stream-backed writer over a non-writable stream throws
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenStreamNotWritable_ShouldThrowArgumentException()
    {
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            using var stream = new MemoryStream([], writable: false);
            _ = new Utf8TomlWriter(stream);
        });
    }

    /// <summary>
    /// Verifies that passing a <see langword="null" /> buffer writer to the constructor throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>output</c>.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenOutputIsNull_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = new Utf8TomlWriter((System.Buffers.IBufferWriter<byte>)null!);
        }, "output");
    }

    /// <summary>
    /// Verifies that passing a <see langword="null" /> buffer writer to the options constructor overload throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>output</c>.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenOutputIsNull_ForOptionsOverload_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = new Utf8TomlWriter((System.Buffers.IBufferWriter<byte>)null!, new TomlWriterOptions { MaxDepth = 4 });
        }, "output");
    }

}

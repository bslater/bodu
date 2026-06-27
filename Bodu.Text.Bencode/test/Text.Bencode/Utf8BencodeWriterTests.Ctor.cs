// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeWriterTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Test.Assertions;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the <see cref="Utf8BencodeWriter" /> constructors, including destination validation.
/// </summary>
public partial class Utf8BencodeWriterTests
{
    /// <summary>
    /// Verifies that constructing the writer with a <see langword="null" /> output throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>output</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenOutputNull_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = new Utf8BencodeWriter(null!);
        }, "output");
    }

    /// <summary>
    /// Verifies that constructing the writer with a <see langword="null" /> output and explicit options throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>output</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenOutputNull_ForOptionsOverload_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = new Utf8BencodeWriter(null!, new BencodeWriterOptions { MaxDepth = 4 });
        }, "output");
    }

}

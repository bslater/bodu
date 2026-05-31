// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedReaderTests.Ctors.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

public sealed partial class DelimitedReaderTests
{
    /// <summary>
    /// Verifies that constructing a <see cref="DelimitedReader" /> with a <see langword="null" />
    /// <see cref="TextReader" /> throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenReaderIsNull_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DelimitedReader(null!);
        });
    }

    /// <summary>
    /// Verifies that constructing a <see cref="DelimitedReader" /> with <paramref name="bufferSize" /> of
    /// zero throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenBufferSizeIsZero_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new DelimitedReader(TextReader.Null, DelimitedParseOptions.Default, 0);
        });
    }

    /// <summary>
    /// Verifies that constructing a <see cref="DelimitedReader" /> with a negative
    /// <paramref name="bufferSize" /> throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenBufferSizeIsNegative_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new DelimitedReader(TextReader.Null, DelimitedParseOptions.Default, -1);
        });
    }

    /// <summary>
    /// Verifies that constructing a <see cref="DelimitedReader" /> with <paramref name="bufferSize" /> of
    /// 1 succeeds.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenBufferSizeIsOne_ShouldSucceed()
    {
        using DelimitedReader reader = new(TextReader.Null, DelimitedParseOptions.Default, 1);
        Assert.AreEqual(0, reader.Headers.Count);
        Assert.AreEqual(0, reader.RowNumber);
    }
}

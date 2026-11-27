// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvReaderStateTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv.Reader;

public sealed partial class DotEnvReaderStateTests
{
    /// <summary>
    /// Verifies that a new state describes the start of an input: a reader built from it skips a leading byte-order
    /// mark, and reports an error on line 1 at the column counted from the byte after the mark.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCreated_ShouldDescribeTheStartOfTheInput()
    {
        byte[] input = [0xEF, 0xBB, 0xBF, .. "KEY value\n"u8.ToArray()];

        DotEnvFormatException ex = Assert.ThrowsExactly<DotEnvFormatException>(() =>
        {
            var reader = new Utf8DotEnvReader(input, isFinalBlock: true, new DotEnvReaderState(new DotEnvReaderOptions { SkipComments = true }));
            while (reader.Read())
            {
            }
        });

        Assert.AreEqual(1, ex.LineNumber);
        Assert.AreEqual(5, ex.ColumnNumber);
        Assert.AreEqual(7, ex.Offset);
    }

    /// <summary>
    /// Verifies that a default-initialized state reads an input exactly as a state built with the default options does.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefaultInitialized_ShouldReadAsAStateBuiltWithTheDefaultOptions()
    {
        byte[] input = "# note\nexport A=1\nB=\"x\\ny\" # inline\n"u8.ToArray();

        CollectionAssert.AreEqual(
            DotEnvReaderTranscript.ReadInBlocks(input, new DotEnvReaderState(DotEnvReaderOptions.Default), []),
            DotEnvReaderTranscript.ReadInBlocks(input, default(DotEnvReaderState), []));
    }
}

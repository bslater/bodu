// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedReaderStateTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited.Reader;

public sealed partial class DelimitedReaderStateTests
{
    /// <summary>
    /// Verifies that a new state describes the start of an input: a reader built from it reads the first record from
    /// line 1, at offset 0, and skips a leading byte-order mark.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCreated_ShouldDescribeTheStartOfTheInput()
    {
        byte[] input = [0xEF, 0xBB, 0xBF, .. "a,b\n"u8.ToArray()];
        var reader = new Utf8DelimitedReader(input, isFinalBlock: true, new DelimitedReaderState(new DelimitedReaderOptions { NoHeader = true }));

        Assert.IsTrue(reader.Read());
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(DelimitedTokenType.StartArray, reader.TokenType);
        Assert.AreEqual(1, reader.RecordLine);
        Assert.AreEqual(3, reader.RecordOffset);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual("a", reader.GetString());
    }

    /// <summary>
    /// Verifies that a default-initialized state reads an input exactly as a state built with the default options does.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefaultInitialized_ShouldReadAsAStateBuiltWithTheDefaultOptions()
    {
        byte[] input = "name,city\nAda,London\n"u8.ToArray();

        CollectionAssert.AreEqual(
            DelimitedReaderTranscript.ReadInBlocks(input, new DelimitedReaderState(DelimitedReaderOptions.Default), []),
            DelimitedReaderTranscript.ReadInBlocks(input, default(DelimitedReaderState), []));
    }
}

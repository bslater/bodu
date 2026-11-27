// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniReaderStateTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini.Reader;

public sealed partial class IniReaderStateTests
{
    /// <summary>
    /// Verifies that a new state describes the start of an input: a reader built from it skips a leading byte-order
    /// mark and reports the first section header on line 1, at the offset of its bracket in the whole input.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCreated_ShouldDescribeTheStartOfTheInput()
    {
        byte[] input = [0xEF, 0xBB, 0xBF, .. "[s]\nk=v\n"u8.ToArray()];
        var reader = new Utf8IniReader(input, isFinalBlock: true, new IniReaderState(new IniReaderOptions { SkipComments = true }));

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(IniTokenType.SectionHeader, reader.TokenType);
        Assert.AreEqual("s", reader.GetString());
        Assert.AreEqual(1, reader.LineNumber);
        Assert.AreEqual(3, reader.TokenOffset);
    }

    /// <summary>
    /// Verifies that a default-initialized state reads an input exactly as a state built with the default options does.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefaultInitialized_ShouldReadAsAStateBuiltWithTheDefaultOptions()
    {
        byte[] input = "# note\nglobal=1\n[s] ; trailing\nk = v\n"u8.ToArray();

        CollectionAssert.AreEqual(
            IniReaderTranscript.ReadInBlocks(input, new IniReaderState(IniReaderOptions.Default), []),
            IniReaderTranscript.ReadInBlocks(input, default(IniReaderState), []));
    }
}

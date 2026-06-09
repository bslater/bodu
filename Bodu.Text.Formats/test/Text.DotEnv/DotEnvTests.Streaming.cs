// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvTests.Streaming.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv;

public partial class DotEnvTests
{
    /// <summary>
    /// Verifies that <see cref="DotEnv.CreateReader(TextReader)" /> returns a streaming reader that surfaces the
    /// source's key and value.
    /// </summary>
    [TestMethod]
    public void CreateReader_WhenGivenReader_ShouldStreamEntries()
    {
        using DotEnvReader reader = DotEnv.CreateReader(new StringReader("PORT=8080"));

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(("PORT", "8080"), (reader.Key, reader.Value));
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.CreateReader(TextReader, DotEnvParseOptions)" /> returns a streaming reader
    /// honoring the supplied options.
    /// </summary>
    [TestMethod]
    public void CreateReader_WhenGivenReaderAndOptions_ShouldStreamEntries()
    {
        using DotEnvReader reader = DotEnv.CreateReader(new StringReader("HOST=local"), DotEnvParseOptions.Default);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual("HOST", reader.Key);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.CreateWriter(TextWriter)" /> returns a streaming writer that emits the authored
    /// entry.
    /// </summary>
    [TestMethod]
    public void CreateWriter_WhenGivenWriter_ShouldEmitDotEnv()
    {
        StringWriter sink = new();

        using (DotEnvWriter writer = DotEnv.CreateWriter(sink))
            writer.WriteEntry("PORT", "8080");

        Assert.Contains("PORT", sink.ToString());
    }
}

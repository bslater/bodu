// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniTests.Streaming.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

public partial class IniTests
{
    /// <summary>
    /// Verifies that <see cref="Ini.CreateReader(TextReader)" /> returns a streaming reader that surfaces the source's
    /// section, key, and value.
    /// </summary>
    [TestMethod]
    public void CreateReader_WhenGivenReader_ShouldStreamEntries()
    {
        using IniReader reader = Ini.CreateReader(new StringReader("[s]\nk=v"));

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(("s", "k", "v"), (reader.Section, reader.Key, reader.Value));
    }

    /// <summary>
    /// Verifies that <see cref="Ini.CreateReader(TextReader, IniParseOptions)" /> returns a streaming reader honoring the
    /// supplied options.
    /// </summary>
    [TestMethod]
    public void CreateReader_WhenGivenReaderAndOptions_ShouldStreamEntries()
    {
        using IniReader reader = Ini.CreateReader(new StringReader("g=1"), IniParseOptions.Default);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual("g", reader.Key);
    }

    /// <summary>
    /// Verifies that <see cref="Ini.CreateWriter(TextWriter)" /> returns a streaming writer that emits the authored
    /// section and entry.
    /// </summary>
    [TestMethod]
    public void CreateWriter_WhenGivenWriter_ShouldEmitIni()
    {
        StringWriter sink = new();

        using (IniWriter writer = Ini.CreateWriter(sink))
        {
            writer.WriteSection("s");
            writer.WriteEntry("k", "v");
        }

        Assert.Contains("[s]", sink.ToString());
        Assert.Contains("k", sink.ToString());
    }
}

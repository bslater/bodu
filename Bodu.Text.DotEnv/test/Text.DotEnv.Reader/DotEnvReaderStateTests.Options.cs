// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvReaderStateTests.Options.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv.Reader;

public sealed partial class DotEnvReaderStateTests
{
    /// <summary>
    /// Verifies that <see cref="DotEnvReaderState.Options" /> returns the options the state was created with.
    /// </summary>
    [TestMethod]
    public void Options_WhenCreatedWithOptions_ShouldReturnThem()
    {
        var options = new DotEnvReaderOptions { DisallowExportPrefix = true, SkipComments = true };

        var state = new DotEnvReaderState(options);

        Assert.AreEqual(options, state.Options);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvReaderState.Options" /> of a default-initialized state is
    /// <see cref="DotEnvReaderOptions.Default" />.
    /// </summary>
    [TestMethod]
    public void Options_WhenDefaultInitialized_ShouldBeTheDefaultOptions() =>
        Assert.AreEqual(DotEnvReaderOptions.Default, default(DotEnvReaderState).Options);

    /// <summary>
    /// Verifies that the state a reader captures carries the reader's options, so the next block's reader reads the
    /// same syntax.
    /// </summary>
    [TestMethod]
    public void Options_WhenCapturedFromAReader_ShouldBeTheReadersOptions()
    {
        var options = new DotEnvReaderOptions { DisallowInlineComments = true };
        var reader = new Utf8DotEnvReader("A=1 # kept\n"u8, isFinalBlock: false, new DotEnvReaderState(options));

        while (reader.Read())
        {
        }

        Assert.AreEqual(options, reader.CurrentState.Options);
    }
}

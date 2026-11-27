// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniReaderStateTests.Options.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini.Reader;

public sealed partial class IniReaderStateTests
{
    /// <summary>
    /// Verifies that <see cref="IniReaderState.Options" /> returns the options the state was created with.
    /// </summary>
    [TestMethod]
    public void Options_WhenCreatedWithOptions_ShouldReturnThem()
    {
        var options = new IniReaderOptions { DisallowHashComments = true, SkipComments = true };

        var state = new IniReaderState(options);

        Assert.AreEqual(options, state.Options);
    }

    /// <summary>
    /// Verifies that <see cref="IniReaderState.Options" /> of a default-initialized state is
    /// <see cref="IniReaderOptions.Default" />.
    /// </summary>
    [TestMethod]
    public void Options_WhenDefaultInitialized_ShouldBeTheDefaultOptions() =>
        Assert.AreEqual(IniReaderOptions.Default, default(IniReaderState).Options);

    /// <summary>
    /// Verifies that the state a reader captures carries the reader's options, so the next block's reader reads the
    /// same dialect.
    /// </summary>
    [TestMethod]
    public void Options_WhenCapturedFromAReader_ShouldBeTheReadersOptions()
    {
        var options = new IniReaderOptions { DisallowHashComments = true };
        var reader = new Utf8IniReader("#k=v\n"u8, isFinalBlock: false, new IniReaderState(options));

        while (reader.Read())
        {
        }

        Assert.AreEqual(options, reader.CurrentState.Options);
    }
}

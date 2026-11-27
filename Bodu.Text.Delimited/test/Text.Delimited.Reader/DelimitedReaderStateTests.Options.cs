// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedReaderStateTests.Options.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited.Reader;

public sealed partial class DelimitedReaderStateTests
{
    /// <summary>
    /// Verifies that <see cref="DelimitedReaderState.Options" /> returns the options the state was created with.
    /// </summary>
    [TestMethod]
    public void Options_WhenCreatedWithOptions_ShouldReturnThem()
    {
        var options = new DelimitedReaderOptions { Delimiter = ';', NoHeader = true, AllowComments = true };

        var state = new DelimitedReaderState(options);

        Assert.AreEqual(options, state.Options);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedReaderState.Options" /> of a default-initialized state is
    /// <see cref="DelimitedReaderOptions.Default" />.
    /// </summary>
    [TestMethod]
    public void Options_WhenDefaultInitialized_ShouldBeTheDefaultOptions() =>
        Assert.AreEqual(DelimitedReaderOptions.Default, default(DelimitedReaderState).Options);

    /// <summary>
    /// Verifies that the state a reader captures carries the reader's options, so the next block's reader applies the
    /// same dialect.
    /// </summary>
    [TestMethod]
    public void Options_WhenCapturedFromAReader_ShouldBeTheReadersOptions()
    {
        var options = new DelimitedReaderOptions { Delimiter = '\t', TrimFields = true };
        var reader = new Utf8DelimitedReader("a\tb\n"u8, isFinalBlock: false, new DelimitedReaderState(options));

        while (reader.Read())
        {
        }

        Assert.AreEqual(options, reader.CurrentState.Options);
    }
}

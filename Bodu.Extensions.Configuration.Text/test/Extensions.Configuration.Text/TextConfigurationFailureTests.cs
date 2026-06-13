// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextConfigurationFailureTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test.IO;
using Bodu.Text.Configuration;
using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// Verifies that the file and stream provider variants preserve the malformed-file, optional-file, and
/// missing-file conventions defined by <see cref="FileConfigurationProvider" /> and that the bridge does not
/// silently swallow parse exceptions raised by <see cref="ConfigurationDocument" />.
/// </summary>
[TestClass]
public class TextConfigurationFailureTests
{
    private const string Malformed = """
[unterminated section
key = value
""";

    /// <summary>
    /// Verifies that a missing file with <c>optional: false</c> throws when the configuration is built —
    /// matching the JSON/INI provider conventions in <c>Microsoft.Extensions.Configuration</c>.
    /// </summary>
    [TestMethod]
    public void Build_WhenMissingFileAndRequired_ShouldThrowFileNotFoundException()
    {
        using TempDirectoryScope scope = new();
        IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(scope.Path)
            .AddBoduConfigurationFile("does-not-exist.boduconfig", targetPath: null, optional: false, reloadOnChange: false);

        Assert.ThrowsExactly<FileNotFoundException>(() => _ = builder.Build());
    }

    /// <summary>
    /// Verifies that a missing file with <c>optional: true</c> yields an empty configuration view without
    /// throwing, matching the standard provider behaviour.
    /// </summary>
    [TestMethod]
    public void Build_WhenMissingFileAndOptional_ShouldYieldEmptyConfiguration()
    {
        using TempDirectoryScope scope = new();
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(scope.Path)
            .AddBoduConfigurationFile("missing.boduconfig", targetPath: null, optional: true, reloadOnChange: false)
            .Build();

        Assert.IsNull(configuration["anything"]);
    }

    /// <summary>
    /// Verifies that a malformed required file surfaces the parse error during build. The exception is
    /// wrapped by <see cref="FileConfigurationProvider" /> in an <see cref="InvalidDataException" /> whose
    /// inner exception is the <see cref="ConfigurationParseException" />.
    /// </summary>
    [TestMethod]
    public void Build_WhenMalformedRequiredFile_ShouldThrowAndPreserveParseException()
    {
        using TempDirectoryScope scope = new();
        scope.WriteFile("malformed.boduconfig", Malformed);

        IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(scope.Path)
            .AddBoduConfigurationFile("malformed.boduconfig", targetPath: null, optional: false, reloadOnChange: false);

        InvalidDataException ex = Assert.ThrowsExactly<InvalidDataException>(() => _ = builder.Build());

        Assert.IsNotNull(ex.InnerException);
        Assert.IsInstanceOfType<ConfigurationParseException>(ex.InnerException);
    }

    /// <summary>
    /// Verifies that a malformed *optional* file still throws by default — optional means "may be absent",
    /// not "may be corrupt". This matches the JSON provider behaviour and protects against silent data loss.
    /// </summary>
    [TestMethod]
    public void Build_WhenMalformedOptionalFile_ShouldStillThrow()
    {
        using TempDirectoryScope scope = new();
        scope.WriteFile("malformed.boduconfig", Malformed);

        IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(scope.Path)
            .AddBoduConfigurationFile("malformed.boduconfig", targetPath: null, optional: true, reloadOnChange: false);

        Assert.ThrowsExactly<InvalidDataException>(() => _ = builder.Build());
    }

    /// <summary>
    /// Verifies that a malformed stream source surfaces the parse error during build. Stream sources do not
    /// go through the <see cref="FileConfigurationProvider" /> wrapping, so the parse exception is raised
    /// directly.
    /// </summary>
    [TestMethod]
    public void Build_WhenMalformedStreamSource_ShouldThrowParseException()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(Malformed));

        IConfigurationBuilder builder = new ConfigurationBuilder().AddBoduConfigurationStream(stream);

        Assert.ThrowsExactly<ConfigurationParseException>(() => _ = builder.Build());
    }

    /// <summary>
    /// Verifies that an empty stream produces an empty configuration view without throwing — empty input is
    /// a legal document.
    /// </summary>
    [TestMethod]
    public void Build_WhenStreamIsEmpty_ShouldYieldEmptyConfiguration()
    {
        using MemoryStream stream = new(System.Array.Empty<byte>());

        IConfiguration configuration = new ConfigurationBuilder()
            .AddBoduConfigurationStream(stream)
            .Build();

        Assert.IsNull(configuration["anything"]);
    }

    /// <summary>
    /// Verifies that a custom <see cref="ConfigurationParseOptions" /> with the Relaxed (collect) diagnostic
    /// mode allows a malformed-but-recoverable stream to load without throwing. The well-formed preamble
    /// keys survive even though one line is malformed.
    /// </summary>
    [TestMethod]
    public void Build_WhenMalformedStreamWithRelaxedOptions_ShouldNotThrow()
    {
        // Missing '=' on the second line is recoverable when diagnostics are collected; the property is
        // dropped but parsing continues. Place all keys in the preamble so resolution with targetPath==null
        // exposes them.
        const string slightlyBroken = "good = value\nbroken_no_equals\nalso_good = other\n";
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(slightlyBroken));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddBoduConfigurationStream(stream, targetPath: null, parseOptions: ConfigurationParseOptions.Relaxed)
            .Build();

        Assert.AreEqual("value", configuration["good"]);
        Assert.AreEqual("other", configuration["also_good"]);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeConfigurationTests.AddBencodeFile.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.IO;

using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

public sealed partial class BencodeConfigurationTests
{
    /// <summary>
    /// Verifies that loading an existing Bencode file flattens its values into the colon-delimited key model.
    /// </summary>
    [TestMethod]
    public void AddBencodeFile_WhenFileExists_ShouldLoadValues()
    {
        using TempFileScope scope = new("d2:dbd4:porti5432ee4:name9:from-filee");
        IConfigurationRoot config = new ConfigurationBuilder().AddBencodeFile(scope.Path).Build();

        Assert.AreEqual("from-file", config["name"]);
        Assert.AreEqual("5432", config["db:port"]);
    }

    /// <summary>
    /// Verifies that an optional missing Bencode file builds an empty configuration without throwing.
    /// </summary>
    [TestMethod]
    public void AddBencodeFile_WhenOptionalAndMissing_ShouldBuildEmpty()
    {
        IConfigurationRoot config = new ConfigurationBuilder().AddBencodeFile("does-not-exist.bencode", optional: true).Build();

        Assert.IsNull(config["anything"]);
    }

    /// <summary>
    /// Verifies that a required missing Bencode file throws <see cref="FileNotFoundException" /> when built.
    /// </summary>
    [TestMethod]
    public void AddBencodeFile_WhenRequiredAndMissing_ShouldThrowFileNotFound()
    {
        Assert.ThrowsExactly<FileNotFoundException>(() =>
        {
            _ = new ConfigurationBuilder().AddBencodeFile("does-not-exist.bencode", optional: false).Build();
        });
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> builder throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void AddBencodeFile_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = BencodeConfigurationExtensions.AddBencodeFile(null!, "settings.bencode");
        });

        Assert.AreEqual("builder", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a whitespace path throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void AddBencodeFile_WhenPathIsWhiteSpace_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new ConfigurationBuilder().AddBencodeFile("   ");
        });

        Assert.AreEqual("path", ex.ParamName);
    }
}

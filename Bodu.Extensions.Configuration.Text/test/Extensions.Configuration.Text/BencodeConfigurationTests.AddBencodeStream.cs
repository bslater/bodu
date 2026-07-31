// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeConfigurationTests.AddBencodeStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Text.Bencode;

using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

public sealed partial class BencodeConfigurationTests
{
    /// <summary>
    /// Verifies that nested dictionaries flatten into colon-delimited keys with one segment per level.
    /// </summary>
    [TestMethod]
    public void AddBencodeStream_WhenLoaded_ShouldFlattenNestedKeys()
    {
        IConfigurationRoot config = BuildFromStream(Sample);

        Assert.AreEqual("localhost", config["server:host"]);
        Assert.AreEqual("8080", config["server:port"]);
        Assert.AreEqual("1", config["server:enabled"]);
    }

    /// <summary>
    /// Verifies that list elements flatten to zero-based indexed keys, mirroring the framework JSON provider.
    /// </summary>
    [TestMethod]
    public void AddBencodeStream_WhenList_ShouldFlattenToIndexedKeys()
    {
        IConfigurationRoot config = BuildFromStream(Sample);

        Assert.AreEqual("info", config["logging:levels:0"]);
        Assert.AreEqual("warn", config["logging:levels:1"]);
    }

    /// <summary>
    /// Verifies that <see cref="IConfiguration.GetSection(string)" /> exposes the flattened children of a nested
    /// dictionary.
    /// </summary>
    [TestMethod]
    public void AddBencodeStream_WhenSectionRequested_ShouldExposeChildren()
    {
        IConfigurationRoot config = BuildFromStream(Sample);

        var children = config.GetSection("server").GetChildren().Select(section => section.Key).ToList();

        CollectionAssert.AreEquivalent(new[] { "enabled", "host", "port" }, children);
    }

    /// <summary>
    /// Verifies that an empty root dictionary builds an empty configuration.
    /// </summary>
    [TestMethod]
    public void AddBencodeStream_WhenEmptyDictionary_ShouldBuildEmpty()
    {
        IConfigurationRoot config = BuildFromStream("de");

        Assert.IsNull(config["anything"]);
    }

    /// <summary>
    /// Verifies that a malformed document throws <see cref="BencodeFormatException" /> when built.
    /// </summary>
    [TestMethod]
    public void AddBencodeStream_WhenInvalidBencode_ShouldThrowBencodeFormatException()
    {
        Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = BuildFromStream("d1:a");
        });
    }

    /// <summary>
    /// Verifies that the strict canonical defaults apply: a document with unsorted dictionary keys is rejected with
    /// <see cref="BencodeFormatException" />.
    /// </summary>
    [TestMethod]
    public void AddBencodeStream_WhenKeysUnsorted_ShouldThrowBencodeFormatException()
    {
        Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = BuildFromStream("d1:b1:x1:a1:ye");
        });
    }

    /// <summary>
    /// Verifies that a document whose root is not a dictionary throws <see cref="FormatException" /> when built.
    /// </summary>
    /// <param name="bencode">The non-dictionary-rooted document.</param>
    [TestMethod]
    [DataRow("i1e")]
    [DataRow("le")]
    [DataRow("4:spam")]
    public void AddBencodeStream_WhenRootNotDictionary_ShouldThrowFormatException(string bencode)
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = BuildFromStream(bencode);
        });
    }

    /// <summary>
    /// Verifies that two keys differing only in case — valid canonical Bencode — collide in the case-insensitive
    /// configuration key model and throw <see cref="FormatException" /> when built.
    /// </summary>
    [TestMethod]
    public void AddBencodeStream_WhenKeysCollideCaseInsensitively_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = BuildFromStream("d1:A1:x1:a1:ye");
        });
    }

    /// <summary>
    /// Verifies that an integer beyond <see cref="long.MaxValue" /> flattens to its full invariant digits.
    /// </summary>
    [TestMethod]
    public void AddBencodeStream_WhenIntegerBeyondInt64_ShouldFlattenFullDigits()
    {
        IConfigurationRoot config = BuildFromStream("d3:bigi18446744073709551615ee");

        Assert.AreEqual("18446744073709551615", config["big"]);
    }

    /// <summary>
    /// Verifies that a negative integer flattens with its sign intact.
    /// </summary>
    [TestMethod]
    public void AddBencodeStream_WhenIntegerNegative_ShouldFlattenWithSign()
    {
        IConfigurationRoot config = BuildFromStream("d1:ni-42ee");

        Assert.AreEqual("-42", config["n"]);
    }

    /// <summary>
    /// Verifies that a byte-string value that is not valid UTF-8 decodes with U+FFFD replacement rather than being
    /// rejected, pinning the documented decoding contract.
    /// </summary>
    [TestMethod]
    public void AddBencodeStream_WhenValueNotUtf8_ShouldDecodeWithReplacement()
    {
        var stream = new MemoryStream([(byte)'d', (byte)'1', (byte)':', (byte)'a', (byte)'1', (byte)':', 0xFF, (byte)'e']);
        IConfigurationRoot config = new ConfigurationBuilder().AddBencodeStream(stream).Build();

        Assert.AreEqual("\uFFFD", config["a"]);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> builder or stream throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void AddBencodeStream_WhenNullArguments_ShouldThrowArgumentNullException()
    {
        var builderEx = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = BencodeConfigurationExtensions.AddBencodeStream(null!, new MemoryStream());
        });

        var streamEx = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new ConfigurationBuilder().AddBencodeStream(null!);
        });

        Assert.AreEqual("builder", builderEx.ParamName);
        Assert.AreEqual("stream", streamEx.ParamName);
    }

    /// <summary>
    /// Verifies that a non-seekable, non-memory stream is drained to its end and parsed.
    /// </summary>
    [TestMethod]
    public void AddBencodeStream_WhenStreamNotMemoryStream_ShouldReadToEnd()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("d1:k5:valuee");
        using var inner = new MemoryStream(bytes);
        using var buffered = new BufferedStream(inner, bufferSize: 4);

        IConfigurationRoot config = new ConfigurationBuilder().AddBencodeStream(buffered).Build();

        Assert.AreEqual("value", config["k"]);
    }
}

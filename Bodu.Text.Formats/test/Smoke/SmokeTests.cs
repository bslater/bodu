// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SmokeTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Text.Formats.Ini;
using DotEnvParser = Bodu.Text.Formats.DotEnv.DotEnv;
using IniParser = Bodu.Text.Formats.Ini.Ini;

namespace Bodu.Text.Formats;

/// <summary>
/// Smoke tests for <see cref="Bodu.Text.Formats" />. Each test exercises one happy-path on a primary public type
/// so that the smoke-tier build catches catastrophic breakage in any of the format's load-bearing surfaces.
/// </summary>
[TestClass]
public sealed class SmokeTests
{

    /// <summary>
    /// Verifies that <see cref="Bencode.Decode(ReadOnlySpan{byte})" /> and
    /// <see cref="Bencode.Encode(BencodedValue)" /> round-trip the canonical BEP 3 string example.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Bencode_DecodeEncode_ShouldRoundTripBep3StringExample()
    {
        byte[] encoded = System.Text.Encoding.ASCII.GetBytes("4:spam");

        BencodedValue decoded = Bencode.Decode(encoded);
        byte[] reencoded = Bencode.Encode(decoded);

        Assert.AreEqual("spam", ((BencodedString)decoded).GetUtf8String());
        CollectionAssert.AreEqual(encoded, reencoded);
    }

    /// <summary>
    /// Verifies that <see cref="BencodedDictionary" /> constructs with two pairs and enumerates them in sorted
    /// raw-byte-string key order.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void BencodedDictionary_Construct_ShouldExposeOrderedItems()
    {
        BencodedDictionary dict = new(new[]
        {
            new KeyValuePair<BencodedString, BencodedValue>(BencodedString.FromUtf8("cow"), BencodedString.FromUtf8("moo")),
            new KeyValuePair<BencodedString, BencodedValue>(BencodedString.FromUtf8("spam"), BencodedString.FromUtf8("eggs")),
        });

        string[] orderedKeys = dict.GetOrderedItems()
            .Select(pair => pair.Key.GetUtf8String())
            .ToArray();

        CollectionAssert.AreEqual(new[] { "cow", "spam" }, orderedKeys);
    }

    /// <summary>
    /// Verifies that <see cref="BencodedInteger" /> constructs successfully and exposes the supplied value with
    /// <see cref="BencodedValueKind.Integer" />.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void BencodedInteger_Construct_ShouldExposeValueAndKind()
    {
        BencodedInteger integer = new(42);

        Assert.AreEqual(42, integer.Value);
        Assert.AreEqual(BencodedValueKind.Integer, integer.Kind);
    }

    /// <summary>
    /// Verifies that <see cref="BencodedList" /> constructs with two values and exposes
    /// <see cref="BencodedList.Count" /> equal to two.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void BencodedList_Construct_ShouldExposeItems()
    {
        BencodedList list = new(new BencodedValue[]
        {
            BencodedString.FromUtf8("spam"),
            new BencodedInteger(42),
        });

        Assert.AreEqual(2, list.Count);
    }

    /// <summary>
    /// Verifies that <see cref="BencodedString.FromUtf8(string)" /> followed by
    /// <see cref="BencodedString.GetUtf8String" /> round-trips text content.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void BencodedString_FromUtf8_ShouldRoundTripText()
    {
        BencodedString value = BencodedString.FromUtf8("hello");

        Assert.AreEqual("hello", value.GetUtf8String());
    }

    /// <summary>
    /// Verifies that <see cref="BencodedStringComparer.Ordinal" /> compares byte sequences by raw byte order.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void BencodedStringComparer_OrdinalCompare_ShouldOrderByRawBytes()
    {
        BencodedString a = new(new byte[] { 0xAA });
        BencodedString b = new(new byte[] { 0xAA, 0xBB });

        Assert.IsTrue(BencodedStringComparer.Ordinal.Compare(a, b) < 0);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> and
    /// <see cref="DotEnv.Format(DotEnvDocument)" /> round-trip a simple DotEnv document — all keys and values
    /// survive unchanged.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void DotEnv_ParseFormat_ShouldRoundTripSimpleDocument()
    {
        const string source = "HOST=localhost\nPORT=8080\nDEBUG=True";

        Bodu.Text.Formats.DotEnv.DotEnvDocument original = DotEnvParser.Parse(source);
        string formatted = DotEnvParser.Format(original);
        Bodu.Text.Formats.DotEnv.DotEnvDocument roundTripped = DotEnvParser.Parse(formatted);

        Assert.AreEqual("localhost", roundTripped["HOST"]);
        Assert.AreEqual("8080", roundTripped["PORT"]);
        Assert.AreEqual("True", roundTripped["DEBUG"]);
    }

    /// <summary>
    /// Verifies that <see cref="Ini.Parse(ReadOnlySpan{char})" /> and <see cref="Ini.Format(IniDocument)" />
    /// round-trip a simple INI document — all sections, keys, and values survive unchanged.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Ini_ParseFormat_ShouldRoundTripSimpleDocument()
    {
        const string source = "global=g\n[server]\nhost=localhost\nport=8080";

        IniDocument original = IniParser.Parse(source);
        string formatted = IniParser.Format(original);
        IniDocument roundTripped = IniParser.Parse(formatted);

        Assert.AreEqual("g", roundTripped.GlobalSection["global"]);
        IniSection? server = roundTripped.GetSection("server");
        Assert.IsNotNull(server);
        Assert.AreEqual("localhost", server["host"]);
        Assert.AreEqual("8080", server["port"]);
    }

}

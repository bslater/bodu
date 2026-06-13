// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeReaderTests.Leniency.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Reader;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the opt-in dictionary-key leniency of <see cref="Utf8BencodeReader" /> —
/// <see cref="BencodeReaderOptions.AllowUnsortedKeys" /> and <see cref="BencodeReaderOptions.AllowDuplicateKeys" /> —
/// including that each option relaxes only its own rule and that the strict default distinguishes the duplicate-key
/// error from the unordered-key error.
/// </summary>
public partial class Utf8BencodeReaderTests
{
    /// <summary>
    /// Reads every token of the supplied document, returning the number of property names encountered.
    /// </summary>
    /// <param name="data">The document bytes.</param>
    /// <param name="options">The reader options to apply.</param>
    /// <returns>The number of property-name tokens read.</returns>
    private static int CountProperties(byte[] data, BencodeReaderOptions options)
    {
        var reader = new Utf8BencodeReader(data, options);
        var names = 0;
        while (reader.Read())
        {
            if (reader.TokenType == BencodeTokenType.PropertyName)
                names++;
        }

        return names;
    }

    /// <summary>
    /// Verifies that the strict default reports duplicate keys with the duplicate-key error rather than the
    /// unordered-key error, so the two failure modes are distinguishable.
    /// </summary>
    [TestMethod]
    public void Read_WhenDuplicateKeysWithStrictDefaults_ShouldReportDuplicateKeyError()
    {
        var data = Bytes("d1:ai1e1:ai2ee");

        var ex = Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = CountProperties(data, default);
        });

        Assert.IsTrue(ex.Message.Contains("unique", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that the strict default reports out-of-order keys with the unordered-key error.
    /// </summary>
    [TestMethod]
    public void Read_WhenUnsortedKeysWithStrictDefaults_ShouldReportUnorderedKeyError()
    {
        var data = Bytes("d1:bi1e1:ai2ee");

        var ex = Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = CountProperties(data, default);
        });

        Assert.IsTrue(ex.Message.Contains("sorted", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that <see cref="BencodeReaderOptions.AllowUnsortedKeys" /> accepts a dictionary whose keys are out of
    /// order while leaving every other rule enforced.
    /// </summary>
    [TestMethod]
    public void Read_WhenUnsortedKeysAllowed_ShouldReadAllEntries()
    {
        var data = Bytes("d1:bi1e1:ai2e1:ci3ee");

        var names = CountProperties(data, new BencodeReaderOptions { AllowUnsortedKeys = true });

        Assert.AreEqual(3, names);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeReaderOptions.AllowUnsortedKeys" /> alone still rejects duplicate keys, even
    /// when the occurrences are separated by another entry where adjacency-based detection cannot see them.
    /// </summary>
    [TestMethod]
    public void Read_WhenUnsortedKeysAllowedButDuplicatesPresent_ShouldThrowBencodeFormatException()
    {
        var data = Bytes("d1:bi1e1:ai2e1:bi3ee");

        var ex = Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = CountProperties(data, new BencodeReaderOptions { AllowUnsortedKeys = true });
        });

        Assert.IsTrue(ex.Message.Contains("unique", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that <see cref="BencodeReaderOptions.AllowDuplicateKeys" /> accepts repeated keys in sorted position
    /// and reports every occurrence as its own property-name token.
    /// </summary>
    [TestMethod]
    public void Read_WhenDuplicateKeysAllowed_ShouldReportEveryOccurrence()
    {
        var data = Bytes("d1:ai1e1:ai2ee");

        var names = CountProperties(data, new BencodeReaderOptions { AllowDuplicateKeys = true });

        Assert.AreEqual(2, names);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeReaderOptions.AllowDuplicateKeys" /> alone does not relax the ordering rule.
    /// </summary>
    [TestMethod]
    public void Read_WhenDuplicateKeysAllowedButKeysUnsorted_ShouldThrowBencodeFormatException()
    {
        var data = Bytes("d1:bi1e1:ai2ee");

        var ex = Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = CountProperties(data, new BencodeReaderOptions { AllowDuplicateKeys = true });
        });

        Assert.IsTrue(ex.Message.Contains("sorted", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that enabling both options accepts a dictionary with unsorted and duplicated keys.
    /// </summary>
    [TestMethod]
    public void Read_WhenBothLeniencyOptionsEnabled_ShouldReadAllEntries()
    {
        var data = Bytes("d1:bi1e1:ai2e1:bi3ee");

        var names = CountProperties(data, new BencodeReaderOptions { AllowUnsortedKeys = true, AllowDuplicateKeys = true });

        Assert.AreEqual(3, names);
    }

    /// <summary>
    /// Verifies that leniency applies per dictionary: a nested dictionary with duplicate keys is accepted while the
    /// keys of the enclosing dictionary are validated independently.
    /// </summary>
    [TestMethod]
    public void Read_WhenNestedDictionaryHasDuplicates_ShouldValidateEachFrameIndependently()
    {
        var data = Bytes("d1:ad1:xi1e1:xi2ee1:bi3ee");

        var names = CountProperties(data, new BencodeReaderOptions { AllowDuplicateKeys = true });

        Assert.AreEqual(4, names);
    }
}

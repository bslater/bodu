// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DelimitedReaderTests.GetString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited.Reader;

/// <summary>
/// Contains the member backbone tests for <see cref="Utf8DelimitedReader.GetString" />.
/// </summary>
public partial class Utf8DelimitedReaderTests
{
    /// <summary>
    /// Verifies that <see cref="Utf8DelimitedReader.GetString" /> throws <see cref="InvalidOperationException" />
    /// before the first <see cref="Utf8DelimitedReader.Read" /> and on the start and end tokens of the document and of
    /// a record, none of which carries text.
    /// </summary>
    /// <param name="reads">The number of tokens of <c>a,b\n1,2\n</c> read before the call.</param>
    /// <param name="token">The token the reader is on after those reads.</param>
    [TestMethod]
    [DataRow(0, DelimitedTokenType.None)]
    [DataRow(1, DelimitedTokenType.StartArray)]
    [DataRow(2, DelimitedTokenType.StartObject)]
    [DataRow(7, DelimitedTokenType.EndObject)]
    [DataRow(8, DelimitedTokenType.EndArray)]
    public void GetString_WhenTheTokenHasNoText_ShouldThrowInvalidOperationException(int reads, DelimitedTokenType token)
    {
        byte[] source = "a,b\n1,2\n"u8.ToArray();
        Assert.AreEqual(token, TokenAfterReads(source, reads));

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = GetStringAfterReads(source, reads);
        });
    }

    /// <summary>
    /// Reads a number of tokens and reports the token the reader is then on.
    /// </summary>
    /// <param name="source">The CSV source bytes.</param>
    /// <param name="reads">The number of tokens to read.</param>
    /// <returns>The current token after the reads.</returns>
    private static DelimitedTokenType TokenAfterReads(byte[] source, int reads)
    {
        var reader = new Utf8DelimitedReader(source);
        for (int i = 0; i < reads; i++)
            _ = reader.Read();

        return reader.TokenType;
    }

    /// <summary>
    /// Reads a number of tokens and then calls <see cref="Utf8DelimitedReader.GetString" />.
    /// </summary>
    /// <param name="source">The CSV source bytes.</param>
    /// <param name="reads">The number of tokens to read first.</param>
    /// <returns>The text <see cref="Utf8DelimitedReader.GetString" /> returns.</returns>
    private static string GetStringAfterReads(byte[] source, int reads)
    {
        var reader = new Utf8DelimitedReader(source);
        for (int i = 0; i < reads; i++)
            _ = reader.Read();

        return reader.GetString();
    }
}

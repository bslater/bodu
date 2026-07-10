// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialJsonConverterGuardsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bodu.Financial.Serialization.Json;

/// <summary>
/// Verifies the defensive reader guards in the financial JSON converters. The unexpected-end and
/// expected-property-name guards cannot be reached through <see cref="JsonSerializer" /> with a complete, comment-free
/// document — the reader enforces object grammar first — so they are driven directly: an unexpected end via a
/// truncated buffer read in non-final-block mode, and an unexpected token via comment-handling that surfaces a comment
/// where a property name is required.
/// </summary>
[TestClass]
public partial class FinancialJsonConverterGuardsTests
{
    private static readonly JsonSerializerOptions s_options = JsonSerializerOptions.Default;

    /// <summary>Serializer options with the financial converters registered under the Strict policy.</summary>
    private static readonly JsonSerializerOptions s_strictOptions = new JsonSerializerOptions().AddFinancialJsonConverters();

    /// <summary>
    /// Invokes a converter's <c>Read</c> against a hand-built reader and asserts that it throws
    /// <see cref="JsonException" />.
    /// </summary>
    /// <typeparam name="T">The converted type.</typeparam>
    /// <param name="converter">The converter under test.</param>
    /// <param name="json">The (possibly truncated) JSON fragment.</param>
    /// <param name="isFinalBlock"><see langword="false" /> to simulate a partial streaming buffer.</param>
    /// <param name="allowComments"><see langword="true" /> to surface comment tokens to the converter.</param>
    private static void AssertReadThrowsJsonException<T>(JsonConverter<T> converter, string json, bool isFinalBlock = true, bool allowComments = false)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(json);
        var readerOptions = new JsonReaderOptions
        {
            CommentHandling = allowComments ? JsonCommentHandling.Allow : JsonCommentHandling.Disallow,
        };

        var reader = new Utf8JsonReader(utf8, isFinalBlock, new JsonReaderState(readerOptions));
        Assert.IsTrue(reader.Read(), "Expected the fragment to begin with a readable token.");

        bool threw = false;
        try
        {
            _ = converter.Read(ref reader, typeof(T), s_options);
        }
        catch (JsonException)
        {
            threw = true;
        }

        Assert.IsTrue(threw, $"Expected a JsonException reading: {json}");
    }
}

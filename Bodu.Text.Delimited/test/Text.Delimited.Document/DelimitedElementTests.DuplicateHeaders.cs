// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedElementTests.DuplicateHeaders.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Delimited.Reader;

namespace Bodu.Text.Delimited.Document;

/// <summary>
/// Verifies how a record element exposes a header row that repeats a column name, under each duplicate-header policy:
/// the winning column keeps the name for <see cref="DelimitedElement.GetProperty(string)" /> and
/// <see cref="DelimitedElement.EnumerateObject" />, and the others are exposed under an empty name.
/// </summary>
public sealed partial class DelimitedElementTests
{
    /// <summary>
    /// Verifies that under <see cref="DelimitedDuplicateHeaderBehavior.TakeFirst" /> a record enumerates the first
    /// column of a duplicated name under the name and the later one under an empty name.
    /// </summary>
    [TestMethod]
    public void EnumerateObject_WhenDuplicateHeaderTakeFirst_ShouldNameOnlyTheFirstColumn()
    {
        using DelimitedDocument document = Parse("a,b,a\n1,2,3\n", DelimitedDuplicateHeaderBehavior.TakeFirst);

        CollectionAssert.AreEqual(new[] { "a=1", "b=2", "=3" }, DescribeFields(document.RootElement[0]));
    }

    /// <summary>
    /// Verifies that under <see cref="DelimitedDuplicateHeaderBehavior.TakeLast" /> a record enumerates the last column
    /// of a duplicated name under the name and the earlier one under an empty name.
    /// </summary>
    [TestMethod]
    public void EnumerateObject_WhenDuplicateHeaderTakeLast_ShouldNameOnlyTheLastColumn()
    {
        using DelimitedDocument document = Parse("a,b,a\n1,2,3\n", DelimitedDuplicateHeaderBehavior.TakeLast);

        CollectionAssert.AreEqual(new[] { "=1", "b=2", "a=3" }, DescribeFields(document.RootElement[0]));
    }

    /// <summary>
    /// Verifies that under <see cref="DelimitedDuplicateHeaderBehavior.TakeFirst" />
    /// <see cref="DelimitedElement.GetProperty(string)" /> finds the first value of a duplicated column by its name,
    /// and the later value by the empty name.
    /// </summary>
    [TestMethod]
    public void GetProperty_WhenDuplicateHeaderTakeFirst_ShouldFindTheFirstValueByTheName()
    {
        using DelimitedDocument document = Parse("a,b,a\n1,2,3\n", DelimitedDuplicateHeaderBehavior.TakeFirst);
        DelimitedElement record = document.RootElement[0];

        Assert.AreEqual("1", record.GetProperty("a").GetString());
        Assert.AreEqual("3", record.GetProperty(string.Empty).GetString());
    }

    /// <summary>
    /// Verifies that under <see cref="DelimitedDuplicateHeaderBehavior.TakeLast" />
    /// <see cref="DelimitedElement.GetProperty(string)" /> finds the last value of a duplicated column by its name, and
    /// the earlier value by the empty name.
    /// </summary>
    [TestMethod]
    public void GetProperty_WhenDuplicateHeaderTakeLast_ShouldFindTheLastValueByTheName()
    {
        using DelimitedDocument document = Parse("a,b,a\n1,2,3\n", DelimitedDuplicateHeaderBehavior.TakeLast);
        DelimitedElement record = document.RootElement[0];

        Assert.AreEqual("3", record.GetProperty("a").GetString());
        Assert.AreEqual("1", record.GetProperty(string.Empty).GetString());
    }

    /// <summary>
    /// Parses a header-mode document under a duplicate-header policy.
    /// </summary>
    /// <param name="text">The delimited text.</param>
    /// <param name="policy">The duplicate-header policy.</param>
    /// <returns>The parsed document, which the caller disposes.</returns>
    private static DelimitedDocument Parse(string text, DelimitedDuplicateHeaderBehavior policy) =>
        DelimitedDocument.Parse(System.Text.Encoding.UTF8.GetBytes(text), new DelimitedReaderOptions { DuplicateHeaderBehavior = policy });

    /// <summary>
    /// Describes a record element's fields in enumeration order.
    /// </summary>
    /// <param name="record">The record element.</param>
    /// <returns>One <c>name=value</c> entry per field.</returns>
    private static string[] DescribeFields(DelimitedElement record)
    {
        var fields = new List<string>();
        foreach (DelimitedProperty field in record.EnumerateObject())
            fields.Add($"{field.Name}={field.Value.GetString()}");

        return [.. fields];
    }
}

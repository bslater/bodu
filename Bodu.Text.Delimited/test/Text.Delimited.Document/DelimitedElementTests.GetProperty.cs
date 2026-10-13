// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedElementTests.GetProperty.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Delimited.Reader;

namespace Bodu.Text.Delimited.Document;

/// <summary>
/// Contains the member backbone tests for <see cref="DelimitedElement.GetProperty(string)" /> and
/// <see cref="DelimitedElement.TryGetProperty(string, out DelimitedElement)" />.
/// </summary>
public sealed partial class DelimitedElementTests
{
    /// <summary>
    /// Verifies that under <see cref="DelimitedFieldCountBehavior.Ragged" /> every name
    /// <see cref="DelimitedElement.EnumerateObject" /> yields for a long record, the header's names and the column
    /// indexes that name the fields beyond them, finds the same field through
    /// <see cref="DelimitedElement.GetProperty(string)" />.
    /// </summary>
    [TestMethod]
    public void GetProperty_WhenARaggedFieldIsNamedByItsColumnIndex_ShouldFindIt()
    {
        var options = new DelimitedReaderOptions { FieldCountBehavior = DelimitedFieldCountBehavior.Ragged };
        using DelimitedDocument document = DelimitedDocument.Parse("a,b\n1,2,3,4\n"u8, options);
        DelimitedElement record = document.RootElement[0];
        var found = new List<string>();

        foreach (DelimitedProperty property in record.EnumerateObject())
            found.Add(property.Name + "=" + record.GetProperty(property.Name).GetString());

        CollectionAssert.AreEqual(new[] { "a=1", "b=2", "2=3", "3=4" }, found);
    }

    /// <summary>
    /// Verifies that a column index finds a ragged field only as <see cref="DelimitedElement.EnumerateObject" /> names
    /// it: not in a non-canonical spelling, not for a column the header names, and not past the record's last field.
    /// </summary>
    /// <param name="name">A name that no field of the record has.</param>
    [TestMethod]
    [DataRow("1")]
    [DataRow("02")]
    [DataRow("+2")]
    [DataRow(" 2")]
    [DataRow("-1")]
    [DataRow("4")]
    public void TryGetProperty_WhenTheNameIsNotAnEnumeratedColumnIndex_ShouldReturnFalse(string name)
    {
        var options = new DelimitedReaderOptions { FieldCountBehavior = DelimitedFieldCountBehavior.Ragged };
        using DelimitedDocument document = DelimitedDocument.Parse("a,b\n1,2,3,4\n"u8, options);
        DelimitedElement record = document.RootElement[0];

        Assert.IsFalse(record.TryGetProperty(name, out _));
    }
}

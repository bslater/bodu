// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateSchemaRangesTests.GetMinimum.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Xml.Schema;

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateSchemaRangesTests
{
    /// <summary>
    /// Verifies that each range's least value is the least value its XSD type accepts: the type accepts it and rejects
    /// the value below it, so the JSON reader and the schema cannot drift apart.
    /// </summary>
    [TestMethod]
    public void GetMinimum_ForEachRange_ShouldBeTheLeastValueTheSchemaAccepts()
    {
        foreach (NotableDateSchemaRange range in Enum.GetValues<NotableDateSchemaRange>())
        {
            XmlSchemaDatatype datatype = SchemaDatatype(range);
            int minimum = NotableDateSchemaRanges.GetMinimum(range);

            Assert.IsTrue(Accepts(datatype, minimum), range + " " + minimum);
            Assert.IsFalse(Accepts(datatype, minimum - 1L), range + " " + (minimum - 1L));
        }
    }

    /// <summary>
    /// Verifies that a value that names no range throws <see cref="ArgumentOutOfRangeException" /> naming the
    /// <c>range</c> parameter.
    /// </summary>
    [TestMethod]
    public void GetMinimum_WhenRangeIsUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = NotableDateSchemaRanges.GetMinimum((NotableDateSchemaRange)99);
        });

        Assert.AreEqual("range", ex.ParamName);
    }
}

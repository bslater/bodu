// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateSchemaRangesTests.GetMaximum.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Xml.Schema;

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateSchemaRangesTests
{
    /// <summary>
    /// Verifies that each range's greatest value is the greatest value its XSD type accepts: the type accepts it and,
    /// unless the type is unbounded and the value is the 32-bit maximum the reader reads, rejects the value above it.
    /// </summary>
    [TestMethod]
    public void GetMaximum_ForEachRange_ShouldBeTheGreatestValueTheSchemaAccepts()
    {
        foreach (NotableDateSchemaRange range in Enum.GetValues<NotableDateSchemaRange>())
        {
            XmlSchemaDatatype datatype = SchemaDatatype(range);
            int maximum = NotableDateSchemaRanges.GetMaximum(range);

            Assert.IsTrue(Accepts(datatype, maximum), range + " " + maximum);
            if (maximum < int.MaxValue)
                Assert.IsFalse(Accepts(datatype, maximum + 1L), range + " " + (maximum + 1L));
        }
    }

    /// <summary>
    /// Verifies that a value that names no range throws <see cref="ArgumentOutOfRangeException" /> naming the
    /// <c>range</c> parameter.
    /// </summary>
    [TestMethod]
    public void GetMaximum_WhenRangeIsUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = NotableDateSchemaRanges.GetMaximum((NotableDateSchemaRange)99);
        });

        Assert.AreEqual("range", ex.ParamName);
    }
}

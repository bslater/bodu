// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateSchemaPatternsTests.GetPattern.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateSchemaPatternsTests
{
    /// <summary>
    /// Verifies that each pattern's text is the pattern the embedded XSD declares for the simple type it stands for, so
    /// the JSON reader and the schema cannot drift apart.
    /// </summary>
    /// <param name="pattern">The name of the <see cref="NotableDateSchemaPattern" /> member.</param>
    /// <param name="simpleType">The name of the XSD simple type the member stands for.</param>
    [TestMethod]
    [DataRow("Identifier", "identifier")]
    [DataRow("ResourceIdentifier", "resourceIdentifier")]
    [DataRow("TerritoryCode", "territoryCode")]
    [DataRow("NonZeroOrdinal", "nonZeroOrdinal")]
    public void GetPattern_ForEachPattern_ShouldReturnTheSchemaPattern(string pattern, string simpleType)
    {
        string actual = NotableDateSchemaPatterns.GetPattern(Enum.Parse<NotableDateSchemaPattern>(pattern));

        Assert.AreEqual(SchemaPattern(simpleType), actual);
    }

    /// <summary>
    /// Verifies that a value that names no pattern throws <see cref="ArgumentOutOfRangeException" /> naming the
    /// <c>pattern</c> parameter.
    /// </summary>
    [TestMethod]
    public void GetPattern_WhenPatternIsUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = NotableDateSchemaPatterns.GetPattern((NotableDateSchemaPattern)99);
        });

        Assert.AreEqual("pattern", ex.ParamName);
    }
}

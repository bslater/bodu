// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8YamlWriterTests.WriteDouble.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Yaml.Document;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that <see cref="Utf8YamlWriter.WriteDouble" /> emits floating-point scalars that round-trip with their
/// type preserved, including the YAML spellings for the non-finite values.
/// </summary>
public partial class Utf8YamlWriterTests
{
    /// <summary>
    /// Verifies that a whole-valued double is emitted with a trailing <c>.0</c> so the scalar resolver reads it back
    /// as a float rather than reclassifying it as an integer.
    /// </summary>
    /// <param name="value">The whole-valued double to write.</param>
    /// <param name="expected">The expected scalar text.</param>
    [TestMethod]
    [DataRow(1.0, "1.0")]
    [DataRow(-2.0, "-2.0")]
    [DataRow(0.0, "0.0")]
    [DataRow(1000000.0, "1000000.0")]
    public void WriteDouble_WhenWholeValued_ShouldEmitFloatForm(double value, string expected)
    {
        string yaml = Write((Utf8YamlWriter w) => w.WriteDouble(value));

        Assert.AreEqual(expected + "\n", yaml);
    }

    /// <summary>
    /// Verifies that a whole-valued double round-trips through parse with its float identity intact.
    /// </summary>
    [TestMethod]
    public void WriteDouble_WhenWholeValued_ShouldRoundTripAsFloat()
    {
        string yaml = Write((Utf8YamlWriter w) => w.WriteDouble(1.0));

        using var doc = YamlDocument.Parse(yaml);
        Assert.AreEqual(YamlValueKind.Float, doc.RootElement.ValueKind);
        Assert.AreEqual(1.0, doc.RootElement.GetDouble());
    }

    /// <summary>
    /// Verifies that a fractional double is emitted in its shortest round-trip form.
    /// </summary>
    [TestMethod]
    public void WriteDouble_WhenFractional_ShouldEmitShortestRoundTrip()
    {
        string yaml = Write((Utf8YamlWriter w) => w.WriteDouble(1.5));

        Assert.AreEqual("1.5\n", yaml);
    }

    /// <summary>
    /// Verifies that a double whose round-trip form uses scientific notation is emitted unchanged and still resolves
    /// as a float.
    /// </summary>
    [TestMethod]
    public void WriteDouble_WhenScientificNotation_ShouldRoundTripAsFloat()
    {
        string yaml = Write((Utf8YamlWriter w) => w.WriteDouble(1e300));

        using var doc = YamlDocument.Parse(yaml);
        Assert.AreEqual(YamlValueKind.Float, doc.RootElement.ValueKind);
        Assert.AreEqual(1e300, doc.RootElement.GetDouble());
    }

    /// <summary>
    /// Verifies that the non-finite values are emitted with their YAML core-schema spellings.
    /// </summary>
    /// <param name="value">The non-finite double to write.</param>
    /// <param name="expected">The expected scalar text.</param>
    [TestMethod]
    [DataRow(double.NaN, ".nan")]
    [DataRow(double.PositiveInfinity, ".inf")]
    [DataRow(double.NegativeInfinity, "-.inf")]
    public void WriteDouble_WhenNonFinite_ShouldEmitYamlSpelling(double value, string expected)
    {
        string yaml = Write((Utf8YamlWriter w) => w.WriteDouble(value));

        Assert.AreEqual(expected + "\n", yaml);
    }

    /// <summary>
    /// Verifies that the text a sequence of doubles and an integer is written as does not follow a current culture that
    /// uses a decimal comma, and that it reads back to the same numbers under that culture.
    /// </summary>
    [TestMethod]
    public void WriteDouble_WhenCurrentCultureUsesDecimalComma_ShouldWriteInvariantText()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            string yaml = Write((Utf8YamlWriter w) =>
            {
                w.WriteStartSequence();
                w.WriteDouble(1.5);
                w.WriteDouble(2.25);
                w.WriteDouble(3.125);
                w.WriteInteger(123456789);
                w.WriteEndSequence();
            });

            Assert.AreEqual("- 1.5\n- 2.25\n- 3.125\n- 123456789\n", yaml);

            using var document = YamlDocument.Parse(yaml);
            Assert.AreEqual(1.5, document.RootElement[0].GetDouble());
            Assert.AreEqual(2.25, document.RootElement[1].GetDouble());
            Assert.AreEqual(3.125, document.RootElement[2].GetDouble());
            Assert.AreEqual(123456789L, document.RootElement[3].GetInt64());
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}

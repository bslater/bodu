// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlExceptionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the constructor surface and position-reporting contracts of <see cref="TomlFormatException" /> and
/// <see cref="TomlSerializationException" />, and their placement in the exception hierarchy.
/// </summary>
[TestClass]
public class TomlExceptionTests
{
    /// <summary>
    /// Verifies that a default-constructed <see cref="TomlFormatException" /> carries no source location.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ForTomlFormatException_ShouldLeaveLocationNull()
    {
        var ex = new TomlFormatException();

        Assert.IsNull(ex.LineNumber);
        Assert.IsNull(ex.ColumnNumber);
        Assert.IsNull(ex.Offset);
        Assert.IsNull(ex.InnerException);
    }

    /// <summary>
    /// Verifies that the message constructor of <see cref="TomlFormatException" /> preserves the message and leaves
    /// the source location unset.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMessageProvided_ForTomlFormatException_ShouldPreserveMessage()
    {
        var ex = new TomlFormatException("malformed");

        Assert.AreEqual("malformed", ex.Message);
        Assert.IsNull(ex.LineNumber);
        Assert.IsNull(ex.ColumnNumber);
        Assert.IsNull(ex.Offset);
    }

    /// <summary>
    /// Verifies that the inner-exception constructor of <see cref="TomlFormatException" /> preserves the chained
    /// exception.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenInnerExceptionProvided_ForTomlFormatException_ShouldPreserveChain()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new TomlFormatException("outer", inner);

        Assert.AreEqual("outer", ex.Message);
        Assert.AreSame(inner, ex.InnerException);
    }

    /// <summary>
    /// Verifies that the location constructor of <see cref="TomlFormatException" /> records the line, column, and
    /// byte offset.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenLocationProvided_ForTomlFormatException_ShouldRecordLineColumnAndOffset()
    {
        var ex = new TomlFormatException("malformed", 3, 7, 42);

        Assert.AreEqual("malformed", ex.Message);
        Assert.AreEqual(3, ex.LineNumber);
        Assert.AreEqual(7, ex.ColumnNumber);
        Assert.AreEqual(42, ex.Offset);
    }

    /// <summary>
    /// Verifies that <see cref="TomlFormatException" /> derives from <see cref="FormatException" />, so callers can
    /// catch the framework base type.
    /// </summary>
    [TestMethod]
    public void TomlFormatException_WhenCaughtAsFormatException_ShouldMatchHierarchy()
    {
        var ex = new TomlFormatException();

        Assert.IsInstanceOfType<FormatException>(ex);
    }

    /// <summary>
    /// Verifies that a parse failure surfaces a <see cref="TomlFormatException" /> whose recorded location points at
    /// the offending line.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSecondLineMalformed_ShouldReportSecondLineLocation()
    {
        TomlFormatException ex = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            using var document = Document.TomlDocument.Parse("a = 1\nb = = 2\n");
        });

        Assert.AreEqual(2, ex.LineNumber);
        Assert.IsNotNull(ex.ColumnNumber);
        Assert.IsNotNull(ex.Offset);
    }

    /// <summary>
    /// Verifies that a default-constructed <see cref="TomlSerializationException" /> carries no byte offset.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ForTomlSerializationException_ShouldLeaveOffsetNull()
    {
        var ex = new TomlSerializationException();

        Assert.IsNull(ex.Offset);
        Assert.IsNull(ex.InnerException);
    }

    /// <summary>
    /// Verifies that the message constructor of <see cref="TomlSerializationException" /> preserves the message and
    /// leaves the byte offset unset.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMessageProvided_ForTomlSerializationException_ShouldPreserveMessage()
    {
        var ex = new TomlSerializationException("binding failed");

        Assert.AreEqual("binding failed", ex.Message);
        Assert.IsNull(ex.Offset);
    }

    /// <summary>
    /// Verifies that the inner-exception constructor of <see cref="TomlSerializationException" /> preserves the
    /// chained exception, including an explicit <see langword="null" /> inner exception.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenInnerExceptionProvided_ForTomlSerializationException_ShouldPreserveChain()
    {
        var inner = new OverflowException("inner");
        var ex = new TomlSerializationException("outer", inner);

        Assert.AreEqual("outer", ex.Message);
        Assert.AreSame(inner, ex.InnerException);

        var withNullInner = new TomlSerializationException("outer", (Exception?)null);
        Assert.IsNull(withNullInner.InnerException);
    }

    /// <summary>
    /// Verifies that the offset constructor of <see cref="TomlSerializationException" /> records the byte offset.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenOffsetProvided_ForTomlSerializationException_ShouldRecordOffset()
    {
        var ex = new TomlSerializationException("binding failed", 23);

        Assert.AreEqual("binding failed", ex.Message);
        Assert.AreEqual(23, ex.Offset);
    }

    /// <summary>
    /// Verifies that <see cref="TomlSerializationException" /> derives directly from <see cref="Exception" /> rather
    /// than <see cref="FormatException" />, keeping binding failures distinct from parse failures.
    /// </summary>
    [TestMethod]
    public void TomlSerializationException_WhenInspected_ShouldNotBeFormatException()
    {
        var ex = new TomlSerializationException();

        Assert.IsNotInstanceOfType<FormatException>(ex);
        Assert.IsInstanceOfType<Exception>(ex);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlExceptionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies the constructor surface and position-reporting contracts of <see cref="YamlFormatException" /> and
/// <see cref="YamlSerializationException" />, and their placement in the exception hierarchy.
/// </summary>
[TestClass]
public class YamlExceptionTests
{
    /// <summary>
    /// Verifies that a default-constructed <see cref="YamlFormatException" /> carries no source location.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ForYamlFormatException_ShouldLeaveLocationNull()
    {
        var ex = new YamlFormatException();

        Assert.IsNull(ex.LineNumber);
        Assert.IsNull(ex.ColumnNumber);
        Assert.IsNull(ex.Offset);
        Assert.IsNull(ex.InnerException);
    }

    /// <summary>
    /// Verifies that the message constructor of <see cref="YamlFormatException" /> preserves the message and leaves
    /// the source location unset.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMessageProvided_ForYamlFormatException_ShouldPreserveMessage()
    {
        var ex = new YamlFormatException("malformed");

        Assert.AreEqual("malformed", ex.Message);
        Assert.IsNull(ex.LineNumber);
        Assert.IsNull(ex.ColumnNumber);
        Assert.IsNull(ex.Offset);
    }

    /// <summary>
    /// Verifies that the inner-exception constructor of <see cref="YamlFormatException" /> preserves the chained
    /// exception.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenInnerExceptionProvided_ForYamlFormatException_ShouldPreserveChain()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new YamlFormatException("outer", inner);

        Assert.AreEqual("outer", ex.Message);
        Assert.AreSame(inner, ex.InnerException);
    }

    /// <summary>
    /// Verifies that the location constructor of <see cref="YamlFormatException" /> records the line, column, and
    /// byte offset.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenLocationProvided_ForYamlFormatException_ShouldRecordLineColumnAndOffset()
    {
        var ex = new YamlFormatException("malformed", 3, 7, 42);

        Assert.AreEqual("malformed", ex.Message);
        Assert.AreEqual(3, ex.LineNumber);
        Assert.AreEqual(7, ex.ColumnNumber);
        Assert.AreEqual(42, ex.Offset);
    }

    /// <summary>
    /// Verifies that <see cref="YamlFormatException" /> derives from <see cref="FormatException" />, so callers can
    /// catch the framework base type.
    /// </summary>
    [TestMethod]
    public void YamlFormatException_WhenCaughtAsFormatException_ShouldMatchHierarchy()
    {
        var ex = new YamlFormatException();

        Assert.IsInstanceOfType<FormatException>(ex);
    }

    /// <summary>
    /// Verifies that a default-constructed <see cref="YamlSerializationException" /> carries no byte offset, source
    /// position, member path, or inner exception.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ForYamlSerializationException_ShouldLeaveOffsetNull()
    {
        var ex = new YamlSerializationException();

        Assert.IsNull(ex.Offset);
        Assert.IsNull(ex.LineNumber);
        Assert.IsNull(ex.ColumnNumber);
        Assert.IsNull(ex.Path);
        Assert.IsNull(ex.InnerException);
    }

    /// <summary>
    /// Verifies that the message constructor of <see cref="YamlSerializationException" /> preserves the message and
    /// leaves the byte offset unset.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMessageProvided_ForYamlSerializationException_ShouldPreserveMessage()
    {
        var ex = new YamlSerializationException("cannot bind");

        Assert.AreEqual("cannot bind", ex.Message);
        Assert.IsNull(ex.Offset);
        Assert.IsNull(ex.Path);
    }

    /// <summary>
    /// Verifies that the inner-exception constructor of <see cref="YamlSerializationException" /> preserves the
    /// chained exception.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenInnerExceptionProvided_ForYamlSerializationException_ShouldPreserveChain()
    {
        var inner = new FormatException("inner");
        var ex = new YamlSerializationException("outer", inner);

        Assert.AreEqual("outer", ex.Message);
        Assert.AreSame(inner, ex.InnerException);
    }

    /// <summary>
    /// Verifies that the offset constructor of <see cref="YamlSerializationException" /> records the byte offset and
    /// leaves the line, column, and member path unset.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenOffsetProvided_ForYamlSerializationException_ShouldRecordOffset()
    {
        var ex = new YamlSerializationException("cannot bind", 42);

        Assert.AreEqual("cannot bind", ex.Message);
        Assert.AreEqual(42, ex.Offset);
        Assert.IsNull(ex.LineNumber);
        Assert.IsNull(ex.ColumnNumber);
        Assert.IsNull(ex.Path);
    }

    /// <summary>
    /// Verifies that <see cref="YamlSerializationException" /> derives directly from <see cref="Exception" />, not
    /// from <see cref="FormatException" />, so a binding failure is distinct from malformed source text.
    /// </summary>
    [TestMethod]
    public void YamlSerializationException_WhenCaughtAsException_ShouldNotBeAFormatException()
    {
        var ex = new YamlSerializationException();

        Assert.IsInstanceOfType<Exception>(ex);
        Assert.IsNotInstanceOfType<FormatException>(ex);
    }
}

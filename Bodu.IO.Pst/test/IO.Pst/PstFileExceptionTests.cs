// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstFileExceptionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

/// <summary>
/// Verifies the behavior of the library's exception hierarchy: <see cref="PstFileException" /> as the base, with
/// <see cref="PstFileFormatException" /> and <see cref="PstUnsupportedFormatException" /> derived from it.
/// </summary>
[TestClass]
public class PstFileExceptionTests
{
    /// <summary>
    /// Verifies that the derived exceptions extend the base so consumers can catch the family together.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenConstructed_ShouldFormSingleHierarchy()
    {
        Assert.IsInstanceOfType<PstFileException>(new PstFileFormatException());
        Assert.IsInstanceOfType<PstFileException>(new PstUnsupportedFormatException());
    }

    /// <summary>
    /// Verifies that the message constructor of each exception preserves the message.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMessage_ShouldPreserveMessage()
    {
        foreach (PstFileException ex in new PstFileException[]
        {
            new("boom"),
            new PstFileFormatException("boom"),
            new PstUnsupportedFormatException("boom"),
        })
        {
            Assert.AreEqual("boom", ex.Message);
            Assert.IsNull(ex.InnerException);
        }
    }

    /// <summary>
    /// Verifies that the message-and-inner constructor of each exception preserves both.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMessageAndInner_ShouldPreserveBoth()
    {
        var inner = new InvalidOperationException("cause");

        foreach (PstFileException ex in new PstFileException[]
        {
            new("boom", inner),
            new PstFileFormatException("boom", inner),
            new PstUnsupportedFormatException("boom", inner),
        })
        {
            Assert.AreEqual("boom", ex.Message);
            Assert.AreSame(inner, ex.InnerException);
        }
    }
}

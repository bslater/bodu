// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResourcePathResolverTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the path-resolution behaviour and argument-validation contracts of
/// <see cref="ResourcePathResolver" />.
/// </summary>
[TestClass]
public sealed class ResourcePathResolverTests
{
    /// <summary>
    /// Verifies that an absolute child path is returned as-is after canonical normalisation,
    /// without joining it onto the supplied document path.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenChildPathIsAbsolute_ShouldReturnNormalisedChildPath()
    {
        IResourcePathResolver resolver = new ResourcePathResolver();

        var result = resolver.Resolve(
            "/Bodu/Globalization/Calendar/Resources/au-all.xml",
            "/Bodu/Globalization/Calendar/Resources/global-public.xml");

        Assert.AreEqual("/Bodu/Globalization/Calendar/Resources/global-public.xml", result);
    }

    /// <summary>
    /// Verifies that a relative child path is resolved against the directory of the supplied
    /// document path, with <c>..</c> segments collapsed.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenChildPathIsRelative_ShouldResolveAgainstDocumentDirectory()
    {
        IResourcePathResolver resolver = new ResourcePathResolver();

        var result = resolver.Resolve(
            "/Bodu/Globalization/Calendar/Resources/au-all.xml",
            "../regions/au-nsw.xml");

        Assert.AreEqual("/Bodu/Globalization/Calendar/regions/au-nsw.xml", result);
    }

    /// <summary>
    /// Verifies that backslash separators in the supplied paths are normalised to forward
    /// slashes before resolution.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenPathsContainBackslashes_ShouldNormaliseToForwardSlashes()
    {
        IResourcePathResolver resolver = new ResourcePathResolver();

        var result = resolver.Resolve(
            @"\Bodu\Globalization\Calendar\Resources\au-all.xml",
            @"..\regions\au-nsw.xml");

        Assert.AreEqual("/Bodu/Globalization/Calendar/regions/au-nsw.xml", result);
    }

    // -----------------------------------------------------------------------------------------
    // Argument validation
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="ResourcePathResolver.Resolve(string, string)" /> throws
    /// <see cref="ArgumentNullException" /> when <paramref name="documentPath" /> is
    /// <see langword="null" /> and exposes the parameter name on the exception.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenDocumentPathIsNull_ShouldThrowExactly()
    {
        IResourcePathResolver resolver = new ResourcePathResolver();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = resolver.Resolve(null!, "child.xml");
        });

        Assert.AreEqual("documentPath", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="ResourcePathResolver.Resolve(string, string)" /> throws
    /// <see cref="ArgumentException" /> when <paramref name="documentPath" /> is empty or
    /// whitespace and exposes the parameter name on the exception.
    /// </summary>
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t\r\n")]
    [TestMethod]
    public void Resolve_WhenDocumentPathIsEmptyOrWhitespace_ShouldThrowExactly(string documentPath)
    {
        IResourcePathResolver resolver = new ResourcePathResolver();

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = resolver.Resolve(documentPath, "child.xml");
        });

        Assert.AreEqual("documentPath", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="ResourcePathResolver.Resolve(string, string)" /> throws
    /// <see cref="ArgumentNullException" /> when <paramref name="childPath" /> is
    /// <see langword="null" /> and exposes the parameter name on the exception.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenChildPathIsNull_ShouldThrowExactly()
    {
        IResourcePathResolver resolver = new ResourcePathResolver();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = resolver.Resolve("/some/document.xml", null!);
        });

        Assert.AreEqual("childPath", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="ResourcePathResolver.Resolve(string, string)" /> throws
    /// <see cref="ArgumentException" /> when <paramref name="childPath" /> is empty or
    /// whitespace and exposes the parameter name on the exception.
    /// </summary>
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t\r\n")]
    [TestMethod]
    public void Resolve_WhenChildPathIsEmptyOrWhitespace_ShouldThrowExactly(string childPath)
    {
        IResourcePathResolver resolver = new ResourcePathResolver();

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = resolver.Resolve("/some/document.xml", childPath);
        });

        Assert.AreEqual("childPath", ex.ParamName);
    }

    // -----------------------------------------------------------------------------------------
    // Path traversal escapes root
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a relative child path whose <c>..</c> segments traverse above the document
    /// root throws <see cref="InvalidOperationException" /> rather than silently returning a
    /// path outside the resource hierarchy.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenRelativeChildPathEscapesRoot_ShouldThrowExactly()
    {
        IResourcePathResolver resolver = new ResourcePathResolver();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = resolver.Resolve("/document.xml", "../escape.xml");
        });
    }

    /// <summary>
    /// Verifies that an absolute child path whose <c>..</c> segments traverse above the root
    /// also throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenAbsoluteChildPathEscapesRoot_ShouldThrowExactly()
    {
        IResourcePathResolver resolver = new ResourcePathResolver();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = resolver.Resolve("/Bodu/Resources/document.xml", "/../../../escape.xml");
        });
    }

    /// <summary>
    /// Verifies that a deeply-nested chain of <c>..</c> segments that exhausts the document
    /// directory and then steps once more above the root throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenManyParentSegmentsExhaustDirectory_ShouldThrowExactly()
    {
        IResourcePathResolver resolver = new ResourcePathResolver();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = resolver.Resolve("/Bodu/Globalization/Calendar/Resources/au-all.xml", "../../../../../escape.xml");
        });
    }

    /// <summary>
    /// Verifies that <c>.</c> segments in the child path are collapsed by the resolver.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenChildContainsCurrentDirectorySegments_ShouldCollapseThem()
    {
        IResourcePathResolver resolver = new ResourcePathResolver();

        var result = resolver.Resolve(
            "/Bodu/Globalization/Calendar/Resources/au-all.xml",
            "./neighbour.xml");

        Assert.AreEqual("/Bodu/Globalization/Calendar/Resources/neighbour.xml", result);
    }

    /// <summary>
    /// Verifies that a child path whose <c>..</c> segments resolve exactly to the root produces a path
    /// at the canonical root.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenChildResolvesAtRoot_ShouldReturnRootRelativePath()
    {
        IResourcePathResolver resolver = new ResourcePathResolver();

        var result = resolver.Resolve(
            "/Bodu/document.xml",
            "../sibling.xml");

        Assert.AreEqual("/sibling.xml", result);
    }

    /// <summary>
    /// Verifies that a child path resolved relative to a root-level document path joins onto the root segment.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenDocumentIsAtRoot_ShouldJoinChildOntoRoot()
    {
        IResourcePathResolver resolver = new ResourcePathResolver();

        var result = resolver.Resolve("/document.xml", "child.xml");

        Assert.AreEqual("/child.xml", result);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CorpusTreeTests.FindRepositoryRoot.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Corpus;

public sealed partial class CorpusTreeTests
{
    /// <summary>
    /// Verifies that the root found from a subdirectory is the directory that holds <c>bodu.slnx</c>.
    /// </summary>
    [TestMethod]
    public void FindRepositoryRoot_WhenAnAncestorHoldsTheSolution_ShouldReturnIt()
    {
        string root = CreateTemporaryRoot();
        try
        {
            File.WriteAllText(Path.Combine(root, "bodu.slnx"), "<Solution />");
            string nested = Directory.CreateDirectory(Path.Combine(root, "a", "b")).FullName;

            string? found = CorpusTree.FindRepositoryRoot(nested);

            Assert.AreEqual(new DirectoryInfo(root).FullName, found);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that no root is found when no ancestor holds <c>bodu.slnx</c>.
    /// </summary>
    [TestMethod]
    public void FindRepositoryRoot_WhenNoAncestorHoldsTheSolution_ShouldReturnNull()
    {
        string root = CreateTemporaryRoot();
        try
        {
            string? found = CorpusTree.FindRepositoryRoot(root);

            Assert.IsNull(found);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

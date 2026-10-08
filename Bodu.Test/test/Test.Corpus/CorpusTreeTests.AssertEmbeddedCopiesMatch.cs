// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CorpusTreeTests.AssertEmbeddedCopiesMatch.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Corpus;

public sealed partial class CorpusTreeTests
{
    /// <summary>
    /// Verifies that the check passes when the corpus holds exactly the embedded catalogues, byte for byte.
    /// </summary>
    [TestMethod]
    public void AssertEmbeddedCopiesMatch_WhenCorpusHoldsTheSameFiles_ShouldPass()
    {
        string root = CreateTemporaryRoot();
        try
        {
            WriteCorpus(root, ("sample-fixes.csv", EmbeddedSample()));

            CorpusTree.AssertEmbeddedCopiesMatch(typeof(CorpusTreeTests).Assembly, "sample-area", root);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that the check fails, naming the file, when an embedded copy differs from its source.
    /// </summary>
    [TestMethod]
    public void AssertEmbeddedCopiesMatch_WhenACopyDiffersFromItsSource_ShouldFail()
    {
        string root = CreateTemporaryRoot();
        try
        {
            byte[] changed = EmbeddedSample();
            changed[^2] = (byte)'X';
            WriteCorpus(root, ("sample-fixes.csv", changed));

            var ex = Assert.ThrowsExactly<AssertFailedException>(() =>
            {
                CorpusTree.AssertEmbeddedCopiesMatch(typeof(CorpusTreeTests).Assembly, "sample-area", root);
            });

            Assert.IsTrue(ex.Message.Contains("sample-fixes.csv differs", StringComparison.Ordinal), ex.Message);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that the check fails when the corpus holds a catalogue the assembly does not embed.
    /// </summary>
    [TestMethod]
    public void AssertEmbeddedCopiesMatch_WhenCorpusHoldsAFileNotEmbedded_ShouldFail()
    {
        string root = CreateTemporaryRoot();
        try
        {
            WriteCorpus(root, ("sample-fixes.csv", EmbeddedSample()), ("other-fixes.csv", EmbeddedSample()));

            var ex = Assert.ThrowsExactly<AssertFailedException>(() =>
            {
                CorpusTree.AssertEmbeddedCopiesMatch(typeof(CorpusTreeTests).Assembly, "sample-area", root);
            });

            Assert.IsTrue(ex.Message.Contains("other-fixes.csv is not embedded", StringComparison.Ordinal), ex.Message);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that the check fails when the assembly embeds a catalogue the corpus does not hold.
    /// </summary>
    [TestMethod]
    public void AssertEmbeddedCopiesMatch_WhenAnEmbeddedCopyHasNoSource_ShouldFail()
    {
        string root = CreateTemporaryRoot();
        try
        {
            WriteCorpus(root);

            var ex = Assert.ThrowsExactly<AssertFailedException>(() =>
            {
                CorpusTree.AssertEmbeddedCopiesMatch(typeof(CorpusTreeTests).Assembly, "sample-area", root);
            });

            Assert.IsTrue(ex.Message.Contains("sample-fixes.csv has no source", StringComparison.Ordinal), ex.Message);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that the check fails when the area has no <c>fixes</c> directory.
    /// </summary>
    [TestMethod]
    public void AssertEmbeddedCopiesMatch_WhenAreaDirectoryIsMissing_ShouldFail()
    {
        string root = CreateTemporaryRoot();
        try
        {
            var ex = Assert.ThrowsExactly<AssertFailedException>(() =>
            {
                CorpusTree.AssertEmbeddedCopiesMatch(typeof(CorpusTreeTests).Assembly, "sample-area", root);
            });

            Assert.IsTrue(ex.Message.Contains("corpus/sample-area/fixes", StringComparison.Ordinal), ex.Message);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that checking a <see langword="null" /> assembly throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void AssertEmbeddedCopiesMatch_WhenAssemblyIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            CorpusTree.AssertEmbeddedCopiesMatch(null!, "sample-area");
        });

        Assert.AreEqual("assembly", ex.ParamName);
    }
}

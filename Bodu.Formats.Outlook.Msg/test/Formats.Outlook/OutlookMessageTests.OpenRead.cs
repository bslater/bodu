// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMessageTests.OpenRead.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Msg;
using Bodu.Test;
using Bodu.Test.Assertions;

namespace Bodu.Formats.Outlook;

public partial class OutlookMessageTests
{
    /// <summary>
    /// Verifies that opening a minimal synthetic message surfaces its subject and body - the package's primary happy
    /// path.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void OpenRead_WhenMinimalMessage_ShouldExposeSubjectAndBody()
    {
        using MemoryStream container = CreateMinimalContainer();

        using var message = OutlookMessage.OpenRead(container);

        Assert.AreEqual("Test subject", message.Subject);
        Assert.AreEqual("Test body", message.Properties.GetString(MapiPropertyIds.Body));
    }

    /// <summary>
    /// Verifies that a message opens from a file path.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenPath_ShouldOpenFile()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".msg");
        using (MemoryStream container = CreateMinimalContainer())
            File.WriteAllBytes(path, container.ToArray());

        try
        {
            using var message = OutlookMessage.OpenRead(path);

            Assert.AreEqual("Test subject", message.Subject);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Verifies that <c>leaveOpen</c> governs whether disposing the message disposes the caller's stream.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenLeaveOpen_ShouldHonorStreamOwnership()
    {
        MemoryStream kept = CreateMinimalContainer();
        using (OutlookMessage.OpenRead(kept, leaveOpen: true))
        {
        }

        Assert.IsTrue(kept.CanRead);
        kept.Dispose();

        MemoryStream disposed = CreateMinimalContainer();
        using (OutlookMessage.OpenRead(disposed, leaveOpen: false))
        {
        }

        Assert.IsFalse(disposed.CanRead);
    }

    /// <summary>
    /// Verifies the argument guards of the open overloads.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenArgumentsNull_ShouldThrowArgumentNullException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = OutlookMessage.OpenRead((string)null!);
            },
            "path");

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = OutlookMessage.OpenRead((Stream)null!);
            },
            "stream");

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                using MemoryStream container = CreateMinimalContainer();
                _ = OutlookMessage.Open(container, null!);
            },
            "options");
    }

    /// <summary>
    /// Verifies that a stream that is not a compound file throws <see cref="OutlookMsgFormatException" /> with the
    /// container failure as its inner exception.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenNotCompoundFile_ShouldThrowOutlookMsgFormatException()
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        var ex = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
        {
            _ = OutlookMessage.OpenRead(stream, leaveOpen: true);
        });

        Assert.IsNotNull(ex.InnerException);
        Assert.IsInstanceOfType<Bodu.IO.Compound.CompoundFileFormatException>(ex.InnerException);
    }

    /// <summary>
    /// Verifies that a compound file without the message property stream throws
    /// <see cref="OutlookMsgFormatException" />.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenCompoundFileIsNotMessage_ShouldThrowOutlookMsgFormatException()
    {
        using MemoryStream container = new MsgFixtureBuilder().OmitPropertiesStream().Build();

        _ = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
        {
            _ = OutlookMessage.OpenRead(container, leaveOpen: true);
        });
    }

    /// <summary>
    /// Verifies that an empty path throws <see cref="ArgumentException" /> naming the <c>path</c> parameter.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenPathIsEmpty_ShouldThrowArgumentException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                _ = OutlookMessage.OpenRead(string.Empty);
            },
            "path");
    }

    /// <summary>
    /// Verifies that opening a path whose compound file is not a message (its root holds a <c>WordDocument</c> stream
    /// and no <c>__properties_version1.0</c>) throws <see cref="OutlookMsgFormatException" /> and leaves the file
    /// unlocked, because the method disposes the file stream it opened.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenFileIsNotAMessage_ShouldReleaseTheFile()
    {
        using MemoryStream container = new MsgFixtureBuilder()
            .OmitPropertiesStream()
            .AddRawStream("WordDocument", "abc"u8.ToArray())
            .Build();
        string path = WriteTemporaryMessage(container);

        try
        {
            _ = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
            {
                _ = OutlookMessage.OpenRead(path);
            });

            AssertFileIsReleased(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Verifies that opening a path whose root property stream is malformed (the 32-byte header, one record and 7
    /// trailing bytes, which are not a whole record) throws <see cref="OutlookMsgFormatException" /> and leaves the file
    /// unlocked.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenPropertyStreamIsMalformed_ShouldReleaseTheFile()
    {
        MsgFixtureBuilder builder = new MsgFixtureBuilder().AddUnicode(MapiPropertyIds.MessageClass, "IPM.Note");
        byte[] properties = [.. builder.BuildPropertiesPayload(), .. new byte[7]];
        using MemoryStream container = builder
            .OmitPropertiesStream()
            .AddRawStream(MsgStreamNames.PropertiesStreamName, properties)
            .Build();
        string path = WriteTemporaryMessage(container);

        try
        {
            _ = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
            {
                _ = OutlookMessage.OpenRead(path);
            });

            AssertFileIsReleased(path);
        }
        finally
        {
            File.Delete(path);
        }
    }
}

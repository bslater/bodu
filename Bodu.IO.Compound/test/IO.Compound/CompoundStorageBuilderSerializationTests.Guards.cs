// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorageBuilderSerializationTests.Guards.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.IO.Compound.Builders;

namespace Bodu.IO.Compound;

public partial class CompoundStorageBuilderSerializationTests
{
    /// <summary>
    /// Verifies that exceeding the configured maximum nesting depth throws
    /// <see cref="CompoundFileSerializationException" /> during serialization.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenNestingExceedsMaxDepth_ShouldThrowCompoundFileSerializationException()
    {
        var builder = CompoundStorageBuilder.CreateRoot();
        _ = builder.AddStorage("A").AddStorage("B").AddStorage("C");

        _ = Assert.ThrowsExactly<CompoundFileSerializationException>(
            () => builder.ToArray(new CompoundBuildOptions { MaxDepth = 2 }));
    }

    /// <summary>
    /// Verifies that every serializer throws <see cref="ArgumentOutOfRangeException" /> naming <c>options</c> for a
    /// version that is neither the default nor a defined <see cref="CompoundFileVersion" />, before it writes any byte or
    /// creates any file.
    /// </summary>
    /// <param name="entryPoint">The serializer called.</param>
    [TestMethod]
    [DataRow("WriteToStream")]
    [DataRow("WriteToBufferWriter")]
    [DataRow("Save")]
    [DataRow("ToArray")]
    public void WriteTo_WhenVersionIsUndefined_ShouldThrowArgumentOutOfRangeException(string entryPoint)
    {
        var builder = CompoundStorageBuilder.CreateRoot();
        _ = builder.AddStream("A", CompoundContainerBytes.Pattern(100));
        var options = new CompoundBuildOptions { Version = (CompoundFileVersion)5 };
        using var destination = new MemoryStream();
        var output = new ArrayBufferWriter<byte>();
        string path = Path.Combine(Path.GetTempPath(), $"bodu-undefined-version-{Guid.NewGuid():N}.cfb");
        try
        {
            ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                switch (entryPoint)
                {
                    case "WriteToStream":
                        builder.WriteTo(destination, options);
                        break;

                    case "WriteToBufferWriter":
                        builder.WriteTo(output, options);
                        break;

                    case "Save":
                        builder.Save(path, options);
                        break;

                    default:
                        _ = builder.ToArray(options);
                        break;
                }
            }, entryPoint);

            Assert.AreEqual("options", ex.ParamName, entryPoint);
            Assert.AreEqual(0, destination.Length, entryPoint);
            Assert.AreEqual(0, output.WrittenCount, entryPoint);
            Assert.IsFalse(File.Exists(path), entryPoint);
        }
        finally
        {
            File.Delete(path);
        }
    }
}

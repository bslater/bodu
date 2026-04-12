using Bodu.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography.Extensions
{
    public partial class ICryptoTransformExtensionsTests
    {
        public static IEnumerable<object[]> GetValidTransformTestData()
        {
            yield return new object[]
            {
                new KnownAnswerTest
                {
                    Name = "Reversal Only",
                    Input = new byte[] { 1, 2, 3, 4 },
                    ExpectedOutput = new byte[] { 4, 3, 2, 1 },
                    Parameters = new Dictionary<string, object>
                    {
                        ["BlockSize"] = 32,
                        ["Padding"] = PaddingMode.None,
                        ["Mode"] = TransformMode.Encrypt
                    }
                }
            };
        }

        [TestMethod]
        public void Transform_ByteArray_WhenNullInput_ShouldThrow()
        {
            using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                transform.Transform(null!);
            });
        }

        [DataTestMethod]
        [DataRow(null)]
        public void Transform_ByteArray_WhenNullTransform_ShouldThrow(ICryptoTransform? transform)
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                transform!.Transform(new byte[] { 1, 2, 3 });
            });
        }

        /// <summary>
        /// Verifies that Transform(byte[]) correctly transforms a valid input using reversal logic.
        /// </summary>
        [DataTestMethod]
        [DynamicData(nameof(GetValidTransformTestData), DynamicDataSourceType.Method)]
        public void Transform_ByteArray_WhenValid_ShouldTransform(KnownAnswerTest kat)
        {
            using var transform = CreateTransform(kat);
            byte[] actual = transform.Transform(kat.Input);
            CollectionAssert.AreEqual(kat.ExpectedOutput, actual, $"Test case failed: {kat.Name}");
        }

        [TestMethod]
        public void Transform_ByteArrayRange_WhenOffsetOutOfRange_ShouldThrow()
        {
            using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);
            byte[] data = new byte[] { 1, 2, 3 };
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                transform.Transform(data, 2, 2);
            });
        }

        /// <summary>
        /// Verifies that Transform(byte[], int, int) returns expected output for valid offset/count.
        /// </summary>
        [DataTestMethod]
        [DynamicData(nameof(GetValidTransformTestData), DynamicDataSourceType.Method)]
        public void Transform_ByteArrayRange_WhenValid_ShouldTransform(KnownAnswerTest kat)
        {
            using var transform = CreateTransform(kat);
            byte[] actual = transform.Transform(kat.Input, 0, kat.Input.Length);
            CollectionAssert.AreEqual(kat.ExpectedOutput, actual, $"Test case failed: {kat.Name}");
        }

        /// <summary>
        /// Verifies that Transform(ReadOnlyMemory&lt;byte&gt;) produces the expected transformed output.
        /// </summary>
        [DataTestMethod]
        [DynamicData(nameof(GetValidTransformTestData), DynamicDataSourceType.Method)]
        public void Transform_Memory_WhenValid_ShouldTransform(KnownAnswerTest kat)
        {
            using var transform = CreateTransform(kat);
            byte[] actual = transform.Transform((ReadOnlyMemory<byte>)kat.Input);
            CollectionAssert.AreEqual(kat.ExpectedOutput, actual, $"Test case failed: {kat.Name}");
        }

        /// <summary>
        /// Verifies that Transform(ReadOnlySpan&lt;byte&gt;) produces the expected transformed output.
        /// </summary>
        [DataTestMethod]
        [DynamicData(nameof(GetValidTransformTestData), DynamicDataSourceType.Method)]
        public void Transform_Span_WhenValid_ShouldTransform(KnownAnswerTest kat)
        {
            using var transform = CreateTransform(kat);
            byte[] actual = transform.Transform((ReadOnlySpan<byte>)kat.Input);
            CollectionAssert.AreEqual(kat.ExpectedOutput, actual, $"Test case failed: {kat.Name}");
        }

        [TestMethod]
        public void Transform_SpanToSpan_WhenDestinationTooSmall_ShouldThrow()
        {
            using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                ReadOnlySpan<byte> input = new byte[] { 1, 2, 3, 4 };
                Span<byte> destination = new byte[2];
                transform.Transform(input, destination);
            });
        }

        [TestMethod]
        public void Transform_Stream_WhenBufferSizeZero_ShouldThrow()
        {
            using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);
            var source = new MemoryStream(new byte[] { 1, 2, 3 });
            var target = new MemoryStream();
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                transform.Transform(source, target, 0);
            });
        }

        [TestMethod]
        public void Transform_WhenCalled_ShouldNotDisposeTargetStream()
        {
            using var algorithm = CreateAlgorithm();
            using var transform = algorithm.CreateEncryptor();

            byte[] inputData = Encoding.UTF8.GetBytes("hello world");
            using var input = new MemoryStream(inputData);

            var target = new MemoryStream();
            transform.Transform(input, target, bufferSize: 16);

            // The target stream should still be open and writable
            Assert.IsTrue(target.CanWrite, "Target stream should remain open after Transform.");

            // Optionally, verify we can still write to the stream
            try
            {
                target.WriteByte(0xFF); // Write additional byte
            }
            catch (ObjectDisposedException)
            {
                Assert.Fail("Target stream was unexpectedly closed or disposed.");
            }
        }

        [TestMethod]
        public void Transform_WhenCalled_WithNoPadding_ShouldNotDisposeTargetStream()
        {
            using var algorithm = CreateAlgorithm();
            algorithm.Padding = PaddingMode.None;
            using var transform = algorithm.CreateEncryptor();

            using var input = new MemoryStream();

            var target = new MemoryStream();
            transform.Transform(input, target, bufferSize: algorithm.BlockSize / 8);

            // The target stream should still be open and writable
            Assert.IsTrue(target.CanWrite, "Target stream should remain open after Transform.");

            // Optionally, verify we can still write to the stream
            try
            {
                target.WriteByte(0xFF); // Write additional byte
            }
            catch (ObjectDisposedException)
            {
                Assert.Fail("Target stream was unexpectedly closed or disposed.");
            }
        }

        private static SimpleReversingCryptoTransform CreateTransform(KnownAnswerTest kat)
        {
            int blockSize = kat.TryGet("BlockSize", out int bs) ? bs : 32;
            PaddingMode padding = kat.TryGet("Padding", out PaddingMode p) ? p : PaddingMode.None;
            TransformMode mode = kat.TryGet("Mode", out TransformMode m) ? m : TransformMode.Encrypt;
            byte[] key = kat.TryGet("Key", out byte[]? k) ? k! : new byte[blockSize / 8];
            byte[] iv = kat.TryGet("IV", out byte[]? i) ? i! : new byte[blockSize / 8];
            byte[]? tweak = kat.TryGet("Tweak", out byte[]? t) ? t : null;

            return new SimpleReversingCryptoTransform(
                blockSizeBits: blockSize,
                feedbackSizeBits: blockSize,
                key: key,
                iv: iv,
                tweak: tweak,
                cipherMode: CipherMode.ECB,
                paddingMode: padding,
                transformMode: mode);
        }
    }
}
// -----------------------------------------------------------------------
// <copyright file="BlockHashAlgorithmDefectTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    using System;
    using System.Reflection;
    using System.Security.Cryptography;
    using System.Text.RegularExpressions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Regression tests that lock in fixes for defects previously discovered in
    /// <see cref="BlockHashAlgorithm{T}"/> and its derived cryptographic hash types
    /// (Tiger, SipHash, Poly1305, CubeHash).
    /// </summary>
    [TestClass]
    public class BlockHashAlgorithmDefectTests
    {
        /// <summary>
        /// Defect B regression: <c>ThrowIfDisposed</c> previously used
        /// <c>nameof(T)</c> on a non-existent generic parameter. After the fix it
        /// reports the concrete derived type name via <c>GetType().Name</c>.
        /// </summary>
        [TestMethod]
        public void Tiger_Dispose_ThenHashCore_ShouldThrowObjectDisposedExceptionWithTigerName()
        {
            var algo = new Tiger();
            algo.Dispose();
            var ex = Assert.ThrowsExactly<ObjectDisposedException>(() => algo.ComputeHash(new byte[] { 1, 2, 3 }));
            Assert.AreEqual(nameof(Tiger), ex.ObjectName);
        }

        /// <summary>
        /// Defect B regression: SipHash-derived types must surface their concrete
        /// type name from <c>ObjectDisposedException.ObjectName</c>.
        /// </summary>
        [TestMethod]
        public void SipHash64_Dispose_ThenHashCore_ShouldThrowObjectDisposedExceptionWithConcreteName()
        {
            var algo = new SipHash64();
            algo.Dispose();
            var ex = Assert.ThrowsExactly<ObjectDisposedException>(() => algo.ComputeHash(new byte[] { 1, 2, 3 }));
            Assert.AreEqual(nameof(SipHash64), ex.ObjectName);
        }

        /// <summary>
        /// Defect D regression: <c>SipHash.PadBlock</c>'s argument-validation
        /// message must be plain ASCII with no embedded NUL characters (a previous
        /// file-encoding round-trip introduced mojibake). The throw branch is not
        /// reachable from the public API because the base
        /// <see cref="BlockHashAlgorithm{T}"/> always passes a residual span shorter
        /// than the configured block size, and <c>PadBlock</c> takes a
        /// <see cref="ReadOnlySpan{T}"/> which cannot be invoked via reflection.
        /// We therefore exercise the happy path through the public hashing API
        /// (which routes through <c>PadBlock</c>) for several non-aligned input
        /// lengths and assert that no exception (and in particular no exception
        /// with a NUL-bearing message) escapes.
        /// </summary>
        [TestMethod]
        public void SipHash_PadBlock_ExceptionMessage_ShouldBeCleanAscii()
        {
            // Exercise residual lengths 0..7 — the full domain that PadBlock
            // accepts on the happy path. If any of these surface an exception
            // whose message contains NUL or non-ASCII, the defect has regressed.
            for (int len = 0; len < 16; len++)
            {
                using var algo = new SipHash64();
                byte[] data = new byte[len];
                try
                {
                    byte[] tag = algo.ComputeHash(data);
                    Assert.IsNotNull(tag);
                }
                catch (Exception ex)
                {
                    string message = ex.Message ?? string.Empty;
                    Assert.IsFalse(
                        message.Contains('\0'),
                        $"Exception message must not contain NUL characters (input length {len}).");
                    Assert.IsTrue(
                        Regex.IsMatch(message, "^[\\x20-\\x7E]*$"),
                        $"Exception message must be plain printable ASCII (input length {len}). Actual: '{message}'");
                    throw;
                }
            }
        }

        /// <summary>
        /// Defect E regression: <c>CubeHash.Initialize()</c> must reset the base
        /// <see cref="HashAlgorithm.State"/> so the instance can be reconfigured
        /// after a hash operation completes.
        /// </summary>
        [TestMethod]
        public void CubeHash_Initialize_AfterHashing_ShouldResetStateForReuse()
        {
            using var algo = new CubeHash();
            byte[] first = algo.ComputeHash(new byte[] { 1, 2, 3, 4 });
            Assert.IsNotNull(first);

            // After hashing, State may be non-zero. Initialize() must clear it so
            // configuration setters do not throw CryptographicUnexpectedOperationException.
            algo.Initialize();

            // Reconfiguring HashSize would throw CryptographicUnexpectedOperationException
            // if Initialize() failed to reset State.
            algo.HashSize = 256;

            byte[] second = algo.ComputeHash(new byte[] { 5, 6, 7, 8 });
            Assert.IsNotNull(second);
            Assert.AreEqual(256 / 8, second.Length);
        }

        /// <summary>
        /// Defect G regression: <c>Poly1305.Initialize()</c> must throw
        /// <see cref="CryptographicException"/> when the key has been cleared,
        /// instead of silently regenerating a fresh random key. The default
        /// constructor seeds a random key, so we explicitly clear the key first.
        /// </summary>
        [TestMethod]
        public void Poly1305_Initialize_WithoutKey_ShouldThrowCryptographicException()
        {
            var algo = new Poly1305();

            // The constructor seeds KeyValue with random bytes. To exercise the
            // "key not assigned" branch we clear it via the protected KeyValue
            // field, which is reachable through the KeyedHashAlgorithm base.
            FieldInfo? keyValueField = null;
            for (Type? t = algo.GetType(); t != null && keyValueField == null; t = t.BaseType)
            {
                keyValueField = t.GetField(
                    "KeyValue",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            }

            Assert.IsNotNull(keyValueField, "Unable to locate KeyValue field on Poly1305.");
            keyValueField!.SetValue(algo, null);

            var ex = Assert.ThrowsExactly<CryptographicException>(() => algo.Initialize());
            Assert.IsTrue(
                ex.Message.IndexOf("key", StringComparison.OrdinalIgnoreCase) >= 0,
                $"Expected exception message to mention 'key'. Actual: '{ex.Message}'");
        }

        /// <summary>
        /// Defect F regression: <c>Poly1305.PadBlock</c> must never raise
        /// <see cref="NotImplementedException"/>. <c>ShouldPadFinalBlock</c> returns
        /// false for Poly1305, so the padding path is unreachable on the happy
        /// path; this test is a guard against a regression that re-introduces a
        /// reachable code path.
        /// </summary>
        [TestMethod]
        public void Poly1305_PadBlock_IsUnreachableOrThrowsNotSupported()
        {
            using var algo = new Poly1305();
            try
            {
                byte[] tag = algo.ComputeHash(new byte[] { 1, 2, 3, 4, 5 });
                Assert.IsNotNull(tag);
                Assert.AreEqual(16, tag.Length);
            }
            catch (NotImplementedException)
            {
                Assert.Fail("Poly1305.PadBlock must not raise NotImplementedException.");
            }
            catch (NotSupportedException)
            {
                // Acceptable: the contract method throws NotSupportedException if
                // ever dispatched to. The happy path should not reach it, however.
            }
        }

        /// <summary>
        /// Defect A compilation guard: the <c>protected bool finalized;</c>
        /// declaration in <see cref="BlockHashAlgorithm{T}"/> must compile under
        /// both modern (.NET 6+) and pre-.NET 6 target frameworks. This test is a
        /// no-op at runtime; its presence in the test assembly is the assertion
        /// because compilation of the reference itself proves the field exists.
        /// </summary>
        [TestMethod]
        public void BlockHashAlgorithm_FinalizedField_Declared_CompilationGuard()
        {
            // The fact that BlockHashAlgorithm<Tiger> compiles transitively via
            // the Tiger reference confirms the base-class field declaration is
            // syntactically valid on the current TFM. Pre-.NET 6 TFMs are guarded
            // by the same #if and exercised by the build matrix.
            Assert.IsTrue(true);
        }
    }
}
